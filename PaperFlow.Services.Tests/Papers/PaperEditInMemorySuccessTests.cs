

namespace PaperFlow.Services.Tests.Papers
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using PaperFlow.Data;
    using PaperFlow.Data.Models;
    using PaperFlow.Services.Models.Papers;
    using PaperFlow.Services.Papers;

[TestFixture]
public sealed class PaperEditInMemorySuccessTests
{
    private const string Alice = "edit-alice";
    private const string Bob = "edit-bob";

    private PowerFlowDbContext context = null!;
    private PaperEditService service = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<PowerFlowDbContext>()
            .UseInMemoryDatabase($"paper-edit-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        context = new PowerFlowDbContext(options);
        context.Users.AddRange(
            new IdentityUser { Id = Alice, UserName = "alice@example.test", Email = "alice@example.test" },
            new IdentityUser { Id = Bob, UserName = "bob@example.test", Email = "bob@example.test" });
        await context.SaveChangesAsync();
        service = new PaperEditService(context);
    }

    [TearDown]
    public async Task TearDown() => await context.DisposeAsync();

    private async Task<int> CreatePaperAsync(string userId = Alice, string title = "Original research title", string? doi = null)
    {
        var paper = new Paper { UserId = userId, Title = title, Doi = doi };
        context.Papers.Add(paper);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return paper.Id;
    }

    private static PaperEditDto Edit(int id, string title = "Updated research title") => new()
    {
        Id = id,
        Title = title
    };

    [Test]
    public async Task UpdateAsync_ValidMetadata_SavesTrimmedValues()
    {
        int id = await CreatePaperAsync();
        var input = Edit(id, "  Updated research title  ");
        input.Abstract = "  Fresh abstract  ";
        input.Doi = "https://doi.org/10.1000/TEST";
        input.PublicationUrl = "  https://example.org/article  ";
        input.PdfUrl = "https://example.org/article.pdf";
        input.PublicationDate = new DateOnly(2026, 11, 27);

        var result = await service.UpdateAsync(input, Alice);

        context.ChangeTracker.Clear();
        var paper = await context.Papers.SingleAsync(p => p.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PaperEditStatus.Success));
            Assert.That(paper.Title, Is.EqualTo("Updated research title"));
            Assert.That(paper.Abstract, Is.EqualTo("Fresh abstract"));
            Assert.That(paper.Doi, Is.EqualTo("10.1000/test"));
            Assert.That(paper.PublicationUrl, Is.EqualTo("https://example.org/article"));
            Assert.That(paper.PdfUrl, Is.EqualTo("https://example.org/article.pdf"));
            Assert.That(paper.PublicationDate, Is.EqualTo(new DateOnly(2026, 11, 27)));
            Assert.That(paper.ModifiedOn, Is.Not.Null);
        });
    }

    [Test]
    public async Task UpdateAsync_EmptyOptionalFields_AreSavedAsNull()
    {
        int id = await CreatePaperAsync(doi: "10.1000/old");
        var input = Edit(id);
        input.Abstract = " \t ";
        input.Doi = " ";
        input.PublicationUrl = " ";
        input.PdfUrl = null;

        var result = await service.UpdateAsync(input, Alice);

        context.ChangeTracker.Clear();
        var paper = await context.Papers.SingleAsync(p => p.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PaperEditStatus.Success));
            Assert.That(paper.Abstract, Is.Null);
            Assert.That(paper.Doi, Is.Null);
            Assert.That(paper.PublicationUrl, Is.Null);
            Assert.That(paper.PdfUrl, Is.Null);
        });
    }

    [Test]
    public async Task UpdateAsync_Authors_TrimsDeduplicatesAndKeepsOrder()
    {
        int id = await CreatePaperAsync();
        var input = Edit(id);
        input.Authors = ["  Ada Lovelace  ", "Alan Turing", "ada lovelace", " ", "Grace Hopper"];

        var result = await service.UpdateAsync(input, Alice);

        context.ChangeTracker.Clear();
        var names = await context.PaperAuthors
            .Where(pa => pa.PaperId == id)
            .OrderBy(pa => pa.AuthorOrder)
            .Select(pa => pa.Author.FullName)
            .ToListAsync();
        Assert.That(result.Status, Is.EqualTo(PaperEditStatus.Success));
        Assert.That(names, Is.EqualTo(new[] { "Ada Lovelace", "Alan Turing", "Grace Hopper" }));
    }

    [Test]
    public async Task UpdateAsync_Categories_TrimsAndDeduplicates()
    {
        int id = await CreatePaperAsync();
        var input = Edit(id);
        input.Categories = ["  Robotics  ", "AI", "robotics", " "];

        var result = await service.UpdateAsync(input, Alice);

        context.ChangeTracker.Clear();
        var categories = await context.PaperCategories
            .Where(pc => pc.PaperId == id)
            .Select(pc => pc.Category.Name)
            .ToListAsync();
        Assert.That(result.Status, Is.EqualTo(PaperEditStatus.Success));
        Assert.That(categories, Is.EquivalentTo(new[] { "Robotics", "AI" }));
    }

    [Test]
    public async Task UpdateAsync_OldAuthorAndCategoryLinks_AreReplaced()
    {
        int id = await CreatePaperAsync();
        context.PaperAuthors.Add(new PaperAuthor
        {
            PaperId = id,
            Author = new Author { FullName = "Old Author" },
            AuthorOrder = 1
        });
        context.PaperCategories.Add(new PaperCategory
        {
            PaperId = id,
            Category = new Category { Name = "Old Category" }
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var input = Edit(id);
        input.Authors = ["New Author"];
        input.Categories = ["New Category"];
        var result = await service.UpdateAsync(input, Alice);

        context.ChangeTracker.Clear();
        var authors = await context.PaperAuthors.Where(p => p.PaperId == id)
            .Select(p => p.Author.FullName).ToListAsync();
        var categories = await context.PaperCategories.Where(p => p.PaperId == id)
            .Select(p => p.Category.Name).ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PaperEditStatus.Success));
            Assert.That(authors, Is.EqualTo(new[] { "New Author" }));
            Assert.That(categories, Is.EqualTo(new[] { "New Category" }));
        });
    }

    [Test]
    public async Task UpdateAsync_RepeatedSave_DoesNotDuplicateRelationships()
    {
        int id = await CreatePaperAsync();
        var input = Edit(id);
        input.Authors = ["A Researcher"];
        input.Categories = ["Robotics"];

        var first = await service.UpdateAsync(input, Alice);
        context.ChangeTracker.Clear();
        var second = await service.UpdateAsync(input, Alice);
        context.ChangeTracker.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(PaperEditStatus.Success));
            Assert.That(second.Status, Is.EqualTo(PaperEditStatus.Success));
            Assert.That(context.PaperAuthors.Count(p => p.PaperId == id), Is.EqualTo(1));
            Assert.That(context.PaperCategories.Count(p => p.PaperId == id), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task UpdateAsync_NullAuthorAndCategoryLists_RemoveOldLinks()
    {
        int id = await CreatePaperAsync();
        context.PaperAuthors.Add(new PaperAuthor
        {
            PaperId = id,
            Author = new Author { FullName = "Old Author" },
            AuthorOrder = 1
        });
        context.PaperCategories.Add(new PaperCategory
        {
            PaperId = id,
            Category = new Category { Name = "Old Category" }
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var input = Edit(id);
        input.Authors = null!;
        input.Categories = null!;
        var result = await service.UpdateAsync(input, Alice);
        context.ChangeTracker.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PaperEditStatus.Success));
            Assert.That(context.PaperAuthors.Count(pa => pa.PaperId == id), Is.Zero);
            Assert.That(context.PaperCategories.Count(pc => pc.PaperId == id), Is.Zero);
        });
    }

    [Test]
    public async Task UpdateAsync_ForeignPaper_ReturnsNotFoundAndKeepsOriginal()
    {
        int id = await CreatePaperAsync(Bob, "Bob secret research");

        var result = await service.UpdateAsync(Edit(id, "Alice attempted edit"), Alice);

        context.ChangeTracker.Clear();
        Assert.That(result.Status, Is.EqualTo(PaperEditStatus.NotFound));
        Assert.That((await context.Papers.SingleAsync(p => p.Id == id)).Title,
            Is.EqualTo("Bob secret research"));
    }

    [Test]
    public async Task UpdateAsync_DuplicateDoiForSameUser_IsRejected()
    {
        int id = await CreatePaperAsync(title: "Original first paper", doi: "10.1000/first");
        await CreatePaperAsync(title: "Original second paper", doi: "10.1000/second");
        var input = Edit(id);
        input.Doi = "doi:10.1000/second";

        var result = await service.UpdateAsync(input, Alice);

        Assert.That(result.Status, Is.EqualTo(PaperEditStatus.DuplicateDoi));
        context.ChangeTracker.Clear();
        Assert.That((await context.Papers.SingleAsync(p => p.Id == id)).Doi,
            Is.EqualTo("10.1000/first"));
    }

    [Test]
    public async Task UpdateAsync_DuplicateTitleForSameUser_IsRejected()
    {
        int id = await CreatePaperAsync(title: "Original first paper");
        await CreatePaperAsync(title: "Existing duplicate title");

        var result = await service.UpdateAsync(Edit(id, " existing duplicate title "), Alice);

        Assert.That(result.Status, Is.EqualTo(PaperEditStatus.DuplicateTitle));
    }

    [Test]
    public async Task UpdateAsync_SameTitleAndDoiForDifferentUser_AreAllowed()
    {
        int id = await CreatePaperAsync(Alice, "First Alice paper");
        await CreatePaperAsync(Bob, "Shared research title", "10.1000/shared");
        var input = Edit(id, "Shared research title");
        input.Doi = "10.1000/shared";

        var result = await service.UpdateAsync(input, Alice);

        Assert.That(result.Status, Is.EqualTo(PaperEditStatus.Success));
    }
}
}


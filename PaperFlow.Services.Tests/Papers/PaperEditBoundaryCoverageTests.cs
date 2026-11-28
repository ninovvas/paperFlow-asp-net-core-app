namespace PaperFlow.Services.Tests.Papers
{
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Common.Constants;
    using PaperFlow.Services.Models.Papers;
    using PaperFlow.Services.Papers;
    using PaperFlow.Services.Tests.Infrastructure;

    [TestFixture]
    public sealed class PaperEditBoundaryCoverageTests
    {
        private PaperFlowTestDatabase db = null!;
        private PaperEditService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = new PaperFlowTestDatabase();
            service = new PaperEditService(db.Context);
        }

        [TearDown]
        public async Task TearDown() => await db.DisposeAsync();

        [Test]
        public async Task GetAsync_MissingUserId_ReturnsNull()
        {
            Assert.That(await service.GetAsync(1, " "), Is.Null);
        }

        [Test]
        public async Task GetAsync_MissingPaper_ReturnsNull()
        {
            Assert.That(await service.GetAsync(999999, PaperFlowTestDatabase.AliceId), Is.Null);
        }

        [Test]
        public async Task GetAsync_ReturnsMetadataForOwnerOnly()
        {
            await db.SeedTwoUsersAsync();
            var edited = await db.Context.Papers.FindAsync(db.AlicePaperId);
            edited!.Doi = "10.1234/example";
            edited.PublicationUrl = "https://example.org/paper";
            await db.Context.SaveChangesAsync();
            db.Context.ChangeTracker.Clear();

            var own = await service.GetAsync(db.AlicePaperId, PaperFlowTestDatabase.AliceId);
            var foreign = await service.GetAsync(db.BobPaperId, PaperFlowTestDatabase.AliceId);
            Assert.Multiple(() =>
            {
                Assert.That(own, Is.Not.Null);
                Assert.That(own!.Doi, Is.EqualTo("10.1234/example"));
                Assert.That(own.PublicationUrl, Is.EqualTo("https://example.org/paper"));
                Assert.That(foreign, Is.Null);
            });
        }

        [TestCase("ftp://example.org/paper")]
        [TestCase("javascript:alert(1)")]
        [TestCase("/relative/path")]
        public async Task Update_UnsupportedPublicationUrl_ReturnsValidationFailed(string url)
        {
            var result = await service.UpdateAsync(new PaperEditDto
            {
                Id = 1,
                Title = "An otherwise valid paper title",
                PublicationUrl = url
            }, PaperFlowTestDatabase.AliceId);
            Assert.That(result.Status, Is.EqualTo(PaperEditStatus.ValidationFailed));
        }

        [Test]
        public async Task Update_InvalidPdfUrl_ReturnsValidationFailed()
        {
            var result = await service.UpdateAsync(new PaperEditDto
            {
                Id = 1,
                Title = "An otherwise valid paper title",
                PdfUrl = "file:///etc/passwd"
            }, PaperFlowTestDatabase.AliceId);
            Assert.That(result.Status, Is.EqualTo(PaperEditStatus.ValidationFailed));
        }

        [Test]
        public async Task Update_TooLongAbstract_ReturnsValidationFailed()
        {
            var result = await service.UpdateAsync(new PaperEditDto
            {
                Id = 1,
                Title = "An otherwise valid paper title",
                Abstract = new string('a', DataConstants.Paper.AbstractMaxLength + 1)
            }, PaperFlowTestDatabase.AliceId);
            Assert.That(result.Status, Is.EqualTo(PaperEditStatus.ValidationFailed));
        }

        [Test]
        public async Task Update_TooLongAuthor_ReturnsValidationFailed()
        {
            var result = await service.UpdateAsync(new PaperEditDto
            {
                Id = 1,
                Title = "An otherwise valid paper title",
                Authors = [new string('a', DataConstants.Author.NameMaxLength + 1)]
            }, PaperFlowTestDatabase.AliceId);
            Assert.That(result.Status, Is.EqualTo(PaperEditStatus.ValidationFailed));
        }

        [Test]
        public async Task Update_TooLongCategory_ReturnsValidationFailed()
        {
            var result = await service.UpdateAsync(new PaperEditDto
            {
                Id = 1,
                Title = "An otherwise valid paper title",
                Categories = [new string('c', DataConstants.Category.NameMaxLength + 1)]
            }, PaperFlowTestDatabase.AliceId);
            Assert.That(result.Status, Is.EqualTo(PaperEditStatus.ValidationFailed));
        }

        [Test]
        public async Task Update_TooLongTitle_ReturnsValidationFailed()
        {
            var result = await service.UpdateAsync(new PaperEditDto
            {
                Id = 1,
                Title = new string('x', DataConstants.Paper.TitleMaxLength + 1)
            }, PaperFlowTestDatabase.AliceId);
            Assert.That(result.Status, Is.EqualTo(PaperEditStatus.ValidationFailed));
        }
    }
}

namespace PaperFlow.Services.Tests.Infrastructure
{
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PaperFlow.Common.Enums;
using PaperFlow.Data;
using PaperFlow.Data.Models;

/// <summary>Creates a unique EF Core InMemory database for each NUnit test.</summary>
public sealed class PaperFlowTestDatabase : IAsyncDisposable
{
    public const string AliceId = "alice-test-user";
    public const string BobId = "bob-test-user";

    public PowerFlowDbContext Context { get; }
    public int AlicePaperId { get; private set; }
    public int BobPaperId { get; private set; }

    public PaperFlowTestDatabase()
    {
        var options = new DbContextOptionsBuilder<PowerFlowDbContext>()
            .UseInMemoryDatabase($"paperflow-tests-{Guid.NewGuid():N}")
            .Options;
        Context = new PowerFlowDbContext(options);
    }

    public async Task SeedTwoUsersAsync()
    {
        Context.Users.AddRange(
            new IdentityUser { Id = AliceId, UserName = "alice@paperflow.test", Email = "alice@paperflow.test" },
            new IdentityUser { Id = BobId, UserName = "bob@paperflow.test", Email = "bob@paperflow.test" });
        await Context.SaveChangesAsync();

        var alice = new Paper
        {
            Title = "Alice's autonomous navigation paper",
            UserId = AliceId,
            Status = ReadingStatus.Reading,
            TotalPages = 100,
            CurrentPage = 20
        };
        var bob = new Paper
        {
            Title = "Bob's confidential research paper",
            UserId = BobId,
            Status = ReadingStatus.NotStarted
        };
        Context.Papers.AddRange(alice, bob);
        await Context.SaveChangesAsync();
        AlicePaperId = alice.Id;
        BobPaperId = bob.Id;
        Context.ChangeTracker.Clear();
    }

    public async ValueTask DisposeAsync() => await Context.DisposeAsync();
}
}

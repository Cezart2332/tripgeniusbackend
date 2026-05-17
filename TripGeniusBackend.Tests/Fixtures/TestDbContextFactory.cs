using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Infrastructure.Persistence;

namespace TripGeniusBackend.Tests.Fixtures;

/// <summary>
/// Factory for creating in-memory test database contexts with isolation between tests
/// </summary>
public class TestDbContextFactory
{
    public static AppDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static AppDbContext CreateTestContextWithData(Action<AppDbContext> seedData)
    {
        var context = CreateTestContext();
        seedData(context);
        context.SaveChanges();
        return context;
    }
}

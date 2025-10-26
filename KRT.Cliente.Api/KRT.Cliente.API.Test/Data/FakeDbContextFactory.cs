using KRT.Cliente.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace KRT.Cliente.API.Test.Data;

public static class FakeDbContextFactory
{
    public static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}

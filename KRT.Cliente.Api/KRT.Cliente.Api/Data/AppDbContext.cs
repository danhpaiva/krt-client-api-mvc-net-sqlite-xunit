using Microsoft.EntityFrameworkCore;

namespace KRT.Cliente.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext (DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Models.Conta> TB_Contas { get; set; } = default!;
}

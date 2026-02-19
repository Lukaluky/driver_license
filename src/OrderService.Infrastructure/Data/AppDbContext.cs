using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<DriverApplication> Applications => Set<DriverApplication>();
    public DbSet<LicenceCard> LicenceCards => Set<LicenceCard>();
    public DbSet<ApplicationSummaryView> ApplicationSummaries => Set<ApplicationSummaryView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<ApplicationSummaryView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("ApplicationSummaries");
        });
    }
}

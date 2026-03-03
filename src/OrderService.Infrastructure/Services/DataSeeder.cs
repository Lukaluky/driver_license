using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Services;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrated successfully");

            await SeedViewAsync(context);

            if (!await context.Users.AnyAsync(u => u.Role == UserRole.Inspector))
            {
                var inspector = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "inspector@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Inspector123!"),
                    Role = UserRole.Inspector,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddAsync(inspector);
                await context.SaveChangesAsync();
                logger.LogInformation("Test inspector seeded: inspector@test.com / Inspector123!");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private static async Task SeedViewAsync(AppDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""DROP VIEW IF EXISTS "ApplicationSummaries";""");

        const string viewSql = """
            CREATE OR REPLACE VIEW "ApplicationSummaries" AS
            SELECT
                "Category"::text AS "Category",
                COUNT(*)::int AS "TotalCount",
                COUNT(*) FILTER (WHERE "Status" = 'Pending')::int AS "PendingCount",
                COUNT(*) FILTER (WHERE "Status" IN ('ExternalChecksInProgress', 'ExternalChecksPassed', 'AssignedToInspector'))::int AS "InProgressCount",
                COUNT(*) FILTER (WHERE "Status" = 'Approved')::int AS "ApprovedCount",
                COUNT(*) FILTER (WHERE "Status" = 'Rejected')::int AS "RejectedCount",
                COUNT(*) FILTER (WHERE "Status" = 'Printed')::int AS "PrintedCount",
                MIN("CreatedAt") FILTER (WHERE "Status" = 'Pending') AS "OldestPendingCreatedAt",
                MAX("CreatedAt") AS "LatestApplication"
            FROM "Applications"
            GROUP BY "Category"
            """;

        await context.Database.ExecuteSqlRawAsync(viewSql);
    }
}

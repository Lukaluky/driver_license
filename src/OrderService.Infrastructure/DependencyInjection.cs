using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Interfaces;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Jobs;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Services;
using StackExchange.Redis;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Postgres")!;

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var redisConnection = config.GetConnectionString("Redis") ?? "localhost:6399";
        var redisOptions = ConfigurationOptions.Parse(redisConnection);
        redisOptions.AbortOnConnectFail = false;
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisOptions));
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IEmailService, RabbitMqEmailService>();

        services.AddScoped<IExternalCheckService, ExternalCheckService>();
        services.AddHttpClient("external-checks");
        services.AddHostedService<RabbitMqEmailConsumer>();

        services.AddScoped<ExternalCheckJob>();

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();

        return services;
    }
}

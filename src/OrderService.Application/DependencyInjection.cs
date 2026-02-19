using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Interfaces;
using OrderService.Application.Mapping;
using OrderService.Application.Services;

namespace OrderService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IApplicationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IApplicationService, ApplicationService>();

        MappingConfig.Configure();

        return services;
    }
}

using Mapster;
using OrderService.Application.DTOs.Applications;
using OrderService.Domain.Entities;

namespace OrderService.Application.Mapping;

public static class MappingConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<DriverApplication, ApplicationResponse>.NewConfig()
            .Map(dest => dest.Category, src => src.Category.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Data.Configurations;

public class ExternalCheckProviderConfigurationEntityTypeConfiguration
    : IEntityTypeConfiguration<ExternalCheckProviderConfiguration>
{
    public void Configure(EntityTypeBuilder<ExternalCheckProviderConfiguration> builder)
    {
        builder.ToTable("ExternalCheckProviders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.Property(x => x.BaseUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Path)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.TimeoutSeconds)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.ExecutionOrder)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}

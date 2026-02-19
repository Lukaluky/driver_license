using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Data.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<DriverApplication>
{
    public void Configure(EntityTypeBuilder<DriverApplication> builder)
    {
        builder.ToTable("Applications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Iin)
            .IsRequired()
            .HasMaxLength(12);

        builder.HasIndex(a => a.Iin);

        builder.Property(a => a.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => new { a.ApplicantId, a.Category, a.Status });
    }
}

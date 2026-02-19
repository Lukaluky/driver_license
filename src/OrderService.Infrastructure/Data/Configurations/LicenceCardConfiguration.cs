using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Data.Configurations;

public class LicenceCardConfiguration : IEntityTypeConfiguration<LicenceCard>
{
    public void Configure(EntityTypeBuilder<LicenceCard> builder)
    {
        builder.ToTable("LicenceCards");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CardNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.CardNumber).IsUnique();

        builder.Property(c => c.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(c => c.IssuedAt).IsRequired();
        builder.Property(c => c.ExpiresAt).IsRequired();

        builder.HasOne(c => c.Application)
            .WithOne(a => a.LicenceCard)
            .HasForeignKey<LicenceCard>(c => c.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

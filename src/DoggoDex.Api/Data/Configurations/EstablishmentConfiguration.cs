using DoggoDex.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoggoDex.Api.Data.Configurations;

public class EstablishmentConfiguration : IEntityTypeConfiguration<Establishment>
{
    public void Configure(EntityTypeBuilder<Establishment> b)
    {
        b.ToTable("establishments");
        b.HasKey(e => e.Id);

        b.Property(e => e.Name).IsRequired().HasMaxLength(150);
        b.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.Location).HasMaxLength(200);
        b.Property(e => e.Description).HasMaxLength(1000);

        b.HasOne(e => e.OwnerUser)
            .WithMany()
            .HasForeignKey(e => e.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => e.OwnerUserId).IsUnique();
        b.HasIndex(e => e.Name);
    }
}
using DoggoDex.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DoggoDex.Api.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Establishment> Establishments => Set<Establishment>();
    public DbSet<Dog> Dogs => Set<Dog>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- USER ---
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role)
                  .HasConversion<string>()
                  .IsRequired();
            entity.Property(u => u.CreatedAt).IsRequired();
        });

        // --- OWNER ---
        modelBuilder.Entity<Owner>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.UserId).IsUnique();
            entity.HasIndex(o => o.ShareableID).IsUnique();
            entity.Property(o => o.Name).IsRequired();
            entity.Property(o => o.AggregateRating).HasColumnType("decimal(3,2)");

            entity.HasOne(o => o.User)
                  .WithOne(u => u.OwnerProfile)
                  .HasForeignKey<Owner>(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ESTABLISHMENT ---
        modelBuilder.Entity<Establishment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Name).IsRequired();

            entity.HasOne(e => e.User)
                  .WithOne(u => u.EstablishmentProfile)
                  .HasForeignKey<Establishment>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- DOG ---
        modelBuilder.Entity<Dog>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.ShareableID).IsUnique();
            entity.Property(d => d.Name).IsRequired();
            entity.Property(d => d.AggregateRating).HasColumnType("decimal(3,2)");
            entity.Property(d => d.ReviewCount).IsRequired();

            entity.HasOne(d => d.Owner)
                  .WithMany(o => o.Dogs)
                  .HasForeignKey(d => d.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- REVIEW ---
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.ReviewerType)
                  .HasConversion<string>()
                  .IsRequired();

            entity.Property(r => r.ModerationStatus)
                  .HasConversion<string>()
                  .IsRequired();

            entity.Property(r => r.CreatedAt).IsRequired();

            // Check constraints for rating bounds and target XOR condition
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Review_LeashManners", "\"LeashManners\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Review_NoiseControl", "\"NoiseControl\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Review_SocialReactivity", "\"SocialReactivity\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Review_OwnerAttentiveness", "\"OwnerAttentiveness\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Review_Cleanliness", "\"Cleanliness\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Review_SingleTarget", "(\"OwnerId\" IS NOT NULL AND \"DogId\" IS NULL) OR (\"OwnerId\" IS NULL AND \"DogId\" IS NOT NULL)");
            });

            // Unique composite indexes to enforce one review per reviewer per target
            entity.HasIndex(r => new { r.ReviewerId, r.OwnerId })
                  .IsUnique()
                  .HasFilter("\"OwnerId\" IS NOT NULL");

            entity.HasIndex(r => new { r.ReviewerId, r.DogId })
                  .IsUnique()
                  .HasFilter("\"DogId\" IS NOT NULL");

            // Relationships
            entity.HasOne(r => r.Reviewer)
                  .WithMany(u => u.AuthoredReviews)
                  .HasForeignKey(r => r.ReviewerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Owner)
                  .WithMany(o => o.Reviews)
                  .HasForeignKey(r => r.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Dog)
                  .WithMany(d => d.Reviews)
                  .HasForeignKey(r => r.DogId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
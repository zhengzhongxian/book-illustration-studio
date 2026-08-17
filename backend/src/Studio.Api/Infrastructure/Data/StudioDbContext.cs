using Microsoft.EntityFrameworkCore;
using Studio.Api.Domain.Entities;

namespace Studio.Api.Infrastructure.Data;

public class StudioDbContext : DbContext
{
    public StudioDbContext(DbContextOptions<StudioDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<Chapter> Chapters => Set<Chapter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.UserId);
            entity.Property(p => p.Title).IsRequired().HasMaxLength(500);
            entity.Property(p => p.BookText).IsRequired();

            entity.HasOne(p => p.User)
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.ProjectId);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(255);

            entity.HasOne(c => c.Project)
                .WithMany(p => p.Characters)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.ProjectId);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(255);

            entity.HasOne(c => c.Project)
                .WithMany(p => p.Chapters)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

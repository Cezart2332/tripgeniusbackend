using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<TripMember> TripMembers { get; set; }
    public DbSet<TripHistory> TripHistories { get; set; }
    public DbSet<Preferences> Preferences { get; set; }
    public DbSet<Message> Messages { get; set; }
    
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Bug> Bugs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Trip>().Property(t => t.Status).HasConversion<string>();
        modelBuilder.Entity<TripHistory>().Property(t => t.Action).HasConversion<string>();
        modelBuilder.Entity<TripMember>().Property(t => t.Role).HasConversion<string>();
        modelBuilder.Entity<Bug>().Property(b => b.Status).HasConversion<string>();
    }

}
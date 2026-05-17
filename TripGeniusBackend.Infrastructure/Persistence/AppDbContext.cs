using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<TripMember> TripMembers { get; set; }
    public DbSet<TripHistory> TripHistories { get; set; }
    
    public DbSet<TripTimeline> TripTimelines { get; set; }
    public DbSet<Preferences> Preferences { get; set; }
    public DbSet<PushSubscription> PushSubscriptions { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AiChatHistory> AiChatHistories { get; set; }
    public DbSet<AiMemory> AiMemories { get; set; }

    
    public DbSet<Bug> Bugs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.Entity<AiMemory>()
            .Property(m => m.Embedding)
            .HasColumnType("vector(2048)"); 
        modelBuilder.Entity<Trip>().Property(t => t.Status).HasConversion<string>();
        modelBuilder.Entity<TripMember>().Property(t => t.MemberStatus).HasConversion<string>();
        modelBuilder.Entity<TripMember>().Property(t => t.Role).HasConversion<string>();
        modelBuilder.Entity<TripActivity>().Property(a => a.Type).HasConversion<string>();
        modelBuilder.Entity<Bug>().Property(b => b.Status).HasConversion<string>();

    }

}
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
    public DbSet<OffroadTrip> OffroadTrips { get; set; }
    public DbSet<OffroadRoute> OffroadRoutes { get; set; }
    public DbSet<OffroadTripMember> OffroadTripMembers { get; set; }
    public DbSet<OffroadTripHistory> OffroadTripHistories { get; set; }
    public DbSet<OffroadMessage> OffroadMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            modelBuilder.Entity<AiMemory>()
                .Property(m => m.Embedding)
                .HasConversion(
                    v => v.ToString(),
                    v => new Pgvector.Vector(v));

            modelBuilder.Entity<Trip>()
                .Property(t => t.Embedding)
                .HasConversion(
                    v => v != null ? v.ToString() : null,
                    v => v != null ? new Pgvector.Vector(v) : null);

            modelBuilder.Entity<OffroadTrip>()
                .Property(t => t.Embedding)
                .HasConversion(
                    v => v != null ? v.ToString() : null,
                    v => v != null ? new Pgvector.Vector(v) : null);
        }
        else
        {
            modelBuilder.HasDefaultSchema("public");
            modelBuilder.HasPostgresExtension("vector");
            modelBuilder.Entity<AiMemory>()
                .Property(m => m.Embedding)
                .HasColumnType("vector(2048)");
            modelBuilder.Entity<OffroadTrip>()
                .Property(t => t.Embedding)
                .HasColumnType("vector(2048)");
        }
        modelBuilder.Entity<Trip>().Property(t => t.Status).HasConversion<string>();
        modelBuilder.Entity<TripMember>().Property(t => t.MemberStatus).HasConversion<string>();
        modelBuilder.Entity<TripMember>().Property(t => t.Role).HasConversion<string>();
        modelBuilder.Entity<TripActivity>().Property(a => a.Type).HasConversion<string>();
        modelBuilder.Entity<Bug>().Property(b => b.Status).HasConversion<string>();
        modelBuilder.Entity<OffroadTrip>().Property(t => t.Status).HasConversion<string>();
        modelBuilder.Entity<OffroadTripMember>().Property(t => t.MemberStatus).HasConversion<string>();
        modelBuilder.Entity<OffroadTripMember>().Property(t => t.Role).HasConversion<string>();
        modelBuilder.Entity<OffroadTripMember>().Property(t => t.Type).HasConversion<string>();
        modelBuilder.Entity<OffroadRoute>().Property(r => r.Source).HasConversion<string>();
        modelBuilder.Entity<OffroadRoute>().Property(r => r.TrackGeoJson).HasColumnType("jsonb");
        modelBuilder.Entity<OffroadTrip>().HasMany(t => t.Routes).WithOne(r => r.OffroadTrip).HasForeignKey(r => r.OffroadTripId);
        modelBuilder.Entity<OffroadTrip>().HasMany(t => t.Members).WithOne(m => m.OffroadTrip).HasForeignKey(m => m.OffroadTripId);
        modelBuilder.Entity<OffroadTrip>().HasMany(t => t.History).WithOne(h => h.OffroadTrip).HasForeignKey(h => h.OffroadTripId);

        modelBuilder.Entity<Message>().Property(m => m.SenderType).HasConversion<string>();
        modelBuilder.Entity<OffroadMessage>().Property(m => m.SenderType).HasConversion<string>();
    }

}
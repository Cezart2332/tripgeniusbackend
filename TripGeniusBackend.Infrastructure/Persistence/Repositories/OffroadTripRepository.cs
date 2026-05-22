using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Repositories;

public class OffroadTripRepository : IOffroadTripRepository
{
    private readonly AppDbContext _context;

    public OffroadTripRepository(AppDbContext context) => _context = context;

    public Task CreateTrip(OffroadTrip trip)
    {
        _context.OffroadTrips.Add(trip);
        return Task.CompletedTask;
    }

    public Task UpdateTrip(OffroadTrip trip)
    {
        _context.OffroadTrips.Update(trip);
        return Task.CompletedTask;
    }

    public async Task<OffroadTrip?> GetTripById(int id) =>
        await _context.OffroadTrips
            .Include(t => t.Members)
            .Include(t => t.History)
            .Include(t => t.Routes)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task DeleteTrip(int id)
    {
        var trip = await _context.OffroadTrips.FindAsync(id);
        if (trip is null) return;
        _context.OffroadTrips.Remove(trip);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChanges() => await _context.SaveChangesAsync();

    public async Task<List<OffroadTrip>> SearchSimilarAsync(float[] queryEmbedding, int userId, int limit = 5)
    {
        var vector = new Vector(queryEmbedding);
        return await _context.OffroadTrips
            .Where(t => t.Embedding != null && !t.Members.Any(m => m.UserId == userId))
            .OrderBy(t => t.Embedding!.CosineDistance(vector))
            .Include(t => t.Routes)
            .Take(limit)
            .ToListAsync();
    }
}

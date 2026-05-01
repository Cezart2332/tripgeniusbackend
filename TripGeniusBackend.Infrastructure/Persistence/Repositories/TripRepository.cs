using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Domain.Entities;


namespace TripGeniusBackend.Infrastructure.Persistence.Repositories;

public class TripRepository : ITripRepository
{
    private readonly AppDbContext _context;
    
    public TripRepository(AppDbContext context)
    {
        _context = context;
    }

    public  Task CreateTrip(Trip trip)
    { 
        _context.Trips.Add(trip);
        return Task.CompletedTask;
        
    }

    public Task UpdateTrip(Trip trip)
    {
        _context.Trips.Update(trip);
        return Task.CompletedTask;
    }

    public async Task<Trip?> GetTripById(int id)
    {
        return await _context.Trips.Include(t => t.Members).Include(t => t.History).Include(t => t.Timelines).FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task SaveChanges()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<Trip>> SearchSimilarAsync(float[] queryEmbedding,int userId, int limit = 5)
    {
        var vector = new Vector(queryEmbedding);
        return await _context.Trips.Where(t => t.Embedding != null && !t.Members.Any(m => m.UserId == userId))
            .OrderBy(t => t.Embedding!.CosineDistance(vector)).Take(limit).ToListAsync();
    }
}
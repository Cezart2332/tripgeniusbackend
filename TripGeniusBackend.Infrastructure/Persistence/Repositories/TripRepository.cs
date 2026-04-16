using Microsoft.EntityFrameworkCore;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

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

    public async Task SaveChanges()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Trip?> GetTripById(int id)
    {
        var trip = await _context.Trips.Include(t => t.Timelines).Include(t => t.Members).Include(t => t.History).FirstOrDefaultAsync(t => t.Id == id);
        if(trip == null) return null;
        return trip;
    }

    public async Task<List<Trip>> GetTrips()
    {
        return await _context.Trips.Where(t => t.Status == Status.Upcoming).ToListAsync();
    }
}
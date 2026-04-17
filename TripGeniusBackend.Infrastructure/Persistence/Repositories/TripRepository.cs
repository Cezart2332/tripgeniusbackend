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


}
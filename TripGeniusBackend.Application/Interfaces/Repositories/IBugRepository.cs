using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces;

public interface IBugRepository
{
    public Task CreateBug(Bug bug);
}
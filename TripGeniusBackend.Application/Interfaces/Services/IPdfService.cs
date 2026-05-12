using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Application.Interfaces;

public interface IPdfService
{
    public byte[] GenerateCostsPdf(Trip trip);
}
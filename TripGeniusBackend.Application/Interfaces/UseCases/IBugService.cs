using TripGeniusBackend.Application.DTOs.User;

namespace TripGeniusBackend.Application.Interfaces;

public interface IBugService
{
    public Task ReportBug(BugRequest bugRequest);
}
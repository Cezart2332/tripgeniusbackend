using TripGeniusBackend.Application.DTOs.User;

namespace TripGeniusBackend.Application.Interfaces.UseCases;

public interface IBugService
{
    public Task ReportBug(BugRequest bugRequest);
}
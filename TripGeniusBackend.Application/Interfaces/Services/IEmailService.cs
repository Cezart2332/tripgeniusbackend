namespace TripGeniusBackend.Application.Interfaces;

public interface IEmailService
{
    public Task SendEmailAsync(string to, string subject, string content, string actionUrl, string actionLabel);
}
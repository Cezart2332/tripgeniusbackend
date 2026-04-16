namespace TripGeniusBackend.Application.Interfaces;

public interface IPasswordHasher
{
    public string HashPassword(string Password);
    public bool VerifyPassword(string Password, string HashedPassword);
}
namespace TripGeniusBackend.Application.Interfaces;

public interface ITokenHasher
{
    public string HashToken(string token);
    public bool VerifyToken(string token, string hashedToken);
}
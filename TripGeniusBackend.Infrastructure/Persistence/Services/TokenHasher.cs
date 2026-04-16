using System.Security.Cryptography;
using System.Text;
using TripGeniusBackend.Application.Interfaces;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class TokenHasher : ITokenHasher
{
    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
    public bool VerifyToken(string token, string hashedToken)
    {
        var hashedTokenBytes = Convert.FromBase64String(hashedToken);
        var tokenBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return hashedTokenBytes.SequenceEqual(tokenBytes);
    }
}
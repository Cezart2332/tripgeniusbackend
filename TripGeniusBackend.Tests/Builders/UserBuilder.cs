using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Tests.Builders;

/// <summary>
/// Builder pattern for creating test User entities with default or custom values
/// </summary>
public class UserBuilder
{
    private int _id = 1;
    private string _email = "test@example.com";
    private string _username = "testuser";
    private string _passwordHash = "hashed_password";
    private bool _isEmailConfirmed = true;

    public UserBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithEmailConfirmed(bool confirmed)
    {
        _isEmailConfirmed = confirmed;
        return this;
    }

    public User Build()
    {
        var user = User.UserCreate(_email, _passwordHash);
        
        // Use reflection to set private Id property
        typeof(User).GetProperty("Id")?.SetValue(user, _id);
        
        if (_isEmailConfirmed)
        {
            user.VerifyEmail();
        }

        user.UpdateProfile(_username, "", "");

        return user;
    }
}

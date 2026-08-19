namespace Paga.Domain.Entities;

/// <summary>
/// Application user with authentication credentials.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User(Guid id, string name, string email, string passwordHash, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    // EF Core requires a parameterless constructor; kept private to enforce invariants.
    private User()
    {
        Name = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }
}

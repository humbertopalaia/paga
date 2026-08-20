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

    /// <summary>
    /// Updates the user's mutable fields. Password hash is only changed when a new value is provided.
    /// </summary>
    /// <param name="name">New display name.</param>
    /// <param name="email">New email address.</param>
    /// <param name="passwordHash">New BCrypt hash, or null to keep the current one.</param>
    public void Update(string name, string email, string? passwordHash = null)
    {
        Name = name;
        Email = email;
        if (passwordHash is not null)
            PasswordHash = passwordHash;
    }

    // EF Core requires a parameterless constructor; kept private to enforce invariants.
    private User()
    {
        Name = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }
}

using Flow.Domain.Common;
using Flow.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Flow.Domain.Entities;

public class User : IdentityUser<Guid>, IAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public int Points { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private User() { }

    public static User Create(string name, string email, UserRole role)
    {
        var now = DateTimeOffset.UtcNow;
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            Role = role,
            Points = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void AddPoints(int points)
    {
        if (points <= 0) throw new ArgumentException("Points must be positive.", nameof(points));
        Points += points;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

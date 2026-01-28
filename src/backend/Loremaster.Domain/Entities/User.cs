using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;

namespace Loremaster.Domain.Entities;

public class User : AuditableEntity
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; } = UserRole.Player;
    public string? DisplayName { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }
    
    // Master-Player relationship
    public Guid? MasterId { get; private set; }
    public User? Master { get; private set; }
    
    // Invitation code for Masters
    public string? InvitationCode { get; private set; }
    
    // Players associated with this Master
    private readonly List<User> _players = new();
    public IReadOnlyCollection<User> Players => _players.AsReadOnly();

    private User() { } // EF Core

    public static User Create(
        string email, 
        string passwordHash, 
        string? displayName = null,
        UserRole role = UserRole.Player,
        Guid? masterId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        var user = new User
        {
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            DisplayName = displayName?.Trim(),
            Role = role,
            MasterId = role == UserRole.Player ? masterId : null
        };
        
        // Generate invitation code for Masters
        if (role == UserRole.Master)
        {
            user.InvitationCode = GenerateInvitationCode();
        }

        return user;
    }

    private static string GenerateInvitationCode()
    {
        return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }

    public void RegenerateInvitationCode()
    {
        if (Role != UserRole.Master)
            throw new InvalidOperationException("Only Masters can have invitation codes");
            
        InvitationCode = GenerateInvitationCode();
    }

    public void AssignToMaster(Guid masterId)
    {
        if (Role != UserRole.Player)
            throw new InvalidOperationException("Only Players can be assigned to a Master");
            
        MasterId = masterId;
    }

    public void UpdateDisplayName(string? displayName)
    {
        DisplayName = displayName?.Trim();
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
        
        // Generate invitation code when becoming a Master
        if (newRole == UserRole.Master && string.IsNullOrEmpty(InvitationCode))
        {
            InvitationCode = GenerateInvitationCode();
        }
        
        // Clear master relationship when becoming a Master
        if (newRole == UserRole.Master)
        {
            MasterId = null;
        }
    }

    public void SetRefreshToken(string refreshToken, DateTime expiryTime)
    {
        RefreshToken = refreshToken;
        RefreshTokenExpiryTime = expiryTime;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiryTime = null;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsMaster => Role == UserRole.Master;
    public bool IsPlayer => Role == UserRole.Player;
}

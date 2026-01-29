using Loremaster.Domain.Common;
using Loremaster.Domain.Enums;

namespace Loremaster.Domain.Entities;

public class User : SoftDeletableEntity
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Player;
    public string? AvatarUrl { get; private set; }
    
    // External authentication support (for future OAuth integration)
    public string? ExternalId { get; private set; }
    
    // Password-based authentication fields
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    
    // Master-Player relationship (legacy)
    public string? InvitationCode { get; private set; }
    public Guid? MasterId { get; private set; }
    public User? Master { get; private set; }
    
    private readonly List<User> _players = new();
    public IReadOnlyCollection<User> Players => _players.AsReadOnly();

    // Navigation properties
    private readonly List<Campaign> _ownedCampaigns = new();
    public IReadOnlyCollection<Campaign> OwnedCampaigns => _ownedCampaigns.AsReadOnly();

    private readonly List<CampaignMember> _campaignMemberships = new();
    public IReadOnlyCollection<CampaignMember> CampaignMemberships => _campaignMemberships.AsReadOnly();

    private readonly List<LoreEntity> _ownedLoreEntities = new();
    public IReadOnlyCollection<LoreEntity> OwnedLoreEntities => _ownedLoreEntities.AsReadOnly();

    private readonly List<GenerationRequest> _generationRequests = new();
    public IReadOnlyCollection<GenerationRequest> GenerationRequests => _generationRequests.AsReadOnly();

    private readonly List<LoreEntityImport> _imports = new();
    public IReadOnlyCollection<LoreEntityImport> Imports => _imports.AsReadOnly();

    private User() { } // EF Core

    /// <summary>
    /// Creates a new user with password-based authentication
    /// </summary>
    public static User Create(
        string email,
        string passwordHash,
        string? displayName,
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
            MasterId = masterId,
            IsActive = true
        };
        
        // Generate invitation code for Masters
        if (role == UserRole.Master)
        {
            user.InvitationCode = GenerateInvitationCode();
        }

        return user;
    }

    /// <summary>
    /// Creates a new user with external authentication (OAuth)
    /// </summary>
    public static User CreateWithExternalAuth(
        string externalId,
        string email,
        string displayName,
        UserRole role = UserRole.Player,
        string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External ID cannot be empty", nameof(externalId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        var user = new User
        {
            ExternalId = externalId.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            DisplayName = displayName?.Trim(),
            PasswordHash = string.Empty, // No password for external auth
            Role = role,
            AvatarUrl = avatarUrl?.Trim(),
            IsActive = true
        };

        if (role == UserRole.Master)
        {
            user.InvitationCode = GenerateInvitationCode();
        }

        return user;
    }

    public void Update(string? displayName, string? avatarUrl = null)
    {
        DisplayName = displayName?.Trim();
        AvatarUrl = avatarUrl?.Trim();
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
        
        // Generate invitation code if becoming a Master
        if (newRole == UserRole.Master && string.IsNullOrEmpty(InvitationCode))
        {
            InvitationCode = GenerateInvitationCode();
        }
    }

    public void UpdateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        Email = email.ToLowerInvariant().Trim();
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }

    public void SetRefreshToken(string token, DateTime expiryTime)
    {
        RefreshToken = token;
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

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        RevokeRefreshToken();
    }

    public void RegenerateInvitationCode()
    {
        if (Role != UserRole.Master)
            throw new InvalidOperationException("Only Masters can have invitation codes");

        InvitationCode = GenerateInvitationCode();
    }

    public void SetMaster(User master)
    {
        if (Role != UserRole.Player)
            throw new InvalidOperationException("Only Players can be assigned to a Master");

        if (master.Role != UserRole.Master)
            throw new InvalidOperationException("The specified user is not a Master");

        MasterId = master.Id;
        Master = master;
    }

    public void RemoveMaster()
    {
        MasterId = null;
        Master = null;
    }

    private static string GenerateInvitationCode()
    {
        return Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    }

    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsMaster => Role == UserRole.Master;
    public bool IsPlayer => Role == UserRole.Player;
}

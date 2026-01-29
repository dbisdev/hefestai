using System.Text.Json;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(
        IApplicationDbContext dbContext,
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<CampaignsController> logger)
    {
        _dbContext = dbContext;
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all campaigns the current user is a member of
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CampaignDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CampaignDto>>> GetMyCampaigns(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var campaigns = await _campaignRepository.GetByUserIdAsync(userId.Value, cancellationToken);

        var dtos = new List<CampaignDto>();
        foreach (var campaign in campaigns)
        {
            var membership = await _campaignMemberRepository
                .GetByCampaignAndUserAsync(campaign.Id, userId.Value, cancellationToken);
            dtos.Add(MapToDto(campaign, membership?.Role));
        }

        return Ok(dtos);
    }

    /// <summary>
    /// Get a specific campaign by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CampaignDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var campaign = await _campaignRepository.GetByIdWithMembersAsync(id, cancellationToken);
        if (campaign == null)
            return NotFound();

        // Check if user is a member
        var isMember = await _campaignMemberRepository.IsMemberAsync(id, userId.Value, cancellationToken);
        if (!isMember)
            return Forbid("You are not a member of this campaign");

        var membership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(id, userId.Value, cancellationToken);
        var memberCount = await _campaignMemberRepository.GetMemberCountAsync(id, cancellationToken);

        return Ok(MapToDetailDto(campaign, membership?.Role, memberCount));
    }

    /// <summary>
    /// Create a new campaign (current user becomes Master)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CampaignDetailDto>> Create(
        [FromBody] CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        // Validate game system exists
        var gameSystem = await _dbContext.GameSystems
            .FirstOrDefaultAsync(gs => gs.Id == request.GameSystemId, cancellationToken);
        if (gameSystem == null)
            return BadRequest("Invalid game system");

        JsonDocument? settings = null;
        if (request.Settings != null)
        {
            settings = JsonDocument.Parse(JsonSerializer.Serialize(request.Settings));
        }

        var campaign = Campaign.Create(
            ownerId: userId.Value,
            gameSystemId: request.GameSystemId,
            name: request.Name,
            description: request.Description,
            settings: settings
        );

        await _campaignRepository.AddAsync(campaign, cancellationToken);

        // Add creator as Master
        var membership = CampaignMember.Create(campaign.Id, userId.Value, CampaignRole.Master);
        await _campaignMemberRepository.AddAsync(membership, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Campaign {CampaignId} created by user {UserId}", campaign.Id, userId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = campaign.Id },
            MapToDetailDto(campaign, CampaignRole.Master, 1));
    }

    /// <summary>
    /// Update campaign details (Master only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CampaignDetailDto>> Update(
        Guid id,
        [FromBody] UpdateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var campaign = await _campaignRepository.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
            return NotFound();

        // Only Masters can update campaigns
        var isMaster = await _campaignMemberRepository.IsMasterAsync(id, userId.Value, cancellationToken);
        if (!isMaster)
            return Forbid("Only campaign masters can update campaign details");

        JsonDocument? settings = null;
        if (request.Settings != null)
        {
            settings = JsonDocument.Parse(JsonSerializer.Serialize(request.Settings));
        }

        campaign.Update(request.Name, request.Description, settings);

        _campaignRepository.Update(campaign);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Campaign {CampaignId} updated by user {UserId}", id, userId);

        var memberCount = await _campaignMemberRepository.GetMemberCountAsync(id, cancellationToken);
        return Ok(MapToDetailDto(campaign, CampaignRole.Master, memberCount));
    }

    /// <summary>
    /// Delete a campaign (Owner only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var campaign = await _campaignRepository.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
            return NotFound();

        // Only owner can delete
        if (campaign.OwnerId != userId.Value)
            return Forbid("Only the campaign owner can delete the campaign");

        _campaignRepository.Delete(campaign);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Campaign {CampaignId} deleted by user {UserId}", id, userId);

        return NoContent();
    }

    /// <summary>
    /// Join a campaign using a join code
    /// </summary>
    [HttpPost("join")]
    [ProducesResponseType(typeof(CampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampaignDto>> JoinByCode(
        [FromBody] JoinCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var campaign = await _campaignRepository.GetByJoinCodeAsync(request.JoinCode, cancellationToken);
        if (campaign == null)
            return NotFound("Invalid join code");

        if (!campaign.IsActive)
            return BadRequest("This campaign is no longer accepting new members");

        // Check if already a member
        var existingMembership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(campaign.Id, userId.Value, cancellationToken);
        if (existingMembership != null)
            return BadRequest("You are already a member of this campaign");

        // Join as Player
        var membership = CampaignMember.Create(campaign.Id, userId.Value, CampaignRole.Player);
        await _campaignMemberRepository.AddAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} joined campaign {CampaignId}", userId, campaign.Id);

        return Ok(MapToDto(campaign, CampaignRole.Player));
    }

    /// <summary>
    /// Leave a campaign
    /// </summary>
    [HttpPost("{id:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var campaign = await _campaignRepository.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
            return NotFound();

        // Owner cannot leave (must delete or transfer ownership)
        if (campaign.OwnerId == userId.Value)
            return BadRequest("Campaign owner cannot leave. Transfer ownership or delete the campaign instead.");

        var membership = await _campaignMemberRepository
            .GetByCampaignAndUserAsync(id, userId.Value, cancellationToken);
        if (membership == null)
            return NotFound("You are not a member of this campaign");

        _campaignMemberRepository.Delete(membership);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} left campaign {CampaignId}", userId, id);

        return NoContent();
    }

    /// <summary>
    /// Get campaign members (Master only sees all, Players see limited info)
    /// </summary>
    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(IEnumerable<CampaignMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<CampaignMemberDto>>> GetMembers(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var campaign = await _campaignRepository.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
            return NotFound();

        var isMember = await _campaignMemberRepository.IsMemberAsync(id, userId.Value, cancellationToken);
        if (!isMember)
            return Forbid("You are not a member of this campaign");

        var members = await _dbContext.CampaignMembers
            .Include(cm => cm.User)
            .Where(cm => cm.CampaignId == id)
            .ToListAsync(cancellationToken);

        var dtos = members.Select(m => new CampaignMemberDto
        {
            Id = m.Id,
            UserId = m.UserId,
            DisplayName = m.User.DisplayName ?? m.User.Email,
            Role = m.Role,
            JoinedAt = m.JoinedAt
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Update a member's role (Master only)
    /// </summary>
    [HttpPatch("{campaignId:guid}/members/{memberId:guid}/role")]
    [ProducesResponseType(typeof(CampaignMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CampaignMemberDto>> UpdateMemberRole(
        Guid campaignId,
        Guid memberId,
        [FromBody] UpdateMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken);
        if (campaign == null)
            return NotFound("Campaign not found");

        // Only the owner can change roles
        if (campaign.OwnerId != userId.Value)
            return Forbid("Only the campaign owner can change member roles");

        var member = await _campaignMemberRepository.GetByIdAsync(memberId, cancellationToken);
        if (member == null || member.CampaignId != campaignId)
            return NotFound("Member not found");

        // Cannot change owner's role
        if (member.UserId == campaign.OwnerId)
            return BadRequest("Cannot change the campaign owner's role");

        member.ChangeRole(request.Role);
        _campaignMemberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == member.UserId, cancellationToken);

        return Ok(new CampaignMemberDto
        {
            Id = member.Id,
            UserId = member.UserId,
            DisplayName = user?.DisplayName ?? user?.Email ?? "Unknown",
            Role = member.Role,
            JoinedAt = member.JoinedAt
        });
    }

    /// <summary>
    /// Remove a member from the campaign (Master only)
    /// </summary>
    [HttpDelete("{campaignId:guid}/members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveMember(
        Guid campaignId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        // Only Masters can remove members
        var isMaster = await _campaignMemberRepository.IsMasterAsync(campaignId, userId.Value, cancellationToken);
        if (!isMaster)
            return Forbid("Only campaign masters can remove members");

        var member = await _campaignMemberRepository.GetByIdAsync(memberId, cancellationToken);
        if (member == null || member.CampaignId != campaignId)
            return NotFound("Member not found");

        var campaign = await _campaignRepository.GetByIdAsync(campaignId, cancellationToken);
        
        // Cannot remove the owner
        if (campaign != null && member.UserId == campaign.OwnerId)
            return BadRequest("Cannot remove the campaign owner");

        _campaignMemberRepository.Delete(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Member {MemberId} removed from campaign {CampaignId} by user {UserId}", 
            memberId, campaignId, userId);

        return NoContent();
    }

    /// <summary>
    /// Regenerate campaign join code (Master only)
    /// </summary>
    [HttpPost("{id:guid}/regenerate-code")]
    [ProducesResponseType(typeof(JoinCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<JoinCodeResponse>> RegenerateJoinCode(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var campaign = await _campaignRepository.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
            return NotFound();

        var isMaster = await _campaignMemberRepository.IsMasterAsync(id, userId.Value, cancellationToken);
        if (!isMaster)
            return Forbid("Only campaign masters can regenerate the join code");

        campaign.RegenerateJoinCode();
        _campaignRepository.Update(campaign);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Join code regenerated for campaign {CampaignId} by user {UserId}", id, userId);

        return Ok(new JoinCodeResponse { JoinCode = campaign.JoinCode });
    }

    /// <summary>
    /// Activate/Deactivate campaign (Master only)
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(CampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CampaignDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateCampaignStatusRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Unauthorized();

        var campaign = await _campaignRepository.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
            return NotFound();

        var isMaster = await _campaignMemberRepository.IsMasterAsync(id, userId.Value, cancellationToken);
        if (!isMaster)
            return Forbid("Only campaign masters can change campaign status");

        if (request.IsActive)
            campaign.Activate();
        else
            campaign.Deactivate();

        _campaignRepository.Update(campaign);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(campaign, CampaignRole.Master));
    }

    private static CampaignDto MapToDto(Campaign campaign, CampaignRole? userRole)
    {
        return new CampaignDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            GameSystemId = campaign.GameSystemId,
            IsActive = campaign.IsActive,
            UserRole = userRole,
            CreatedAt = campaign.CreatedAt
        };
    }

    private static CampaignDetailDto MapToDetailDto(Campaign campaign, CampaignRole? userRole, int memberCount)
    {
        return new CampaignDetailDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            OwnerId = campaign.OwnerId,
            GameSystemId = campaign.GameSystemId,
            JoinCode = userRole == CampaignRole.Master ? campaign.JoinCode : null, // Only Masters see join code
            IsActive = campaign.IsActive,
            Settings = DeserializeJsonDocument(campaign.Settings),
            UserRole = userRole,
            MemberCount = memberCount,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt
        };
    }

    private static Dictionary<string, object>? DeserializeJsonDocument(JsonDocument? doc)
    {
        if (doc == null) return null;
        return JsonSerializer.Deserialize<Dictionary<string, object>>(doc.RootElement.GetRawText());
    }
}

// DTOs
public record CampaignDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public Guid GameSystemId { get; init; }
    public bool IsActive { get; init; }
    public CampaignRole? UserRole { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CampaignDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public Guid OwnerId { get; init; }
    public Guid GameSystemId { get; init; }
    public string? JoinCode { get; init; } // Only visible to Masters
    public bool IsActive { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
    public CampaignRole? UserRole { get; init; }
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CampaignMemberDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = null!;
    public CampaignRole Role { get; init; }
    public DateTime JoinedAt { get; init; }
}

public record CreateCampaignRequest
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public Guid GameSystemId { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
}

public record UpdateCampaignRequest
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
}

public record JoinCampaignRequest
{
    public string JoinCode { get; init; } = null!;
}

public record UpdateMemberRoleRequest
{
    public CampaignRole Role { get; init; }
}

public record UpdateCampaignStatusRequest
{
    public bool IsActive { get; init; }
}

public record JoinCodeResponse
{
    public string JoinCode { get; init; } = null!;
}

using Loremaster.Application.Features.Campaigns.Commands.CreateCampaign;
using Loremaster.Application.Features.Campaigns.Commands.DeleteCampaign;
using Loremaster.Application.Features.Campaigns.Commands.JoinCampaign;
using Loremaster.Application.Features.Campaigns.Commands.LeaveCampaign;
using Loremaster.Application.Features.Campaigns.Commands.RegenerateJoinCode;
using Loremaster.Application.Features.Campaigns.Commands.RemoveMember;
using Loremaster.Application.Features.Campaigns.Commands.UpdateCampaign;
using Loremaster.Application.Features.Campaigns.Commands.UpdateCampaignStatus;
using Loremaster.Application.Features.Campaigns.Commands.UpdateMemberRole;
using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Application.Features.Campaigns.Queries.GetCampaignById;
using Loremaster.Application.Features.Campaigns.Queries.GetCampaignMembers;
using Loremaster.Application.Features.Campaigns.Queries.GetMyCampaigns;
using Loremaster.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

/// <summary>
/// Controller for managing campaigns (tabletop RPG game sessions).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CampaignsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all campaigns the current user is a member of.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CampaignDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CampaignDto>>> GetMyCampaigns(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyCampaignsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific campaign by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CampaignDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCampaignByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new campaign (current user becomes Master).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CampaignDetailDto>> Create(
        [FromBody] CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCampaignCommand(
            request.Name,
            request.GameSystemId,
            request.Description,
            request.Settings
        );
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update campaign details (Master only).
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
        var command = new UpdateCampaignCommand(
            id,
            request.Name,
            request.Description,
            request.Settings
        );
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a campaign (Owner only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCampaignCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Join a campaign using a join code.
    /// </summary>
    [HttpPost("join")]
    [ProducesResponseType(typeof(CampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampaignDto>> JoinByCode(
        [FromBody] JoinCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new JoinCampaignCommand(request.JoinCode), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Leave a campaign.
    /// </summary>
    [HttpPost("{id:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new LeaveCampaignCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get campaign members (Members only).
    /// </summary>
    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(IEnumerable<CampaignMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<CampaignMemberDto>>> GetMembers(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCampaignMembersQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update a member's role (Owner only).
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
        var command = new UpdateMemberRoleCommand(campaignId, memberId, request.Role);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Remove a member from the campaign (Master only).
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
        await _mediator.Send(new RemoveMemberCommand(campaignId, memberId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Regenerate campaign join code (Master only).
    /// </summary>
    [HttpPost("{id:guid}/regenerate-code")]
    [ProducesResponseType(typeof(JoinCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<JoinCodeDto>> RegenerateJoinCode(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RegenerateJoinCodeCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Activate/Deactivate campaign (Master only).
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
        var result = await _mediator.Send(new UpdateCampaignStatusCommand(id, request.IsActive), cancellationToken);
        return Ok(result);
    }
}

#region Request DTOs

/// <summary>
/// Request to create a new campaign.
/// </summary>
public record CreateCampaignRequest
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public Guid GameSystemId { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
}

/// <summary>
/// Request to update a campaign.
/// </summary>
public record UpdateCampaignRequest
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
}

/// <summary>
/// Request to join a campaign by code.
/// </summary>
public record JoinCampaignRequest
{
    public string JoinCode { get; init; } = null!;
}

/// <summary>
/// Request to update a member's role.
/// </summary>
public record UpdateMemberRoleRequest
{
    public CampaignRole Role { get; init; }
}

/// <summary>
/// Request to update campaign status.
/// </summary>
public record UpdateCampaignStatusRequest
{
    public bool IsActive { get; init; }
}

#endregion

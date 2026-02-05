using Loremaster.Application.Features.Admin.Commands.CreateUser;
using Loremaster.Application.Features.Admin.Commands.DeleteCampaign;
using Loremaster.Application.Features.Admin.Commands.DeleteUser;
using Loremaster.Application.Features.Admin.Commands.UpdateCampaign;
using Loremaster.Application.Features.Admin.Commands.UpdateUser;
using Loremaster.Application.Features.Admin.DTOs;
using Loremaster.Application.Features.Admin.Queries.GetAllCampaigns;
using Loremaster.Application.Features.Admin.Queries.GetAllUsers;
using Loremaster.Application.Features.Admin.Queries.GetCampaignById;
using Loremaster.Application.Features.Admin.Queries.GetUserById;
using Loremaster.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

/// <summary>
/// Controller for admin-only operations on users and campaigns.
/// All endpoints require Admin role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdminRole")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #region Users

    /// <summary>
    /// Get all users (Admin only).
    /// </summary>
    /// <param name="includeInactive">Include inactive users (default: false).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all users.</returns>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IEnumerable<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAllUsers(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllUsersQuery(includeInactive), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a user by ID (Admin only).
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User details.</returns>
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminUserDto>> GetUserById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new user (Admin only).
    /// </summary>
    /// <param name="request">User creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created user.</returns>
    [HttpPost("users")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminUserDto>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateUserCommand(
            Email: request.Email,
            Password: request.Password,
            DisplayName: request.DisplayName,
            Role: request.Role,
            IsActive: request.IsActive
        );

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update a user (Admin only).
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">User update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user.</returns>
    [HttpPut("users/{id:guid}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateUserCommand(
            UserId: id,
            Email: request.Email,
            Password: request.Password,
            DisplayName: request.DisplayName,
            Role: request.Role,
            IsActive: request.IsActive
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a user (Admin only). Performs soft delete.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteUserCommand(id), cancellationToken);
        return NoContent();
    }

    #endregion

    #region Campaigns

    /// <summary>
    /// Get all campaigns (Admin only).
    /// </summary>
    /// <param name="includeInactive">Include inactive campaigns (default: false).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all campaigns.</returns>
    [HttpGet("campaigns")]
    [ProducesResponseType(typeof(IEnumerable<AdminCampaignDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AdminCampaignDto>>> GetAllCampaigns(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllCampaignsQuery(includeInactive), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a campaign by ID (Admin only).
    /// </summary>
    /// <param name="id">Campaign ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Campaign details.</returns>
    [HttpGet("campaigns/{id:guid}")]
    [ProducesResponseType(typeof(AdminCampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminCampaignDto>> GetCampaignById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAdminCampaignByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update a campaign (Admin only).
    /// </summary>
    /// <param name="id">Campaign ID.</param>
    /// <param name="request">Campaign update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated campaign.</returns>
    [HttpPut("campaigns/{id:guid}")]
    [ProducesResponseType(typeof(AdminCampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminCampaignDto>> UpdateCampaign(
        Guid id,
        [FromBody] AdminUpdateCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateAdminCampaignCommand(
            CampaignId: id,
            Name: request.Name,
            Description: request.Description,
            IsActive: request.IsActive,
            OwnerId: request.OwnerId
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a campaign (Admin only). Performs soft delete.
    /// </summary>
    /// <param name="id">Campaign ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("campaigns/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCampaign(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteAdminCampaignCommand(id), cancellationToken);
        return NoContent();
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// Request to create a new user.
/// </summary>
public record CreateUserRequest
{
    /// <summary>User email address.</summary>
    public string Email { get; init; } = null!;

    /// <summary>User password.</summary>
    public string Password { get; init; } = null!;

    /// <summary>Optional display name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>User role (Player, Master, Admin).</summary>
    public UserRole Role { get; init; } = UserRole.Player;

    /// <summary>Whether the user is active (default: true).</summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Request to update a user.
/// </summary>
public record UpdateUserRequest
{
    /// <summary>New email (optional).</summary>
    public string? Email { get; init; }

    /// <summary>New password (optional).</summary>
    public string? Password { get; init; }

    /// <summary>New display name (optional).</summary>
    public string? DisplayName { get; init; }

    /// <summary>New role (optional).</summary>
    public UserRole? Role { get; init; }

    /// <summary>New active status (optional).</summary>
    public bool? IsActive { get; init; }
}

/// <summary>
/// Request to update a campaign (Admin).
/// </summary>
public record AdminUpdateCampaignRequest
{
    /// <summary>New campaign name (optional).</summary>
    public string? Name { get; init; }

    /// <summary>New description (optional).</summary>
    public string? Description { get; init; }

    /// <summary>New active status (optional).</summary>
    public bool? IsActive { get; init; }

    /// <summary>New owner ID (optional, for ownership transfer).</summary>
    public Guid? OwnerId { get; init; }
}

#endregion

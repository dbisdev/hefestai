using Loremaster.Application.Features.Admin.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Admin.Commands.UpdateCampaign;

/// <summary>
/// Command to update a campaign (Admin only).
/// </summary>
/// <param name="CampaignId">The campaign ID to update.</param>
/// <param name="Name">New campaign name (optional, null means no change).</param>
/// <param name="Description">New description (optional, null means no change).</param>
/// <param name="IsActive">New active status (optional, null means no change).</param>
/// <param name="OwnerId">New owner ID (optional, null means no change).</param>
public record UpdateAdminCampaignCommand(
    Guid CampaignId,
    string? Name,
    string? Description,
    bool? IsActive,
    Guid? OwnerId
) : IRequest<AdminCampaignDto>;

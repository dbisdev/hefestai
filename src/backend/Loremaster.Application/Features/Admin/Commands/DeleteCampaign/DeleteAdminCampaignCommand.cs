using MediatR;

namespace Loremaster.Application.Features.Admin.Commands.DeleteCampaign;

/// <summary>
/// Command to delete a campaign (Admin only).
/// Performs a soft delete.
/// </summary>
/// <param name="CampaignId">The campaign ID to delete.</param>
public record DeleteAdminCampaignCommand(Guid CampaignId) : IRequest<Unit>;

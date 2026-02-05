using Loremaster.Application.Features.Admin.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Admin.Queries.GetCampaignById;

/// <summary>
/// Query to retrieve a specific campaign by ID for admin management.
/// </summary>
/// <param name="CampaignId">The campaign ID to retrieve.</param>
public record GetAdminCampaignByIdQuery(Guid CampaignId) : IRequest<AdminCampaignDto>;

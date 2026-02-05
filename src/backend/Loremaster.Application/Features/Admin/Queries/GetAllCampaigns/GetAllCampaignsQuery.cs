using Loremaster.Application.Features.Admin.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Admin.Queries.GetAllCampaigns;

/// <summary>
/// Query to retrieve all campaigns for admin management.
/// Supports optional filtering by active status.
/// </summary>
/// <param name="IncludeInactive">If true, includes inactive campaigns in results.</param>
public record GetAllCampaignsQuery(bool IncludeInactive = false) : IRequest<IEnumerable<AdminCampaignDto>>;

using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.Campaigns.Commands.UpdateMemberRole;

/// <summary>
/// Command to update a member's role. Only the campaign Owner can change roles.
/// </summary>
/// <param name="CampaignId">The ID of the campaign.</param>
/// <param name="MemberId">The ID of the member (CampaignMember.Id, not UserId).</param>
/// <param name="NewRole">The new role to assign (Player or Master).</param>
public record UpdateMemberRoleCommand(
    Guid CampaignId, 
    Guid MemberId, 
    CampaignRole NewRole
) : IRequest<CampaignMemberDto>;

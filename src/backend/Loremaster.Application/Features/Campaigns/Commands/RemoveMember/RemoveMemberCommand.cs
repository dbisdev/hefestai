using MediatR;

namespace Loremaster.Application.Features.Campaigns.Commands.RemoveMember;

/// <summary>
/// Command to remove a member from a campaign. Only Masters can remove members.
/// The Owner cannot be removed.
/// </summary>
/// <param name="CampaignId">The ID of the campaign.</param>
/// <param name="MemberId">The ID of the member to remove (CampaignMember.Id, not UserId).</param>
public record RemoveMemberCommand(Guid CampaignId, Guid MemberId) : IRequest<Unit>;

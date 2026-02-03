using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Campaigns.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Application.Features.Campaigns.Commands.UpdateMemberRole;

/// <summary>
/// Handler for UpdateMemberRoleCommand. Updates a member's role (Owner only).
/// </summary>
public class UpdateMemberRoleCommandHandler : IRequestHandler<UpdateMemberRoleCommand, CampaignMemberDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICampaignRepository _campaignRepository;
    private readonly ICampaignMemberRepository _campaignMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMemberRoleCommandHandler(
        IApplicationDbContext dbContext,
        ICampaignRepository campaignRepository,
        ICampaignMemberRepository campaignMemberRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _campaignRepository = campaignRepository;
        _campaignMemberRepository = campaignMemberRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Handles the update member role command.
    /// </summary>
    /// <param name="request">The update member role command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated member information.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when user is not authenticated or not the owner.</exception>
    /// <exception cref="NotFoundException">Thrown when campaign or member doesn't exist.</exception>
    /// <exception cref="DomainException">Thrown when trying to change the owner's role.</exception>
    public async Task<CampaignMemberDto> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated to update member roles");
        }

        var userId = _currentUserService.UserId.Value;

        var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            throw new NotFoundException("Campaign", request.CampaignId);
        }

        // Only the owner can change roles
        if (campaign.OwnerId != userId)
        {
            throw new ForbiddenAccessException("Only the campaign owner can change member roles");
        }

        var member = await _campaignMemberRepository.GetByIdAsync(request.MemberId, cancellationToken);
        if (member == null || member.CampaignId != request.CampaignId)
        {
            throw new NotFoundException("Member", request.MemberId);
        }

        // Cannot change owner's role
        if (member.UserId == campaign.OwnerId)
        {
            throw new DomainException("Cannot change the campaign owner's role");
        }

        member.ChangeRole(request.NewRole);
        _campaignMemberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Load user info for the DTO
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == member.UserId, cancellationToken);

        return new CampaignMemberDto
        {
            Id = member.Id,
            UserId = member.UserId,
            DisplayName = user?.DisplayName ?? user?.Email ?? "Unknown",
            Role = member.Role,
            JoinedAt = member.JoinedAt
        };
    }
}

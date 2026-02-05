using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Admin.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Admin.Queries.GetAllUsers;

/// <summary>
/// Handler for GetAllUsersQuery. Returns all users for admin management.
/// </summary>
public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IEnumerable<AdminUserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Handles the get all users query.
    /// </summary>
    /// <param name="request">The query with optional filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of users as AdminUserDto.</returns>
    public async Task<IEnumerable<AdminUserDto>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = request.IncludeInactive
            ? await _userRepository.GetAllAsync(cancellationToken)
            : await _userRepository.GetAllActiveAsync(cancellationToken);

        return users.Select(user => new AdminUserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            OwnedCampaignsCount = user.OwnedCampaigns.Count,
            CampaignMembershipsCount = user.CampaignMemberships.Count
        });
    }
}

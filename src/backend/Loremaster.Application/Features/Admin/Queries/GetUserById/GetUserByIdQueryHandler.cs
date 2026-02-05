using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Admin.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Admin.Queries.GetUserById;

/// <summary>
/// Handler for GetUserByIdQuery. Returns a specific user for admin management.
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, AdminUserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Handles the get user by ID query.
    /// </summary>
    /// <param name="request">The query containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User as AdminUserDto.</returns>
    /// <exception cref="NotFoundException">Thrown when user not found.</exception>
    public async Task<AdminUserDto> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        return new AdminUserDto
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
        };
    }
}

using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Admin.DTOs;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Admin.Commands.UpdateUser;

/// <summary>
/// Handler for UpdateUserCommand. Updates an existing user.
/// </summary>
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, AdminUserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Handles the update user command.
    /// </summary>
    /// <param name="request">The command with updated user data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user as AdminUserDto.</returns>
    /// <exception cref="NotFoundException">Thrown when user not found.</exception>
    /// <exception cref="DomainException">Thrown when email already exists.</exception>
    public async Task<AdminUserDto> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Update email if provided and different
        if (!string.IsNullOrEmpty(request.Email) && 
            !request.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
            if (emailExists)
            {
                throw new DomainException("A user with this email already exists");
            }
            user.UpdateEmail(request.Email);
        }

        // Update password if provided
        if (!string.IsNullOrEmpty(request.Password))
        {
            var passwordHash = _passwordHasher.HashPassword(request.Password);
            user.UpdatePassword(passwordHash);
        }

        // Update display name if provided (can be set to empty string)
        if (request.DisplayName != null)
        {
            user.Update(request.DisplayName, user.AvatarUrl);
        }

        // Update role if provided
        if (request.Role.HasValue)
        {
            user.ChangeRole(request.Role.Value);
        }

        // Update active status if provided
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                user.Activate();
            else
                user.Deactivate();
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

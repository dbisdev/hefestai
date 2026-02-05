using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Features.Admin.DTOs;
using Loremaster.Domain.Entities;
using Loremaster.Domain.Exceptions;
using MediatR;

namespace Loremaster.Application.Features.Admin.Commands.CreateUser;

/// <summary>
/// Handler for CreateUserCommand. Creates a new user in the system.
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, AdminUserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Handles the create user command.
    /// </summary>
    /// <param name="request">The command with user data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created user as AdminUserDto.</returns>
    /// <exception cref="DomainException">Thrown when email already exists.</exception>
    public async Task<AdminUserDto> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Check if email already exists
        var emailExists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            throw new DomainException("A user with this email already exists");
        }

        // Hash the password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create the user
        var user = User.Create(
            request.Email,
            passwordHash,
            request.DisplayName,
            request.Role
        );

        // Set active status if specified as false
        if (!request.IsActive)
        {
            user.Deactivate();
        }

        await _userRepository.AddAsync(user, cancellationToken);
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
            OwnedCampaignsCount = 0,
            CampaignMembershipsCount = 0
        };
    }
}

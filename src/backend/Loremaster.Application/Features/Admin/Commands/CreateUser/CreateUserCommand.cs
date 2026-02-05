using Loremaster.Application.Features.Admin.DTOs;
using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.Admin.Commands.CreateUser;

/// <summary>
/// Command to create a new user (Admin only).
/// </summary>
/// <param name="Email">User email address.</param>
/// <param name="Password">User password.</param>
/// <param name="DisplayName">Optional display name.</param>
/// <param name="Role">User role (Player, Master, Admin).</param>
/// <param name="IsActive">Whether the user is active (defaults to true).</param>
public record CreateUserCommand(
    string Email,
    string Password,
    string? DisplayName,
    UserRole Role,
    bool IsActive = true
) : IRequest<AdminUserDto>;

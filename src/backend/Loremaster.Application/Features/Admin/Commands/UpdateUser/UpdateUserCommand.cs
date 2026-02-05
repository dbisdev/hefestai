using Loremaster.Application.Features.Admin.DTOs;
using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.Admin.Commands.UpdateUser;

/// <summary>
/// Command to update an existing user (Admin only).
/// </summary>
/// <param name="UserId">The user ID to update.</param>
/// <param name="Email">New email (optional, null means no change).</param>
/// <param name="Password">New password (optional, null means no change).</param>
/// <param name="DisplayName">New display name (optional, null means no change).</param>
/// <param name="Role">New role (optional, null means no change).</param>
/// <param name="IsActive">New active status (optional, null means no change).</param>
public record UpdateUserCommand(
    Guid UserId,
    string? Email,
    string? Password,
    string? DisplayName,
    UserRole? Role,
    bool? IsActive
) : IRequest<AdminUserDto>;

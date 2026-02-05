using Loremaster.Application.Features.Admin.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Admin.Queries.GetUserById;

/// <summary>
/// Query to retrieve a specific user by ID for admin management.
/// </summary>
/// <param name="UserId">The user ID to retrieve.</param>
public record GetUserByIdQuery(Guid UserId) : IRequest<AdminUserDto>;

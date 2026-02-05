using Loremaster.Application.Features.Admin.DTOs;
using MediatR;

namespace Loremaster.Application.Features.Admin.Queries.GetAllUsers;

/// <summary>
/// Query to retrieve all users for admin management.
/// Supports optional filtering by active status.
/// </summary>
/// <param name="IncludeInactive">If true, includes inactive users in results.</param>
public record GetAllUsersQuery(bool IncludeInactive = false) : IRequest<IEnumerable<AdminUserDto>>;

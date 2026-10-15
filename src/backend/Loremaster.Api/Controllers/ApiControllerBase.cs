using Loremaster.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Loremaster.Api.Controllers;

/// <summary>
/// Base class for all API controllers.
/// Provides common helpers for extracting authenticated user information from JWT claims,
/// avoiding duplication across controllers.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Extracts the current authenticated user's ID from the JWT "sub" claim.
    /// Falls back to <see cref="ClaimTypes.NameIdentifier"/> for compatibility.
    /// </summary>
    /// <returns>The user's <see cref="Guid"/>.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the token is missing or the claim cannot be parsed as a GUID.</exception>
    protected Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user token");

        return userId;
    }

    /// <summary>
    /// Determines whether the current authenticated user has the Admin role.
    /// Checks the JWT "role" claim, falling back to <see cref="ClaimTypes.Role"/>.
    /// </summary>
    /// <returns><c>true</c> if the user has the Admin role; otherwise, <c>false</c>.</returns>
    protected bool IsCurrentUserAdmin()
    {
        var roleClaim = User.FindFirst("role")?.Value
            ?? User.FindFirst(ClaimTypes.Role)?.Value;

        return roleClaim == "Admin";
    }

    /// <summary>
    /// Returns the current authenticated user's role as a <see cref="UserRole"/> enum value.
    /// Checks the JWT "role" claim, falling back to <see cref="ClaimTypes.Role"/>.
    /// Defaults to <see cref="UserRole.Player"/> when the claim is absent or unrecognised.
    /// </summary>
    /// <returns>The user's <see cref="UserRole"/>.</returns>
    protected UserRole GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst("role")?.Value
            ?? User.FindFirst(ClaimTypes.Role)?.Value;

        return roleClaim switch
        {
            "Admin" => UserRole.Admin,
            "Master" => UserRole.Master,
            _ => UserRole.Player
        };
    }
}

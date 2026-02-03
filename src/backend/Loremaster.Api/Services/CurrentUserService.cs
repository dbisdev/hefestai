using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Loremaster.Application.Common.Interfaces;

namespace Loremaster.Api.Services;

/// <summary>
/// Service to access the current authenticated user's information from JWT claims.
/// Note: With MapInboundClaims = false in JWT configuration, claims are not mapped
/// to ClaimTypes URIs, so we need to use the original JWT claim names.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            // Try JWT standard claim "sub" first (when MapInboundClaims = false)
            // Fall back to ClaimTypes.NameIdentifier for compatibility
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
    
    /// <summary>
    /// Gets the current user's email from JWT claims.
    /// </summary>
    public string? Email => 
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
    
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

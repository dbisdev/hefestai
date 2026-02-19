namespace Loremaster.Application.Features.Auth.Queries.GetCurrentUser;

public record CurrentUserResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

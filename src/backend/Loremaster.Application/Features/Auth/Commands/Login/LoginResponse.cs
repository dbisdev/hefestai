namespace Loremaster.Application.Features.Auth.Commands.Login;

public record LoginResponse(
    Guid UserId,
    string Email,
    string? DisplayName,
    string Role,
    string AccessToken,
    string RefreshToken
);

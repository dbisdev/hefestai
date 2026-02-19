namespace Loremaster.Application.Features.Auth.Commands.Register;

public record RegisterResponse(
    Guid UserId,
    string Email,
    string? DisplayName,
    string Role,
    string AccessToken,
    string RefreshToken
);

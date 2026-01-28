using MediatR;

namespace Loremaster.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string? DisplayName,
    string? Role,
    string? InviteCode
) : IRequest<RegisterResponse>;

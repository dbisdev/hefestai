using Loremaster.Application.Features.LoreEntities.DTOs;
using Loremaster.Domain.Enums;
using MediatR;

namespace Loremaster.Application.Features.LoreEntities.Commands.TransferEntityOwnership;

/// <summary>
/// Command to transfer ownership of a lore entity to another campaign member.
/// Only the campaign master can transfer ownership of entities.
/// </summary>
/// <param name="CampaignId">The campaign the entity belongs to.</param>
/// <param name="EntityId">The ID of the entity to transfer.</param>
/// <param name="NewOwnerId">The ID of the new owner (must be a campaign member).</param>
/// <param name="NewOwnershipType">Optional new ownership type (defaults to Player for player transfers, Master for master transfers).</param>
public record TransferEntityOwnershipCommand(
    Guid CampaignId,
    Guid EntityId,
    Guid NewOwnerId,
    OwnershipType? NewOwnershipType = null
) : IRequest<LoreEntityDto>;

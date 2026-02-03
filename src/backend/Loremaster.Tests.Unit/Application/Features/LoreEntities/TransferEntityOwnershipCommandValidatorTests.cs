using FluentValidation.TestHelper;
using Loremaster.Application.Features.LoreEntities.Commands.TransferEntityOwnership;
using Loremaster.Domain.Enums;

namespace Loremaster.Tests.Unit.Application.Features.LoreEntities;

/// <summary>
/// Unit tests for TransferEntityOwnershipCommandValidator.
/// Tests validation rules for ownership transfer.
/// </summary>
public class TransferEntityOwnershipCommandValidatorTests
{
    private readonly TransferEntityOwnershipCommandValidator _validator;
    private readonly Guid _validCampaignId = Guid.NewGuid();
    private readonly Guid _validEntityId = Guid.NewGuid();
    private readonly Guid _validNewOwnerId = Guid.NewGuid();

    public TransferEntityOwnershipCommandValidatorTests()
    {
        _validator = new TransferEntityOwnershipCommandValidator();
    }

    #region Valid Commands

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new TransferEntityOwnershipCommand(
            _validCampaignId,
            _validEntityId,
            _validNewOwnerId);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidDataAndExplicitOwnershipType_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new TransferEntityOwnershipCommand(
            _validCampaignId,
            _validEntityId,
            _validNewOwnerId,
            OwnershipType.Player);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(OwnershipType.Master)]
    [InlineData(OwnershipType.Player)]
    [InlineData(OwnershipType.Shared)]
    public void Validate_WithAllOwnershipTypes_ShouldNotHaveErrors(OwnershipType ownershipType)
    {
        // Arrange
        var command = new TransferEntityOwnershipCommand(
            _validCampaignId,
            _validEntityId,
            _validNewOwnerId,
            ownershipType);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region CampaignId Validation

    [Fact]
    public void Validate_WithEmptyCampaignId_ShouldHaveError()
    {
        // Arrange
        var command = new TransferEntityOwnershipCommand(
            Guid.Empty,
            _validEntityId,
            _validNewOwnerId);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CampaignId)
            .WithErrorMessage("Campaign ID is required");
    }

    #endregion

    #region EntityId Validation

    [Fact]
    public void Validate_WithEmptyEntityId_ShouldHaveError()
    {
        // Arrange
        var command = new TransferEntityOwnershipCommand(
            _validCampaignId,
            Guid.Empty,
            _validNewOwnerId);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EntityId)
            .WithErrorMessage("Entity ID is required");
    }

    #endregion

    #region NewOwnerId Validation

    [Fact]
    public void Validate_WithEmptyNewOwnerId_ShouldHaveError()
    {
        // Arrange
        var command = new TransferEntityOwnershipCommand(
            _validCampaignId,
            _validEntityId,
            Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewOwnerId)
            .WithErrorMessage("New owner ID is required");
    }

    #endregion

    #region Multiple Validation Errors

    [Fact]
    public void Validate_WithAllEmptyIds_ShouldHaveMultipleErrors()
    {
        // Arrange
        var command = new TransferEntityOwnershipCommand(
            Guid.Empty,
            Guid.Empty,
            Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CampaignId);
        result.ShouldHaveValidationErrorFor(x => x.EntityId);
        result.ShouldHaveValidationErrorFor(x => x.NewOwnerId);
    }

    #endregion
}

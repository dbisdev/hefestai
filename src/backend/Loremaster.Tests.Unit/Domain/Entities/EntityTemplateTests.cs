using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;

namespace Loremaster.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit tests for the EntityTemplate domain entity.
/// Tests creation, status workflow, field definitions, and validation (EPIC 4 - Entity Definitions).
/// </summary>
public class EntityTemplateTests
{
    private readonly Guid _gameSystemId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _confirmerId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateTemplate()
    {
        // Arrange & Act
        var template = EntityTemplate.Create(
            entityTypeName: "Character",
            displayName: "Character",
            gameSystemId: _gameSystemId,
            ownerId: _ownerId,
            description: "Player and NPC characters");

        // Assert
        template.EntityTypeName.Should().Be("character"); // Normalized
        template.DisplayName.Should().Be("Character");
        template.Description.Should().Be("Player and NPC characters");
        template.GameSystemId.Should().Be(_gameSystemId);
        template.OwnerId.Should().Be(_ownerId);
        template.Status.Should().Be(TemplateStatus.Draft);
        template.FieldDefinitionsJson.Should().Be("[]");
        template.ConfirmedAt.Should().BeNull();
        template.ConfirmedByUserId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldNormalizeEntityTypeName()
    {
        // Arrange & Act
        var template = EntityTemplate.Create(
            entityTypeName: "Player Character",
            displayName: "Player Character",
            gameSystemId: _gameSystemId,
            ownerId: _ownerId);

        // Assert - spaces become underscores, lowercase
        template.EntityTypeName.Should().Be("player_character");
    }

    [Fact]
    public void Create_ShouldNormalizeHyphensToUnderscores()
    {
        // Arrange & Act
        var template = EntityTemplate.Create(
            entityTypeName: "Non-Player-Character",
            displayName: "NPC",
            gameSystemId: _gameSystemId,
            ownerId: _ownerId);

        // Assert
        template.EntityTypeName.Should().Be("non_player_character");
    }

    [Fact]
    public void Create_ShouldTrimValues()
    {
        // Arrange & Act
        var template = EntityTemplate.Create(
            entityTypeName: "  Character  ",
            displayName: "  Character  ",
            gameSystemId: _gameSystemId,
            ownerId: _ownerId,
            description: "  Description  ");

        // Assert
        template.DisplayName.Should().Be("Character");
        template.Description.Should().Be("Description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEntityTypeName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        var act = () => EntityTemplate.Create(
            entityTypeName: invalidName!,
            displayName: "Display",
            gameSystemId: _gameSystemId,
            ownerId: _ownerId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("entityTypeName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDisplayName_ShouldThrowArgumentException(string? invalidDisplayName)
    {
        // Act
        var act = () => EntityTemplate.Create(
            entityTypeName: "character",
            displayName: invalidDisplayName!,
            gameSystemId: _gameSystemId,
            ownerId: _ownerId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("displayName");
    }

    [Fact]
    public void Create_WithOptionalFields_ShouldSetThem()
    {
        // Arrange
        var sourceDocId = Guid.NewGuid();

        // Act
        var template = EntityTemplate.Create(
            entityTypeName: "Vehicle",
            displayName: "Vehicle",
            gameSystemId: _gameSystemId,
            ownerId: _ownerId,
            sourceDocumentId: sourceDocId,
            version: "v2.0",
            iconHint: "car-icon");

        // Assert
        template.SourceDocumentId.Should().Be(sourceDocId);
        template.Version.Should().Be("v2.0");
        template.IconHint.Should().Be("car-icon");
    }

    #endregion

    #region NormalizeEntityTypeName Tests

    [Theory]
    [InlineData("Character", "character")]
    [InlineData("VEHICLE", "vehicle")]
    [InlineData("Player Character", "player_character")]
    [InlineData("Non-Player-Character", "non_player_character")]
    [InlineData("  Trimmed  ", "trimmed")]
    [InlineData("MixedCase Name", "mixedcase_name")]
    public void NormalizeEntityTypeName_ShouldNormalizeCorrectly(string input, string expected)
    {
        // Act
        var result = EntityTemplate.NormalizeEntityTypeName(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeEntityTypeName_WithEmptyInput_ShouldThrowArgumentException(string? invalidInput)
    {
        // Act
        var act = () => EntityTemplate.NormalizeEntityTypeName(invalidInput!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Field Definition Tests

    [Fact]
    public void SetFieldDefinitions_InDraftStatus_ShouldSucceed()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        var fields = new[]
        {
            FieldDefinition.Text("name", "Name", isRequired: true),
            FieldDefinition.Number("level", "Level", minValue: 1, maxValue: 20)
        };

        // Act
        template.SetFieldDefinitions(fields);

        // Assert
        template.GetFieldDefinitions().Should().HaveCount(2);
    }

    [Fact]
    public void SetFieldDefinitions_WithDuplicateNames_ShouldThrowArgumentException()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        var fields = new[]
        {
            FieldDefinition.Text("name", "Name"),
            FieldDefinition.Text("NAME", "Full Name") // Duplicate (case insensitive)
        };

        // Act
        var act = () => template.SetFieldDefinitions(fields);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Duplicate*");
    }

    [Fact]
    public void SetFieldDefinitions_OnConfirmedTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = CreateConfirmedTemplate();
        var fields = new[] { FieldDefinition.Text("newField", "New Field") };

        // Act
        var act = () => template.SetFieldDefinitions(fields);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*confirmed*");
    }

    [Fact]
    public void SetFieldDefinitions_OnRejectedTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });
        template.Reject("Not good");
        var fields = new[] { FieldDefinition.Text("newField", "New Field") };

        // Act
        var act = () => template.SetFieldDefinitions(fields);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*rejected*");
    }

    [Fact]
    public void GetFieldDefinitions_ShouldReturnOrderedFields()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        var fields = new[]
        {
            FieldDefinition.Create("third", "Third", FieldType.Text, order: 3),
            FieldDefinition.Create("first", "First", FieldType.Text, order: 1),
            FieldDefinition.Create("second", "Second", FieldType.Text, order: 2)
        };

        // Act
        template.SetFieldDefinitions(fields);
        var result = template.GetFieldDefinitions();

        // Assert
        result[0].Name.Should().Be("first");
        result[1].Name.Should().Be("second");
        result[2].Name.Should().Be("third");
    }

    [Fact]
    public void AddField_ShouldAddToExistingFields()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });

        // Act
        template.AddField(FieldDefinition.Number("level", "Level"));

        // Assert
        template.GetFieldDefinitions().Should().HaveCount(2);
    }

    [Fact]
    public void AddField_WithDuplicateName_ShouldThrowArgumentException()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });

        // Act
        var act = () => template.AddField(FieldDefinition.Text("name", "Different Name"));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void RemoveField_ShouldRemoveField()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[]
        {
            FieldDefinition.Text("name", "Name"),
            FieldDefinition.Number("level", "Level")
        });

        // Act
        template.RemoveField("level");

        // Assert
        template.GetFieldDefinitions().Should().HaveCount(1);
        template.GetFieldDefinitions().Should().NotContain(f => f.Name == "level");
    }

    [Fact]
    public void RemoveField_NonExistent_ShouldThrowArgumentException()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });

        // Act
        var act = () => template.RemoveField("nonexistent");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region Status Workflow Tests

    [Fact]
    public void SubmitForReview_FromDraft_ShouldChangeToPendingReview()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });

        // Act
        template.SubmitForReview();

        // Assert
        template.Status.Should().Be(TemplateStatus.PendingReview);
    }

    [Fact]
    public void SubmitForReview_WithoutFields_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);

        // Act
        var act = () => template.SubmitForReview();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*field definitions*");
    }

    [Fact]
    public void SubmitForReview_FromPendingReview_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });
        template.SubmitForReview();

        // Act
        var act = () => template.SubmitForReview();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Confirm_FromDraft_ShouldChangeToConfirmed()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });

        // Act
        template.Confirm(_confirmerId, "Looks good!");

        // Assert
        template.Status.Should().Be(TemplateStatus.Confirmed);
        template.ConfirmedAt.Should().NotBeNull();
        template.ConfirmedByUserId.Should().Be(_confirmerId);
        template.ReviewNotes.Should().Be("Looks good!");
    }

    [Fact]
    public void Confirm_FromPendingReview_ShouldChangeToConfirmed()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });
        template.SubmitForReview();

        // Act
        template.Confirm(_confirmerId);

        // Assert
        template.Status.Should().Be(TemplateStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WithoutFields_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);

        // Act
        var act = () => template.Confirm(_confirmerId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*field definitions*");
    }

    [Fact]
    public void Confirm_FromConfirmed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = CreateConfirmedTemplate();

        // Act
        var act = () => template.Confirm(_confirmerId);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_FromDraft_ShouldChangeToRejected()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });

        // Act
        template.Reject("Needs more fields");

        // Assert
        template.Status.Should().Be(TemplateStatus.Rejected);
        template.ReviewNotes.Should().Be("Needs more fields");
    }

    [Fact]
    public void Reject_FromPendingReview_ShouldChangeToRejected()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });
        template.SubmitForReview();

        // Act
        template.Reject("Missing required attributes");

        // Assert
        template.Status.Should().Be(TemplateStatus.Rejected);
    }

    [Fact]
    public void RevertToDraft_FromConfirmed_ShouldResetToDraft()
    {
        // Arrange
        var template = CreateConfirmedTemplate();

        // Act
        template.RevertToDraft();

        // Assert
        template.Status.Should().Be(TemplateStatus.Draft);
        template.ConfirmedAt.Should().BeNull();
        template.ConfirmedByUserId.Should().BeNull();
    }

    [Fact]
    public void RevertToDraft_FromRejected_ShouldResetToDraft()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });
        template.Reject("Bad");

        // Act
        template.RevertToDraft();

        // Assert
        template.Status.Should().Be(TemplateStatus.Draft);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_InDraftStatus_ShouldUpdateProperties()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);

        // Act
        template.Update(
            displayName: "Updated Character",
            description: "Updated description",
            iconHint: "new-icon",
            version: "v2.0");

        // Assert
        template.DisplayName.Should().Be("Updated Character");
        template.Description.Should().Be("Updated description");
        template.IconHint.Should().Be("new-icon");
        template.Version.Should().Be("v2.0");
    }

    [Fact]
    public void Update_OnConfirmedTemplate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var template = CreateConfirmedTemplate();

        // Act
        var act = () => template.Update("New Name");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*confirmed*");
    }

    [Fact]
    public void Update_WithEmptyDisplayName_ShouldThrowArgumentException()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);

        // Act
        var act = () => template.Update(displayName: "");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region CanBeUsedForEntityCreation Tests

    [Fact]
    public void CanBeUsedForEntityCreation_WhenDraft_ShouldBeFalse()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);

        // Assert
        template.CanBeUsedForEntityCreation.Should().BeFalse();
    }

    [Fact]
    public void CanBeUsedForEntityCreation_WhenPendingReview_ShouldBeFalse()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });
        template.SubmitForReview();

        // Assert
        template.CanBeUsedForEntityCreation.Should().BeFalse();
    }

    [Fact]
    public void CanBeUsedForEntityCreation_WhenConfirmed_ShouldBeTrue()
    {
        // Arrange
        var template = CreateConfirmedTemplate();

        // Assert
        template.CanBeUsedForEntityCreation.Should().BeTrue();
    }

    [Fact]
    public void CanBeUsedForEntityCreation_WhenRejected_ShouldBeFalse()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[] { FieldDefinition.Text("name", "Name") });
        template.Reject();

        // Assert
        template.CanBeUsedForEntityCreation.Should().BeFalse();
    }

    #endregion

    #region IsOwnedBy Tests

    [Fact]
    public void IsOwnedBy_WithCorrectOwner_ShouldReturnTrue()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);

        // Assert
        template.IsOwnedBy(_ownerId).Should().BeTrue();
    }

    [Fact]
    public void IsOwnedBy_WithDifferentOwner_ShouldReturnFalse()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        var differentUserId = Guid.NewGuid();

        // Assert
        template.IsOwnedBy(differentUserId).Should().BeFalse();
    }

    #endregion

    #region ValidateEntityAttributes Tests

    [Fact]
    public void ValidateEntityAttributes_WithValidAttributes_ShouldReturnValid()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[]
        {
            FieldDefinition.Text("name", "Name", isRequired: true),
            FieldDefinition.Number("level", "Level", minValue: 1, maxValue: 20)
        });
        template.Confirm(_confirmerId);

        var attributes = new Dictionary<string, object?>
        {
            { "name", "Hero" },
            { "level", 10 }
        };

        // Act
        var result = template.ValidateEntityAttributes(attributes);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateEntityAttributes_MissingRequiredField_ShouldReturnInvalid()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[]
        {
            FieldDefinition.Text("name", "Name", isRequired: true)
        });
        template.Confirm(_confirmerId);

        var attributes = new Dictionary<string, object?>();

        // Act
        var result = template.ValidateEntityAttributes(attributes);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Name") && e.Contains("missing"));
    }

    [Fact]
    public void ValidateEntityAttributes_WithInvalidNumberValue_ShouldReturnInvalid()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[]
        {
            FieldDefinition.Number("level", "Level", minValue: 1, maxValue: 20)
        });
        template.Confirm(_confirmerId);

        var attributes = new Dictionary<string, object?>
        {
            { "level", 25 } // Exceeds max
        };

        // Act
        var result = template.ValidateEntityAttributes(attributes);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Level") && e.Contains("invalid"));
    }

    [Fact]
    public void ValidateEntityAttributes_WithUnknownField_ShouldIgnoreAndReturnValid()
    {
        // Arrange
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[]
        {
            FieldDefinition.Text("name", "Name")
        });
        template.Confirm(_confirmerId);

        var attributes = new Dictionary<string, object?>
        {
            { "name", "Hero" },
            { "unknownField", "value" } // Extra field not in template
        };

        // Act
        var result = template.ValidateEntityAttributes(attributes);

        // Assert
        result.IsValid.Should().BeTrue(); // Unknown fields are ignored, not errors
    }

    #endregion

    #region Complete Workflow Tests

    [Fact]
    public void Template_CompleteWorkflow_ShouldSucceed()
    {
        // Create template
        var template = EntityTemplate.Create(
            "character",
            "Character",
            _gameSystemId,
            _ownerId,
            description: "A playable character");

        template.Status.Should().Be(TemplateStatus.Draft);
        template.CanBeUsedForEntityCreation.Should().BeFalse();

        // Add fields
        template.SetFieldDefinitions(new[]
        {
            FieldDefinition.Text("name", "Name", isRequired: true, order: 1),
            FieldDefinition.Select("race", "Race", new[] { "Human", "Elf" }, order: 2),
            FieldDefinition.Number("level", "Level", minValue: 1, maxValue: 20, order: 3)
        });

        template.GetFieldDefinitions().Should().HaveCount(3);

        // Submit for review
        template.SubmitForReview();
        template.Status.Should().Be(TemplateStatus.PendingReview);
        template.CanBeUsedForEntityCreation.Should().BeFalse();

        // Confirm
        template.Confirm(_confirmerId, "Approved after review");
        template.Status.Should().Be(TemplateStatus.Confirmed);
        template.CanBeUsedForEntityCreation.Should().BeTrue();
        template.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Validate attributes
        var validAttributes = new Dictionary<string, object?>
        {
            { "name", "Aragorn" },
            { "race", "Human" },
            { "level", 15 }
        };
        template.ValidateEntityAttributes(validAttributes).IsValid.Should().BeTrue();

        // Revert to draft for modifications
        template.RevertToDraft();
        template.Status.Should().Be(TemplateStatus.Draft);
        template.CanBeUsedForEntityCreation.Should().BeFalse();

        // Update and re-confirm
        template.Update("Updated Character", description: "Updated description");
        template.Confirm(_confirmerId);
        template.CanBeUsedForEntityCreation.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private EntityTemplate CreateConfirmedTemplate()
    {
        var template = EntityTemplate.Create("character", "Character", _gameSystemId, _ownerId);
        template.SetFieldDefinitions(new[]
        {
            FieldDefinition.Text("name", "Name", isRequired: true)
        });
        template.Confirm(_confirmerId);
        return template;
    }

    #endregion
}

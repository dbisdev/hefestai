using Loremaster.Domain.Enums;
using Loremaster.Domain.ValueObjects;

namespace Loremaster.Tests.Unit.Domain.ValueObjects;

/// <summary>
/// Unit tests for the FieldDefinition value object.
/// Tests creation, validation, and equality (EPIC 4 - Entity Definitions).
/// </summary>
public class FieldDefinitionTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateFieldDefinition()
    {
        // Arrange & Act
        var field = FieldDefinition.Create(
            name: "strength",
            displayName: "Strength",
            fieldType: FieldType.Number,
            isRequired: true,
            description: "Physical strength attribute");

        // Assert
        field.Name.Should().Be("strength");
        field.DisplayName.Should().Be("Strength");
        field.FieldType.Should().Be(FieldType.Number);
        field.IsRequired.Should().BeTrue();
        field.Description.Should().Be("Physical strength attribute");
    }

    [Fact]
    public void Create_ShouldTrimStrings()
    {
        // Arrange & Act
        // Note: Field name is trimmed first, then validated, so spaces at edges are ok
        // but the validation rejects names with internal spaces
        var field = FieldDefinition.Create(
            name: "fieldName",
            displayName: "  Display Name  ",
            fieldType: FieldType.Text,
            description: "  Description  ");

        // Assert
        field.Name.Should().Be("fieldName");
        field.DisplayName.Should().Be("Display Name");
        field.Description.Should().Be("Description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        var act = () => FieldDefinition.Create(
            name: invalidName!,
            displayName: "Display",
            fieldType: FieldType.Text);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDisplayName_ShouldThrowArgumentException(string? invalidDisplayName)
    {
        // Act
        var act = () => FieldDefinition.Create(
            name: "validName",
            displayName: invalidDisplayName!,
            fieldType: FieldType.Text);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("displayName");
    }

    [Theory]
    [InlineData("1field")]       // Starts with number
    [InlineData("field-name")]   // Contains hyphen
    [InlineData("field.name")]   // Contains dot
    [InlineData("field name")]   // Contains space
    [InlineData("@field")]       // Starts with special char
    public void Create_WithInvalidFieldName_ShouldThrowArgumentException(string invalidName)
    {
        // Act
        var act = () => FieldDefinition.Create(
            name: invalidName,
            displayName: "Display",
            fieldType: FieldType.Text);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Theory]
    [InlineData("field")]
    [InlineData("fieldName")]
    [InlineData("field_name")]
    [InlineData("Field123")]
    [InlineData("a")]
    [InlineData("Field_Name_123")]
    public void Create_WithValidFieldName_ShouldSucceed(string validName)
    {
        // Act
        var field = FieldDefinition.Create(
            name: validName,
            displayName: "Display",
            fieldType: FieldType.Text);

        // Assert
        field.Name.Should().Be(validName);
    }

    #endregion

    #region Select/MultiSelect Tests

    [Fact]
    public void Create_SelectField_WithOptions_ShouldStoreOptions()
    {
        // Arrange
        var options = new[] { "Option 1", "Option 2", "Option 3" };

        // Act
        var field = FieldDefinition.Create(
            name: "choice",
            displayName: "Choice",
            fieldType: FieldType.Select,
            options: options);

        // Assert
        field.GetOptions().Should().BeEquivalentTo(options);
    }

    [Fact]
    public void Create_SelectField_WithoutOptions_ShouldThrowArgumentException()
    {
        // Act
        var act = () => FieldDefinition.Create(
            name: "choice",
            displayName: "Choice",
            fieldType: FieldType.Select,
            options: null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Create_SelectField_WithEmptyOptions_ShouldThrowArgumentException()
    {
        // Act
        var act = () => FieldDefinition.Create(
            name: "choice",
            displayName: "Choice",
            fieldType: FieldType.Select,
            options: Array.Empty<string>());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Create_MultiSelectField_WithOptions_ShouldStoreOptions()
    {
        // Arrange
        var options = new[] { "A", "B", "C" };

        // Act
        var field = FieldDefinition.Create(
            name: "multiChoice",
            displayName: "Multi Choice",
            fieldType: FieldType.MultiSelect,
            options: options);

        // Assert
        field.GetOptions().Should().BeEquivalentTo(options);
    }

    #endregion

    #region Number Field Tests

    [Fact]
    public void Create_NumberField_WithMinMax_ShouldStoreBounds()
    {
        // Act
        var field = FieldDefinition.Create(
            name: "score",
            displayName: "Score",
            fieldType: FieldType.Number,
            minValue: 1,
            maxValue: 100);

        // Assert
        field.MinValue.Should().Be(1);
        field.MaxValue.Should().Be(100);
    }

    [Fact]
    public void Create_NumberField_WithMinGreaterThanMax_ShouldThrowArgumentException()
    {
        // Act
        var act = () => FieldDefinition.Create(
            name: "score",
            displayName: "Score",
            fieldType: FieldType.Number,
            minValue: 100,
            maxValue: 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Factory Methods Tests

    [Fact]
    public void Text_ShouldCreateTextField()
    {
        // Act
        var field = FieldDefinition.Text("name", "Name", isRequired: true);

        // Assert
        field.FieldType.Should().Be(FieldType.Text);
        field.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TextArea_ShouldCreateTextAreaField()
    {
        // Act
        var field = FieldDefinition.TextArea("description", "Description");

        // Assert
        field.FieldType.Should().Be(FieldType.TextArea);
    }

    [Fact]
    public void Number_ShouldCreateNumberField()
    {
        // Act
        var field = FieldDefinition.Number("level", "Level", minValue: 1, maxValue: 20);

        // Assert
        field.FieldType.Should().Be(FieldType.Number);
        field.MinValue.Should().Be(1);
        field.MaxValue.Should().Be(20);
    }

    [Fact]
    public void Boolean_ShouldCreateBooleanField()
    {
        // Act
        var field = FieldDefinition.Boolean("isActive", "Is Active", defaultValue: true);

        // Assert
        field.FieldType.Should().Be(FieldType.Boolean);
        field.DefaultValue.Should().Be("true");
    }

    [Fact]
    public void Select_ShouldCreateSelectField()
    {
        // Act
        var field = FieldDefinition.Select("race", "Race", new[] { "Human", "Elf", "Dwarf" });

        // Assert
        field.FieldType.Should().Be(FieldType.Select);
        field.GetOptions().Should().HaveCount(3);
    }

    #endregion

    #region ValidateValue Tests

    [Fact]
    public void ValidateValue_RequiredField_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var field = FieldDefinition.Text("name", "Name", isRequired: true);

        // Act
        var result = field.ValidateValue(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateValue_OptionalField_WithNull_ShouldReturnTrue()
    {
        // Arrange
        var field = FieldDefinition.Text("name", "Name", isRequired: false);

        // Act
        var result = field.ValidateValue(null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateValue_TextField_WithValidText_ShouldReturnTrue()
    {
        // Arrange
        var field = FieldDefinition.Text("name", "Name");

        // Act
        var result = field.ValidateValue("John Doe");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateValue_TextField_WithMaxLength_ExceedsMax_ShouldReturnFalse()
    {
        // Arrange
        var field = FieldDefinition.Text("code", "Code", maxLength: 5);

        // Act
        var result = field.ValidateValue("TOOLONG");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateValue_NumberField_WithValidNumber_ShouldReturnTrue()
    {
        // Arrange
        var field = FieldDefinition.Number("score", "Score", minValue: 1, maxValue: 100);

        // Act
        var result = field.ValidateValue(50);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateValue_NumberField_BelowMin_ShouldReturnFalse()
    {
        // Arrange
        var field = FieldDefinition.Number("score", "Score", minValue: 1, maxValue: 100);

        // Act
        var result = field.ValidateValue(0);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateValue_NumberField_AboveMax_ShouldReturnFalse()
    {
        // Arrange
        var field = FieldDefinition.Number("score", "Score", minValue: 1, maxValue: 100);

        // Act
        var result = field.ValidateValue(150);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateValue_NumberField_WithStringNumber_ShouldReturnTrue()
    {
        // Arrange
        var field = FieldDefinition.Number("score", "Score");

        // Act
        var result = field.ValidateValue("42");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateValue_NumberField_WithInvalidString_ShouldReturnFalse()
    {
        // Arrange
        var field = FieldDefinition.Number("score", "Score");

        // Act
        var result = field.ValidateValue("not-a-number");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateValue_BooleanField_WithBoolean_ShouldReturnTrue(bool value)
    {
        // Arrange
        var field = FieldDefinition.Boolean("isActive", "Is Active");

        // Act
        var result = field.ValidateValue(value);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("True")]
    [InlineData("False")]
    public void ValidateValue_BooleanField_WithBooleanString_ShouldReturnTrue(string value)
    {
        // Arrange
        var field = FieldDefinition.Boolean("isActive", "Is Active");

        // Act
        var result = field.ValidateValue(value);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateValue_SelectField_WithValidOption_ShouldReturnTrue()
    {
        // Arrange
        var field = FieldDefinition.Select("race", "Race", new[] { "Human", "Elf", "Dwarf" });

        // Act
        var result = field.ValidateValue("Elf");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateValue_SelectField_WithInvalidOption_ShouldReturnFalse()
    {
        // Arrange
        var field = FieldDefinition.Select("race", "Race", new[] { "Human", "Elf", "Dwarf" });

        // Act
        var result = field.ValidateValue("Orc");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateValue_MultiSelectField_WithValidOptions_ShouldReturnTrue()
    {
        // Arrange
        var field = FieldDefinition.Create(
            "skills", "Skills", FieldType.MultiSelect,
            options: new[] { "Stealth", "Magic", "Combat" });

        // Act
        var result = field.ValidateValue(new[] { "Stealth", "Magic" });

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateValue_MultiSelectField_WithInvalidOption_ShouldReturnFalse()
    {
        // Arrange
        var field = FieldDefinition.Create(
            "skills", "Skills", FieldType.MultiSelect,
            options: new[] { "Stealth", "Magic", "Combat" });

        // Act
        var result = field.ValidateValue(new[] { "Stealth", "Flying" });

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameNameDisplayNameTypeRequired_ShouldBeEqual()
    {
        // Arrange
        var field1 = FieldDefinition.Text("name", "Name", isRequired: true);
        var field2 = FieldDefinition.Text("name", "Name", isRequired: true);

        // Assert
        field1.Should().Be(field2);
        field1.GetHashCode().Should().Be(field2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentName_ShouldNotBeEqual()
    {
        // Arrange
        var field1 = FieldDefinition.Text("name1", "Name", isRequired: true);
        var field2 = FieldDefinition.Text("name2", "Name", isRequired: true);

        // Assert
        field1.Should().NotBe(field2);
    }

    [Fact]
    public void Equals_DifferentType_ShouldNotBeEqual()
    {
        // Arrange
        var field1 = FieldDefinition.Text("name", "Name");
        var field2 = FieldDefinition.Number("name", "Name");

        // Assert
        field1.Should().NotBe(field2);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnReadableFormat()
    {
        // Arrange
        var field = FieldDefinition.Number("strength", "Strength");

        // Act
        var result = field.ToString();

        // Assert
        result.Should().Contain("Strength");
        result.Should().Contain("strength");
        result.Should().Contain("Number");
    }

    #endregion
}

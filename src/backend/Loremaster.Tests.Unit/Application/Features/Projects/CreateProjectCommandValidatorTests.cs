using FluentValidation.TestHelper;
using Loremaster.Application.Features.Projects.Commands.CreateProject;

namespace Loremaster.Tests.Unit.Application.Features.Projects;

public class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator;

    public CreateProjectCommandValidatorTests()
    {
        _validator = new CreateProjectCommandValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateProjectCommand("Valid Project Name", "A valid description");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ShouldHaveError(string? name)
    {
        // Arrange
        var command = new CreateProjectCommand(name!, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameExceeding200Characters_ShouldHaveError()
    {
        // Arrange
        var longName = new string('a', 201);
        var command = new CreateProjectCommand(longName, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Project name cannot exceed 200 characters");
    }

    [Fact]
    public void Validate_WithNameExactly200Characters_ShouldNotHaveError()
    {
        // Arrange
        var name = new string('a', 200);
        var command = new CreateProjectCommand(name, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("My Project")]
    [InlineData("Project-1")]
    [InlineData("Project.Name")]
    [InlineData("Project_123")]
    [InlineData("My New Project 2024")]
    public void Validate_WithValidNameCharacters_ShouldNotHaveError(string name)
    {
        // Arrange
        var command = new CreateProjectCommand(name, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("Project@Name")]
    [InlineData("Project#1")]
    [InlineData("Project!")]
    [InlineData("Project$")]
    public void Validate_WithInvalidNameCharacters_ShouldHaveError(string name)
    {
        // Arrange
        var command = new CreateProjectCommand(name, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Project name can only contain letters, numbers, spaces, hyphens, and dots");
    }

    [Fact]
    public void Validate_WithDescriptionExceeding2000Characters_ShouldHaveError()
    {
        // Arrange
        var longDescription = new string('a', 2001);
        var command = new CreateProjectCommand("Valid Name", longDescription);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 2000 characters");
    }

    [Fact]
    public void Validate_WithDescriptionExactly2000Characters_ShouldNotHaveError()
    {
        // Arrange
        var description = new string('a', 2000);
        var command = new CreateProjectCommand("Valid Name", description);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WithNullDescription_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateProjectCommand("Valid Name", null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
}

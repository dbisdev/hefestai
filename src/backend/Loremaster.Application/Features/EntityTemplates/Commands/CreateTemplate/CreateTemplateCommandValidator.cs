using FluentValidation;
using Loremaster.Application.Features.EntityTemplates.DTOs;
using Loremaster.Domain.Enums;
using System.Text.RegularExpressions;

namespace Loremaster.Application.Features.EntityTemplates.Commands.CreateTemplate;

/// <summary>
/// Validator for CreateTemplateCommand.
/// Validates all input parameters before command execution.
/// </summary>
public partial class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
{
    /// <summary>
    /// Regex pattern for valid field names: starts with letter, alphanumeric + underscore.
    /// </summary>
    private static readonly Regex FieldNamePattern = FieldNameRegex();

    public CreateTemplateCommandValidator()
    {
        RuleFor(x => x.GameSystemId)
            .NotEmpty()
            .WithMessage("Game system ID is required");

        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .WithMessage("Owner ID is required");

        RuleFor(x => x.EntityTypeName)
            .NotEmpty()
            .WithMessage("Entity type name is required")
            .MaximumLength(100)
            .WithMessage("Entity type name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_\s-]*$")
            .WithMessage("Entity type name must start with a letter and contain only letters, numbers, spaces, hyphens, or underscores");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required")
            .MaximumLength(200)
            .WithMessage("Display name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.IconHint)
            .MaximumLength(100)
            .WithMessage("Icon hint cannot exceed 100 characters")
            .When(x => x.IconHint != null);

        RuleFor(x => x.Version)
            .MaximumLength(50)
            .WithMessage("Version cannot exceed 50 characters")
            .When(x => x.Version != null);

        // Validate field definitions if provided
        RuleForEach(x => x.Fields)
            .SetValidator(new FieldDefinitionDtoValidator())
            .When(x => x.Fields != null && x.Fields.Count > 0);

        // Validate no duplicate field names
        RuleFor(x => x.Fields)
            .Must(HaveUniqueFieldNames)
            .WithMessage("Field names must be unique within a template")
            .When(x => x.Fields != null && x.Fields.Count > 1);
    }

    /// <summary>
    /// Checks that all field names are unique (case-insensitive).
    /// </summary>
    private static bool HaveUniqueFieldNames(IReadOnlyList<FieldDefinitionDto>? fields)
    {
        if (fields == null || fields.Count == 0)
            return true;

        var names = fields.Select(f => f.Name?.ToLowerInvariant() ?? string.Empty).ToList();
        return names.Distinct().Count() == names.Count;
    }

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex FieldNameRegex();
}

/// <summary>
/// Validator for individual field definitions.
/// </summary>
public class FieldDefinitionDtoValidator : AbstractValidator<FieldDefinitionDto>
{
    public FieldDefinitionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Field name is required")
            .MaximumLength(100)
            .WithMessage("Field name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_]*$")
            .WithMessage("Field name must start with a letter and contain only letters, numbers, or underscores");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Field display name is required")
            .MaximumLength(200)
            .WithMessage("Field display name cannot exceed 200 characters");

        RuleFor(x => x.FieldType)
            .IsInEnum()
            .WithMessage("Invalid field type");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Field description cannot exceed 500 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.DefaultValue)
            .MaximumLength(1000)
            .WithMessage("Default value cannot exceed 1000 characters")
            .When(x => x.DefaultValue != null);

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Field order must be non-negative");

        // Select/MultiSelect fields must have options
        RuleFor(x => x.Options)
            .NotEmpty()
            .WithMessage("Options are required for Select and MultiSelect field types")
            .When(x => x.FieldType == FieldType.Select || x.FieldType == FieldType.MultiSelect);

        // Number fields validation
        RuleFor(x => x.MaxValue)
            .GreaterThanOrEqualTo(x => x.MinValue ?? decimal.MinValue)
            .WithMessage("Maximum value must be greater than or equal to minimum value")
            .When(x => x.MinValue.HasValue && x.MaxValue.HasValue);
    }
}

using Loremaster.Domain.ValueObjects;

namespace Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;

/// <summary>
/// Converts a list of <see cref="ExtractedField"/> DTOs (produced by the LLM parser) into
/// domain <see cref="FieldDefinition"/> value objects, applying type coercion, name sanitization
/// and uniqueness enforcement.
/// </summary>
internal interface IFieldDefinitionConverter
{
    /// <summary>
    /// Converts <paramref name="extractedFields"/> to a list of domain <see cref="FieldDefinition"/>
    /// objects.  Field names are sanitized to valid identifiers and made unique by appending a
    /// numeric suffix when duplicates would otherwise occur.
    /// </summary>
    IReadOnlyList<FieldDefinition> Convert(IReadOnlyList<ExtractedField> extractedFields);
}

using Loremaster.Domain.Entities;

namespace Loremaster.Application.Features.EntityGeneration.Services;

public interface ITemplateResolutionService
{
    Task<TemplateResolutionResult> ResolveTemplateAsync(
        Guid? gameSystemId,
        Guid userId,
        string entityTypeName,
        string entityTypeDisplayName,
        CancellationToken cancellationToken = default);
}

public record TemplateResolutionResult
{
    public EntityTemplate? Template { get; init; }
    public string ExampleJson { get; init; } = string.Empty;
    public string FieldDescriptions { get; init; } = string.Empty;
    public string FallbackExampleJson { get; init; } = string.Empty;
    public string FallbackFieldDescriptions { get; init; } = string.Empty;
    public bool HasTemplate => Template != null;

    public static TemplateResolutionResult WithoutTemplate(
        string fallbackExampleJson,
        string fallbackFieldDescriptions) => new()
        {
            FallbackExampleJson = fallbackExampleJson,
            FallbackFieldDescriptions = fallbackFieldDescriptions
        };

    public static TemplateResolutionResult WithTemplate(
        EntityTemplate template,
        string exampleJson,
        string fieldDescriptions,
        string fallbackExampleJson,
        string fallbackFieldDescriptions) => new()
        {
            Template = template,
            ExampleJson = exampleJson,
            FieldDescriptions = fieldDescriptions,
            FallbackExampleJson = fallbackExampleJson,
            FallbackFieldDescriptions = fallbackFieldDescriptions
        };
}

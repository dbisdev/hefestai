using Loremaster.Domain.Entities;
using Loremaster.Domain.ValueObjects;

namespace Loremaster.Application.Features.EntityGeneration.Services;

public interface IPromptBuilderService
{
    string BuildExampleJsonFromTemplate(IReadOnlyList<FieldDefinition> fields);
    string BuildFieldDescriptions(IReadOnlyList<FieldDefinition> fields);
    string BuildFullPromptTrace(string systemPrompt, string userQuery, IEnumerable<string>? ragContext = null);
    (string ExampleJson, string FieldDescriptions) BuildTemplateSchema(EntityTemplate template, string entityTypeDisplayName);
}

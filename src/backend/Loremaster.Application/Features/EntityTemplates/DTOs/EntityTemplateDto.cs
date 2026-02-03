using Loremaster.Domain.Enums;

namespace Loremaster.Application.Features.EntityTemplates.DTOs;

/// <summary>
/// DTO for entity template information.
/// </summary>
public record EntityTemplateDto(
    Guid Id,
    string EntityTypeName,
    string DisplayName,
    string? Description,
    TemplateStatus Status,
    IReadOnlyList<FieldDefinitionDto> Fields,
    string? IconHint,
    string? Version,
    string? ReviewNotes,
    DateTime? ConfirmedAt,
    Guid? ConfirmedByUserId,
    Guid GameSystemId,
    string GameSystemName,
    Guid? SourceDocumentId,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// DTO for field definition within a template.
/// </summary>
public record FieldDefinitionDto(
    string Name,
    string DisplayName,
    FieldType FieldType,
    bool IsRequired,
    string? DefaultValue,
    string? Description,
    int Order,
    IReadOnlyList<string>? Options,
    decimal? MinValue,
    decimal? MaxValue,
    string? ValidationPattern);

/// <summary>
/// Lightweight DTO for template listing.
/// </summary>
public record EntityTemplateSummaryDto(
    Guid Id,
    string EntityTypeName,
    string DisplayName,
    TemplateStatus Status,
    int FieldCount,
    string? IconHint,
    DateTime CreatedAt);

using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Features.Projects.DTOs;

public record ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public ProjectStatus Status { get; init; }
    public Guid OwnerId { get; init; }
    public string? OwnerEmail { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ArchivedAt { get; init; }

    public static ProjectDto FromEntity(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status,
            OwnerId = project.OwnerId,
            OwnerEmail = project.Owner?.Email,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            ArchivedAt = project.ArchivedAt
        };
    }
}

public record ProjectListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public ProjectStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }

    public static ProjectListDto FromEntity(Project project)
    {
        return new ProjectListDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description?.Length > 100 
                ? project.Description[..100] + "..." 
                : project.Description,
            Status = project.Status,
            CreatedAt = project.CreatedAt
        };
    }
}

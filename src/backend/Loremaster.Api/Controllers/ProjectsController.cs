using Loremaster.Application.Features.Projects.Commands.ArchiveProject;
using Loremaster.Application.Features.Projects.Commands.CreateProject;
using Loremaster.Application.Features.Projects.Commands.DeleteProject;
using Loremaster.Application.Features.Projects.Commands.UpdateProject;
using Loremaster.Application.Features.Projects.DTOs;
using Loremaster.Application.Features.Projects.Queries.GetProjectById;
using Loremaster.Application.Features.Projects.Queries.GetProjects;
using Loremaster.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loremaster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all projects for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectListDto>>> GetProjects(
        [FromQuery] ProjectStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProjectsQuery(status), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific project by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetProject(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectDto>> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand(request.Name, request.Description);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProject), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing project
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectDto>> UpdateProject(
        Guid id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProjectCommand(id, request.Name, request.Description);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteProjectCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Archive a project
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveProject(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveProjectCommand(id, Archive: true), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Restore an archived project
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreProject(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveProjectCommand(id, Archive: false), cancellationToken);
        return NoContent();
    }
}

// Request DTOs for API
public record CreateProjectRequest(string Name, string? Description);
public record UpdateProjectRequest(string Name, string? Description);

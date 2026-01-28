using Loremaster.Domain.Entities;
using Loremaster.Domain.Enums;

namespace Loremaster.Application.Common.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdWithOwnerAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByOwnerIdAsync(Guid ownerId, ProjectStatus? status = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsForOwnerAsync(Guid ownerId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
    Task DeleteAsync(Project project, CancellationToken cancellationToken = default);
    Task<int> GetCountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
}

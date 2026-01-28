using Loremaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Loremaster.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Project> Projects { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

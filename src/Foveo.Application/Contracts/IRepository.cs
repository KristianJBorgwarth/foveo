using Foveo.Domain.Abstractions;

namespace Foveo.Application.Contracts;

public interface IRepository<T> where T : Entity
{
    Task AddAsync(T entity, CancellationToken ct = default);

    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

using Examine2DSerialization.Entities;

namespace Examine2DSerialization.Repositories;

/// <summary>
/// Base repository interface
/// </summary>
public interface IRepository<T> where T : EntityBase
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Project repository interface
/// </summary>
public interface IProjectRepository : IRepository<ProjectEntity>
{
    Task<ProjectEntity?> GetWithBoundariesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectEntity?> GetCompleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Boundary repository interface
/// </summary>
public interface IBoundaryRepository : IRepository<BoundaryEntity>
{
    Task<IEnumerable<BoundaryEntity>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<BoundaryEntity?> GetWithVerticesAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stress grid repository interface
/// </summary>
public interface IStressGridRepository : IRepository<StressGridEntity>
{
    Task<IEnumerable<StressGridEntity>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Query repository interface
/// </summary>
public interface IQueryRepository : IRepository<QueryEntity>
{
    Task<IEnumerable<QueryEntity>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<QueryEntity?> GetWithPointsAsync(Guid id, CancellationToken cancellationToken = default);
}

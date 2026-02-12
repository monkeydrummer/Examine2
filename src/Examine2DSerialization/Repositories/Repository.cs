using Examine2DSerialization.Data;
using Examine2DSerialization.Entities;
using Microsoft.EntityFrameworkCore;

namespace Examine2DSerialization.Repositories;

/// <summary>
/// Base repository implementation
/// </summary>
public class Repository<T> : IRepository<T> where T : EntityBase
{
    protected readonly Examine2DContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public Repository(Examine2DContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }
    
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }
    
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;
        
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }
    
    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.ModifiedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }
    
    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }
    
    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }
}

/// <summary>
/// Project repository implementation
/// </summary>
public class ProjectRepository : Repository<ProjectEntity>, IProjectRepository
{
    public ProjectRepository(Examine2DContext context) : base(context) { }
    
    public async Task<ProjectEntity?> GetWithBoundariesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Boundaries)
                .ThenInclude(b => b.Vertices.OrderBy(v => v.Order))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
    
    public async Task<ProjectEntity?> GetCompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Boundaries)
                .ThenInclude(b => b.Vertices.OrderBy(v => v.Order))
            .Include(p => p.StressGrids)
            .Include(p => p.Queries)
                .ThenInclude(q => q.Points.OrderBy(p => p.Order))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}

/// <summary>
/// Boundary repository implementation
/// </summary>
public class BoundaryRepository : Repository<BoundaryEntity>, IBoundaryRepository
{
    public BoundaryRepository(Examine2DContext context) : base(context) { }
    
    public async Task<IEnumerable<BoundaryEntity>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<BoundaryEntity?> GetWithVerticesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Vertices.OrderBy(v => v.Order))
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }
}

/// <summary>
/// Stress grid repository implementation
/// </summary>
public class StressGridRepository : Repository<StressGridEntity>, IStressGridRepository
{
    public StressGridRepository(Examine2DContext context) : base(context) { }
    
    public async Task<IEnumerable<StressGridEntity>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Query repository implementation
/// </summary>
public class QueryRepository : Repository<QueryEntity>, IQueryRepository
{
    public QueryRepository(Examine2DContext context) : base(context) { }
    
    public async Task<IEnumerable<QueryEntity>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(q => q.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<QueryEntity?> GetWithPointsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(q => q.Points.OrderBy(p => p.Order))
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }
}

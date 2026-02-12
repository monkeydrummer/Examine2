using Examine2DSerialization.Data;
using Examine2DSerialization.Repositories;

namespace Examine2DSerialization;

/// <summary>
/// Unit of Work pattern implementation
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IProjectRepository Projects { get; }
    IBoundaryRepository Boundaries { get; }
    IStressGridRepository StressGrids { get; }
    IQueryRepository Queries { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work implementation
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly Examine2DContext _context;
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;
    
    private IProjectRepository? _projectRepository;
    private IBoundaryRepository? _boundaryRepository;
    private IStressGridRepository? _stressGridRepository;
    private IQueryRepository? _queryRepository;
    
    public UnitOfWork(Examine2DContext context)
    {
        _context = context;
    }
    
    public IProjectRepository Projects =>
        _projectRepository ??= new ProjectRepository(_context);
    
    public IBoundaryRepository Boundaries =>
        _boundaryRepository ??= new BoundaryRepository(_context);
    
    public IStressGridRepository StressGrids =>
        _stressGridRepository ??= new StressGridRepository(_context);
    
    public IQueryRepository Queries =>
        _queryRepository ??= new QueryRepository(_context);
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }
    
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

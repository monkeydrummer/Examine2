using CAD2DModel.Geometry;

namespace CAD2DModel.Services.Implementations;

/// <summary>
/// Simple spatial index using a grid-based approach for fast entity queries
/// Note: This is a simplified implementation. For production, consider using a proper R-tree library.
/// </summary>
/// <typeparam name="T">Entity type that implements IEntity</typeparam>
public class SpatialIndex<T> : ISpatialIndex<T> where T : IEntity
{
    private readonly Dictionary<T, Rect2D> _entityBounds = new();
    private readonly Dictionary<(int, int), HashSet<T>> _grid = new();
    private readonly double _cellSize;
    
    public SpatialIndex(double cellSize = 10.0)
    {
        _cellSize = cellSize;
    }
    
    public void Insert(T entity, Rect2D bounds)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        
        _entityBounds[entity] = bounds;
        
        // Add to all overlapping grid cells
        var cells = GetOverlappingCells(bounds);
        foreach (var cell in cells)
        {
            if (!_grid.ContainsKey(cell))
                _grid[cell] = new HashSet<T>();
            
            _grid[cell].Add(entity);
        }
    }
    
    public void Remove(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        
        if (!_entityBounds.TryGetValue(entity, out var bounds))
            return;
        
        // Remove from all overlapping grid cells
        var cells = GetOverlappingCells(bounds);
        foreach (var cell in cells)
        {
            if (_grid.TryGetValue(cell, out var cellEntities))
            {
                cellEntities.Remove(entity);
                if (cellEntities.Count == 0)
                    _grid.Remove(cell);
            }
        }
        
        _entityBounds.Remove(entity);
    }
    
    public void Update(T entity, Rect2D newBounds)
    {
        Remove(entity);
        Insert(entity, newBounds);
    }
    
    public IEnumerable<T> Query(Rect2D bounds)
    {
        var results = new HashSet<T>();
        var cells = GetOverlappingCells(bounds);
        
        foreach (var cell in cells)
        {
            if (_grid.TryGetValue(cell, out var cellEntities))
            {
                foreach (var entity in cellEntities)
                {
                    // Verify that entity actually intersects the query bounds
                    if (_entityBounds.TryGetValue(entity, out var entityBounds))
                    {
                        if (entityBounds.Intersects(bounds))
                        {
                            results.Add(entity);
                        }
                    }
                }
            }
        }
        
        return results;
    }
    
    public IEnumerable<T> QueryRadius(Point2D center, double radius)
    {
        var bounds = new Rect2D(
            center.X - radius,
            center.Y - radius,
            radius * 2,
            radius * 2);
        
        var radiusSquared = radius * radius;
        var candidates = Query(bounds);
        
        // Filter to only entities actually within the radius
        var results = new List<T>();
        foreach (var entity in candidates)
        {
            if (_entityBounds.TryGetValue(entity, out var entityBounds))
            {
                // Check if any corner of the entity bounds is within radius
                var corners = new[]
                {
                    new Point2D(entityBounds.X, entityBounds.Y),
                    new Point2D(entityBounds.Right, entityBounds.Y),
                    new Point2D(entityBounds.Right, entityBounds.Bottom),
                    new Point2D(entityBounds.X, entityBounds.Bottom)
                };
                
                foreach (var corner in corners)
                {
                    if (center.DistanceSquaredTo(corner) <= radiusSquared)
                    {
                        results.Add(entity);
                        break;
                    }
                }
            }
        }
        
        return results;
    }
    
    public void Clear()
    {
        _entityBounds.Clear();
        _grid.Clear();
    }
    
    public void Rebuild()
    {
        // Rebuild using existing bounds
        var entries = _entityBounds.ToList();
        Clear();
        foreach (var entry in entries)
        {
            Insert(entry.Key, entry.Value);
        }
    }
    
    private List<(int, int)> GetOverlappingCells(Rect2D bounds)
    {
        var cells = new List<(int, int)>();
        
        int minCellX = (int)Math.Floor(bounds.X / _cellSize);
        int maxCellX = (int)Math.Floor(bounds.Right / _cellSize);
        int minCellY = (int)Math.Floor(bounds.Y / _cellSize);
        int maxCellY = (int)Math.Floor(bounds.Bottom / _cellSize);
        
        for (int x = minCellX; x <= maxCellX; x++)
        {
            for (int y = minCellY; y <= maxCellY; y++)
            {
                cells.Add((x, y));
            }
        }
        
        return cells;
    }
}

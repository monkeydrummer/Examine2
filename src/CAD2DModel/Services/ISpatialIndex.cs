using CAD2DModel.Geometry;

namespace CAD2DModel.Services;

/// <summary>
/// Interface for spatial indexing (R-tree) for efficient hit-testing
/// </summary>
public interface ISpatialIndex<T> where T : IEntity
{
    /// <summary>
    /// Insert an entity into the spatial index
    /// </summary>
    void Insert(T entity, Rect2D bounds);
    
    /// <summary>
    /// Remove an entity from the spatial index
    /// </summary>
    void Remove(T entity);
    
    /// <summary>
    /// Update an entity's bounds in the index
    /// </summary>
    void Update(T entity, Rect2D newBounds);
    
    /// <summary>
    /// Query entities that intersect with a rectangular region
    /// </summary>
    IEnumerable<T> Query(Rect2D bounds);
    
    /// <summary>
    /// Query entities within a radius of a point
    /// </summary>
    IEnumerable<T> QueryRadius(Point2D center, double radius);
    
    /// <summary>
    /// Clear all entities from the index
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Rebuild the entire index
    /// </summary>
    void Rebuild();
}

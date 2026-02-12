using CAD2DModel.Geometry;

namespace CAD2DModel.Selection;

/// <summary>
/// Represents a handle to a specific vertex in an entity
/// </summary>
public class VertexHandle
{
    /// <summary>
    /// Gets the entity that owns this vertex
    /// </summary>
    public IEntity Entity { get; }
    
    /// <summary>
    /// Gets the index of the vertex within the entity
    /// </summary>
    public int VertexIndex { get; }
    
    /// <summary>
    /// Gets the current location of the vertex
    /// </summary>
    public Point2D Location
    {
        get
        {
            if (Entity is Polyline polyline)
                return polyline.Vertices[VertexIndex].Location;
            if (Entity is Boundary boundary)
                return boundary.Vertices[VertexIndex].Location;
            
            throw new InvalidOperationException("Entity type does not support vertices");
        }
    }
    
    public VertexHandle(IEntity entity, int vertexIndex)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        VertexIndex = vertexIndex;
        
        // Validate that the entity supports vertices
        if (entity is not Polyline && entity is not Boundary)
        {
            throw new ArgumentException("Entity must be a Polyline or Boundary", nameof(entity));
        }
        
        // Validate index
        int vertexCount = entity is Polyline p ? p.Vertices.Count 
                        : entity is Boundary b ? b.Vertices.Count 
                        : throw new InvalidOperationException("Entity type does not support vertices");
        
        if (vertexIndex < 0 || vertexIndex >= vertexCount)
        {
            throw new ArgumentOutOfRangeException(nameof(vertexIndex), 
                $"Vertex index {vertexIndex} is out of range (0-{vertexCount - 1})");
        }
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is VertexHandle other)
        {
            return Entity == other.Entity && VertexIndex == other.VertexIndex;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Entity, VertexIndex);
    }
}

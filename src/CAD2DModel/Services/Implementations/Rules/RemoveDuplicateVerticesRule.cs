using CAD2DModel.Geometry;

namespace CAD2DModel.Services.Implementations.Rules;

/// <summary>
/// Rule that removes duplicate consecutive vertices in polylines and boundaries
/// </summary>
public class RemoveDuplicateVerticesRule : IGeometryRule
{
    private readonly double _tolerance;
    
    public string Name => "Remove Duplicate Vertices";
    public int Priority => 50; // Run before minimum segment length
    
    public RemoveDuplicateVerticesRule(double tolerance = 0.0001)
    {
        _tolerance = tolerance;
    }
    
    public bool AppliesTo(IEntity entity)
    {
        return entity is Polyline || entity is Boundary;
    }
    
    public void Apply(IEntity entity, IGeometryModel model)
    {
        if (entity is Polyline polyline)
        {
            RemoveDuplicatesFromPolyline(polyline);
        }
        else if (entity is Boundary boundary)
        {
            RemoveDuplicatesFromBoundary(boundary);
        }
    }
    
    private void RemoveDuplicatesFromPolyline(Polyline polyline)
    {
        if (polyline.Vertices.Count < 2)
            return;
        
        var toleranceSquared = _tolerance * _tolerance;
        
        // Check consecutive vertices and remove duplicates
        for (int i = polyline.Vertices.Count - 1; i > 0; i--)
        {
            var v1 = polyline.Vertices[i - 1];
            var v2 = polyline.Vertices[i];
            
            double distanceSquared = 
                (v2.Location.X - v1.Location.X) * (v2.Location.X - v1.Location.X) +
                (v2.Location.Y - v1.Location.Y) * (v2.Location.Y - v1.Location.Y);
            
            if (distanceSquared < toleranceSquared)
            {
                // Remove duplicate vertex
                polyline.Vertices.RemoveAt(i);
            }
        }
    }
    
    private void RemoveDuplicatesFromBoundary(Boundary boundary)
    {
        if (boundary.Vertices.Count < 3)
            return;
        
        var toleranceSquared = _tolerance * _tolerance;
        var verticesToRemove = new List<int>();
        
        // Check consecutive vertices including wrap-around
        for (int i = 0; i < boundary.Vertices.Count; i++)
        {
            var v1 = boundary.Vertices[i];
            var v2 = boundary.Vertices[(i + 1) % boundary.Vertices.Count];
            
            double distanceSquared = 
                (v2.Location.X - v1.Location.X) * (v2.Location.X - v1.Location.X) +
                (v2.Location.Y - v1.Location.Y) * (v2.Location.Y - v1.Location.Y);
            
            if (distanceSquared < toleranceSquared)
            {
                // Mark the second vertex for removal
                verticesToRemove.Add((i + 1) % boundary.Vertices.Count);
            }
        }
        
        // Remove vertices in reverse order to maintain indices
        foreach (var index in verticesToRemove.OrderByDescending(x => x).Distinct())
        {
            if (boundary.Vertices.Count > 3) // Keep at least 3 vertices for a boundary
            {
                boundary.Vertices.RemoveAt(index);
            }
        }
    }
}

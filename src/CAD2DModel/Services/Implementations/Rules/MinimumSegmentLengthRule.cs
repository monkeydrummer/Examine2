using CAD2DModel.Geometry;

namespace CAD2DModel.Services.Implementations.Rules;

/// <summary>
/// Rule that enforces minimum segment length in polylines and boundaries
/// Removes segments that are too short
/// </summary>
public class MinimumSegmentLengthRule : IGeometryRule
{
    private readonly double _minimumLength;
    
    public string Name => "Minimum Segment Length";
    public int Priority => 100;
    
    public MinimumSegmentLengthRule(double minimumLength = 0.001)
    {
        _minimumLength = minimumLength;
    }
    
    public bool AppliesTo(IEntity entity)
    {
        return entity is Polyline || entity is Boundary;
    }
    
    public void Apply(IEntity entity, IGeometryModel model)
    {
        if (entity is Polyline polyline)
        {
            ApplyToPolyline(polyline);
        }
        else if (entity is Boundary boundary)
        {
            ApplyToBoundary(boundary);
        }
    }
    
    private void ApplyToPolyline(Polyline polyline)
    {
        if (polyline.Vertices.Count < 2)
            return;
        
        // Check each segment and remove vertices that create segments that are too short
        for (int i = polyline.Vertices.Count - 1; i > 0; i--)
        {
            var v1 = polyline.Vertices[i - 1];
            var v2 = polyline.Vertices[i];
            
            double segmentLength = v1.Location.DistanceTo(v2.Location);
            
            if (segmentLength < _minimumLength)
            {
                // Remove the second vertex if segment is too short
                polyline.Vertices.RemoveAt(i);
            }
        }
    }
    
    private void ApplyToBoundary(Boundary boundary)
    {
        if (boundary.Vertices.Count < 3)
            return;
        
        // For boundaries, we need to check including the closing segment
        var verticesToRemove = new List<int>();
        
        for (int i = 0; i < boundary.Vertices.Count; i++)
        {
            var v1 = boundary.Vertices[i];
            var v2 = boundary.Vertices[(i + 1) % boundary.Vertices.Count];
            
            double segmentLength = v1.Location.DistanceTo(v2.Location);
            
            if (segmentLength < _minimumLength)
            {
                // Determine which vertex to remove
                int vertexToRemove;
                
                // If this is the closing segment (from last vertex back to first)
                if (i == boundary.Vertices.Count - 1)
                {
                    // Remove the last vertex (the one we're starting from)
                    vertexToRemove = i;
                }
                else
                {
                    // Remove the second vertex in the pair
                    vertexToRemove = i + 1;
                }
                
                if (!verticesToRemove.Contains(vertexToRemove))
                {
                    verticesToRemove.Add(vertexToRemove);
                }
            }
        }
        
        // Only remove vertices if we'll still have at least 3 left
        int finalCount = boundary.Vertices.Count - verticesToRemove.Count;
        if (finalCount < 3)
        {
            // Cannot remove these vertices, would leave too few
            return;
        }
        
        // Remove vertices in reverse order to maintain indices
        foreach (var index in verticesToRemove.OrderByDescending(x => x))
        {
            boundary.Vertices.RemoveAt(index);
        }
    }
}

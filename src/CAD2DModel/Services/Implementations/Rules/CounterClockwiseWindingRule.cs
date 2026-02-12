using CAD2DModel.Geometry;

namespace CAD2DModel.Services.Implementations.Rules;

/// <summary>
/// Rule that ensures boundaries have counter-clockwise winding order
/// This is important for boundary element analysis where the normal direction matters
/// </summary>
public class CounterClockwiseWindingRule : IGeometryRule
{
    public string Name => "Counter-Clockwise Winding";
    public int Priority => 200; // Run after cleanup rules
    
    public bool AppliesTo(IEntity entity)
    {
        return entity is Boundary;
    }
    
    public void Apply(IEntity entity, IGeometryModel model)
    {
        if (entity is Boundary boundary)
        {
            EnsureCounterClockwise(boundary);
        }
    }
    
    private void EnsureCounterClockwise(Boundary boundary)
    {
        if (boundary.Vertices.Count < 3)
            return;
        
        // Calculate signed area using shoelace formula
        // Positive area = counter-clockwise, Negative area = clockwise
        double signedArea = 0.0;
        
        for (int i = 0; i < boundary.Vertices.Count; i++)
        {
            var v1 = boundary.Vertices[i];
            var v2 = boundary.Vertices[(i + 1) % boundary.Vertices.Count];
            
            // Shoelace formula: sum of (x1 * y2 - x2 * y1)
            signedArea += v1.Location.X * v2.Location.Y - v2.Location.X * v1.Location.Y;
        }
        
        // If signed area is negative, the vertices are clockwise - need to reverse
        if (signedArea < 0)
        {
            // Reverse while keeping first vertex at index 0
            // Take elements 1..n and reverse them
            if (boundary.Vertices.Count > 1)
            {
                var firstVertex = boundary.Vertices[0];
                var remainingVertices = new List<Vertex>();
                
                for (int i = 1; i < boundary.Vertices.Count; i++)
                {
                    remainingVertices.Add(boundary.Vertices[i]);
                }
                
                remainingVertices.Reverse();
                
                boundary.Vertices.Clear();
                boundary.Vertices.Add(firstVertex);
                foreach (var v in remainingVertices)
                {
                    boundary.Vertices.Add(v);
                }
            }
        }
    }
}

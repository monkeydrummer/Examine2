using CAD2DModel.Geometry;

namespace CAD2DModel.Services.Implementations.Rules;

/// <summary>
/// Rule that adds vertices at intersection points between intersectable boundaries
/// This ensures that boundaries that cross each other have proper vertices at the crossings
/// Critical for boundary element analysis where mesh connectivity is important
/// </summary>
public class BoundaryIntersectionRule : IGeometryRule
{
    private readonly double _tolerance;
    
    public string Name => "Boundary Intersection";
    public int Priority => 150; // After cleanup rules, before winding
    
    public BoundaryIntersectionRule(double tolerance = 1e-6)
    {
        _tolerance = tolerance;
    }
    
    public bool AppliesTo(IEntity entity)
    {
        return entity is Boundary boundary && boundary.Intersectable;
    }
    
    public void Apply(IEntity entity, IGeometryModel model)
    {
        if (entity is not Boundary boundary || !boundary.Intersectable)
            return;
        
        // Find all other intersectable boundaries
        var otherBoundaries = model.Entities
            .OfType<Boundary>()
            .Where(b => b != boundary && b.Intersectable)
            .ToList();
        
        if (otherBoundaries.Count == 0)
            return;
        
        // Find all intersection points for this boundary
        var intersectionsForThisBoundary = new List<BoundaryIntersectionPoint>();
        
        // Track intersections for other boundaries as well
        var intersectionsForOtherBoundaries = new Dictionary<Boundary, List<BoundaryIntersectionPoint>>();
        
        foreach (var otherBoundary in otherBoundaries)
        {
            var intersections = FindIntersectionPointsBetween(boundary, otherBoundary);
            
            foreach (var (thisIntersection, otherIntersection) in intersections)
            {
                intersectionsForThisBoundary.Add(thisIntersection);
                
                if (!intersectionsForOtherBoundaries.ContainsKey(otherBoundary))
                {
                    intersectionsForOtherBoundaries[otherBoundary] = new List<BoundaryIntersectionPoint>();
                }
                intersectionsForOtherBoundaries[otherBoundary].Add(otherIntersection);
            }
        }
        
        // Insert vertices into this boundary
        InsertIntersectionVertices(boundary, intersectionsForThisBoundary);
        
        // Insert vertices into other boundaries
        foreach (var kvp in intersectionsForOtherBoundaries)
        {
            InsertIntersectionVertices(kvp.Key, kvp.Value);
        }
    }
    
    private List<(BoundaryIntersectionPoint, BoundaryIntersectionPoint)> FindIntersectionPointsBetween(
        Boundary boundary1, Boundary boundary2)
    {
        var intersections = new List<(BoundaryIntersectionPoint, BoundaryIntersectionPoint)>();
        int segmentCount1 = boundary1.GetSegmentCount();
        int segmentCount2 = boundary2.GetSegmentCount();
        
        for (int i = 0; i < segmentCount1; i++)
        {
            var seg1 = boundary1.GetSegment(i);
            
            for (int j = 0; j < segmentCount2; j++)
            {
                var seg2 = boundary2.GetSegment(j);
                
                var intersection = IntersectionCalculator.LineSegmentIntersection(
                    seg1.Start, seg1.End, seg2.Start, seg2.End, 
                    extendFirst: false, extendSecond: false);
                
                if (intersection != null &&
                    intersection.Parameter1 > _tolerance && 
                    intersection.Parameter1 < 1.0 - _tolerance &&
                    intersection.Parameter2 > _tolerance && 
                    intersection.Parameter2 < 1.0 - _tolerance)
                {
                    // True intersection (not at endpoints)
                    var point1 = new BoundaryIntersectionPoint
                    {
                        SegmentIndex = i,
                        Parameter = intersection.Parameter1,
                        Point = intersection.Location
                    };
                    
                    var point2 = new BoundaryIntersectionPoint
                    {
                        SegmentIndex = j,
                        Parameter = intersection.Parameter2,
                        Point = intersection.Location
                    };
                    
                    intersections.Add((point1, point2));
                }
            }
        }
        
        return intersections;
    }
    
    private void InsertIntersectionVertices(Boundary boundary, List<BoundaryIntersectionPoint> intersections)
    {
        if (intersections.Count == 0)
            return;
        
        // Sort by segment index and parameter (descending) for reverse insertion
        intersections = intersections
            .OrderByDescending(i => i.SegmentIndex)
            .ThenByDescending(i => i.Parameter)
            .ToList();
        
        // Insert vertices in reverse order to maintain indices
        foreach (var intersection in intersections)
        {
            // Check if vertex already exists at this location
            if (!VertexExistsAt(boundary, intersection.Point, _tolerance))
            {
                // Insert after the segment start vertex
                boundary.Vertices.Insert(intersection.SegmentIndex + 1, 
                    new Vertex(intersection.Point));
            }
        }
    }
    
    private bool VertexExistsAt(Boundary boundary, Point2D point, double tolerance)
    {
        double toleranceSquared = tolerance * tolerance;
        
        foreach (var vertex in boundary.Vertices)
        {
            double distSquared = 
                (vertex.Location.X - point.X) * (vertex.Location.X - point.X) +
                (vertex.Location.Y - point.Y) * (vertex.Location.Y - point.Y);
            
            if (distSquared < toleranceSquared)
                return true;
        }
        
        return false;
    }
    
    private class BoundaryIntersectionPoint
    {
        public int SegmentIndex { get; set; }
        public double Parameter { get; set; }
        public Point2D Point { get; set; }
    }
}

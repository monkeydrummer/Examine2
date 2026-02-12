using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Services.Implementations;

/// <summary>
/// Geometry engine implementation for geometric operations
/// </summary>
public class GeometryEngine : IGeometryEngine
{
    private const double Epsilon = 1e-10;
    
    public Point2D? FindIntersection(LineSegment line1, LineSegment line2)
    {
        var p1 = line1.Start;
        var p2 = line1.End;
        var p3 = line2.Start;
        var p4 = line2.End;
        
        var d1 = p2 - p1;
        var d2 = p4 - p3;
        
        double cross = d1.Cross(d2);
        
        // Lines are parallel or coincident
        if (Math.Abs(cross) < Epsilon)
            return null;
        
        var d3 = p3 - p1;
        double t1 = d3.Cross(d2) / cross;
        double t2 = d3.Cross(d1) / cross;
        
        // Check if intersection is within both line segments
        if (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1)
        {
            return p1 + d1 * t1;
        }
        
        return null;
    }
    
    public IEnumerable<Point2D> FindIntersections(Polyline poly1, Polyline poly2)
    {
        var intersections = new List<Point2D>();
        
        int count1 = poly1.GetSegmentCount();
        int count2 = poly2.GetSegmentCount();
        
        for (int i = 0; i < count1; i++)
        {
            var seg1 = poly1.GetSegment(i);
            
            for (int j = 0; j < count2; j++)
            {
                var seg2 = poly2.GetSegment(j);
                var intersection = FindIntersection(seg1, seg2);
                
                if (intersection.HasValue)
                {
                    // Check if this point is already in the list (within tolerance)
                    bool isDuplicate = intersections.Any(p => 
                        p.DistanceSquaredTo(intersection.Value) < Epsilon * Epsilon);
                    
                    if (!isDuplicate)
                    {
                        intersections.Add(intersection.Value);
                    }
                }
            }
        }
        
        return intersections;
    }
    
    public bool IsPointInside(Point2D point, Polyline boundary)
    {
        if (!boundary.IsClosed)
            return false;
        
        // Ray casting algorithm
        int intersectionCount = 0;
        int segmentCount = boundary.GetSegmentCount();
        
        // Cast a ray from the point to the right (positive X direction)
        for (int i = 0; i < segmentCount; i++)
        {
            var segment = boundary.GetSegment(i);
            var v1 = segment.Start;
            var v2 = segment.End;
            
            // Check if the ray intersects with this segment
            if ((v1.Y > point.Y) != (v2.Y > point.Y))
            {
                double xIntersection = (v2.X - v1.X) * (point.Y - v1.Y) / (v2.Y - v1.Y) + v1.X;
                
                if (point.X < xIntersection)
                {
                    intersectionCount++;
                }
            }
        }
        
        return (intersectionCount % 2) == 1;
    }
    
    public Polyline? ClipPolyline(Polyline polyline, Polyline clipBoundary)
    {
        // Sutherland-Hodgman polygon clipping algorithm
        // This is a simplified version that clips a polyline against a convex boundary
        
        if (!clipBoundary.IsClosed)
            return polyline;
        
        var outputVertices = polyline.Vertices.Select(v => v.Location).ToList();
        int clipSegmentCount = clipBoundary.GetSegmentCount();
        
        for (int i = 0; i < clipSegmentCount; i++)
        {
            var clipSegment = clipBoundary.GetSegment(i);
            var inputVertices = outputVertices;
            outputVertices = new List<Point2D>();
            
            if (inputVertices.Count == 0)
                break;
            
            var edge = clipSegment.End - clipSegment.Start;
            var edgeNormal = edge.Perpendicular().Normalized();
            
            for (int j = 0; j < inputVertices.Count; j++)
            {
                var current = inputVertices[j];
                var next = inputVertices[(j + 1) % inputVertices.Count];
                
                bool currentInside = IsPointInsideEdge(current, clipSegment.Start, edgeNormal);
                bool nextInside = IsPointInsideEdge(next, clipSegment.Start, edgeNormal);
                
                if (currentInside && nextInside)
                {
                    outputVertices.Add(next);
                }
                else if (currentInside && !nextInside)
                {
                    var intersection = ComputeIntersection(current, next, clipSegment);
                    if (intersection.HasValue)
                        outputVertices.Add(intersection.Value);
                }
                else if (!currentInside && nextInside)
                {
                    var intersection = ComputeIntersection(current, next, clipSegment);
                    if (intersection.HasValue)
                        outputVertices.Add(intersection.Value);
                    outputVertices.Add(next);
                }
            }
        }
        
        if (outputVertices.Count == 0)
            return null;
        
        return new Polyline(outputVertices) { IsClosed = polyline.IsClosed };
    }
    
    private bool IsPointInsideEdge(Point2D point, Point2D edgeStart, Vector2D edgeNormal)
    {
        var toPoint = point - edgeStart;
        return toPoint.Dot(edgeNormal) >= 0;
    }
    
    private Point2D? ComputeIntersection(Point2D p1, Point2D p2, LineSegment edge)
    {
        var segment = new LineSegment(p1, p2);
        return FindIntersection(segment, edge);
    }
    
    public Polyline? OffsetPolyline(Polyline polyline, double distance)
    {
        // Simple offset algorithm - moves each vertex perpendicular to the average of adjacent edges
        if (polyline.VertexCount < 2)
            return null;
        
        var offsetPoints = new List<Point2D>();
        int count = polyline.VertexCount;
        
        for (int i = 0; i < count; i++)
        {
            var current = polyline.Vertices[i].Location;
            
            Vector2D offset;
            
            if (!polyline.IsClosed && (i == 0 || i == count - 1))
            {
                // End points: offset perpendicular to the edge
                var edge = i == 0
                    ? polyline.Vertices[1].Location - current
                    : current - polyline.Vertices[i - 1].Location;
                
                offset = edge.Perpendicular().Normalized() * distance;
            }
            else
            {
                // Middle points: offset based on average of adjacent edges
                var prev = polyline.Vertices[i == 0 ? count - 1 : i - 1].Location;
                var next = polyline.Vertices[(i + 1) % count].Location;
                
                var edge1 = (current - prev).Normalized();
                var edge2 = (next - current).Normalized();
                
                var bisector = (edge1 + edge2).Normalized();
                var perpendicular = edge1.Perpendicular();
                
                // Calculate offset distance accounting for the angle
                double dotProduct = bisector.Dot(perpendicular);
                double offsetDistance = Math.Abs(dotProduct) > Epsilon
                    ? distance / dotProduct
                    : distance;
                
                offset = bisector * offsetDistance;
            }
            
            offsetPoints.Add(current + offset);
        }
        
        return new Polyline(offsetPoints) { IsClosed = polyline.IsClosed };
    }
    
    public double DistanceToLineSegment(Point2D point, LineSegment segment)
    {
        return segment.DistanceToPoint(point);
    }
    
    public Point2D ClosestPointOnSegment(Point2D point, LineSegment segment)
    {
        return segment.ClosestPoint(point);
    }
    
    public bool DoSegmentsIntersect(LineSegment line1, LineSegment line2)
    {
        return FindIntersection(line1, line2).HasValue;
    }
}

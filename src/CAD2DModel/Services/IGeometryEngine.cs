using CAD2DModel.Geometry;

namespace CAD2DModel.Services;

/// <summary>
/// Interface for geometric operations such as intersection, clipping, and offsetting
/// </summary>
public interface IGeometryEngine
{
    /// <summary>
    /// Find intersection points between two line segments
    /// </summary>
    Point2D? FindIntersection(LineSegment line1, LineSegment line2);
    
    /// <summary>
    /// Find all intersection points between two polylines
    /// </summary>
    IEnumerable<Point2D> FindIntersections(Polyline poly1, Polyline poly2);
    
    /// <summary>
    /// Check if a point is inside a closed polyline
    /// </summary>
    bool IsPointInside(Point2D point, Polyline boundary);
    
    /// <summary>
    /// Clip one polyline to another boundary
    /// </summary>
    Polyline? ClipPolyline(Polyline polyline, Polyline clipBoundary);
    
    /// <summary>
    /// Create an offset polyline at specified distance
    /// </summary>
    Polyline? OffsetPolyline(Polyline polyline, double distance);
    
    /// <summary>
    /// Calculate the distance from a point to a line segment
    /// </summary>
    double DistanceToLineSegment(Point2D point, LineSegment segment);
    
    /// <summary>
    /// Find the closest point on a line segment to a given point
    /// </summary>
    Point2D ClosestPointOnSegment(Point2D point, LineSegment segment);
    
    /// <summary>
    /// Check if two line segments intersect
    /// </summary>
    bool DoSegmentsIntersect(LineSegment line1, LineSegment line2);
}

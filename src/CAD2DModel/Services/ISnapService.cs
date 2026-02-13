using CAD2DModel.Geometry;

namespace CAD2DModel.Services;

/// <summary>
/// Types of snap modes available
/// </summary>
[Flags]
public enum SnapMode
{
    None = 0,
    Vertex = 1,
    Midpoint = 2,
    Grid = 4,
    Ortho = 8,
    Nearest = 16,
    Intersection = 32
}

/// <summary>
/// Result of a snap operation
/// </summary>
public class SnapResult
{
    public Point2D SnappedPoint { get; init; }
    public SnapMode SnapType { get; init; }
    public IEntity? SnapEntity { get; init; }
    public bool IsSnapped { get; init; }
    
    public SnapResult(Point2D point, bool isSnapped = false, SnapMode snapType = SnapMode.None, IEntity? entity = null)
    {
        SnappedPoint = point;
        IsSnapped = isSnapped;
        SnapType = snapType;
        SnapEntity = entity;
    }
}

/// <summary>
/// Interface for snapping operations (vertex, midpoint, grid, ortho)
/// </summary>
public interface ISnapService
{
    /// <summary>
    /// Current active snap modes
    /// </summary>
    SnapMode ActiveSnapModes { get; set; }
    
    /// <summary>
    /// Snap tolerance in screen pixels (remains constant regardless of zoom)
    /// </summary>
    double SnapTolerancePixels { get; set; }
    
    /// <summary>
    /// Vertex snap tolerance in screen pixels (less aggressive than general snap tolerance)
    /// </summary>
    double VertexSnapTolerancePixels { get; set; }
    
    /// <summary>
    /// Grid spacing for grid snap
    /// </summary>
    double GridSpacing { get; set; }
    
    /// <summary>
    /// Ortho snap angle tolerance in degrees (only snaps if within this many degrees of horizontal/vertical)
    /// </summary>
    double OrthoAngleToleranceDegrees { get; set; }
    
    /// <summary>
    /// Attempt to snap a point to nearby geometry
    /// </summary>
    SnapResult Snap(Point2D point, IEnumerable<IEntity> entities, CAD2DModel.Camera.Camera2D camera);
    
    /// <summary>
    /// Snap to vertex
    /// </summary>
    SnapResult? SnapToVertex(Point2D point, IEnumerable<Polyline> polylines, CAD2DModel.Camera.Camera2D camera);
    
    /// <summary>
    /// Snap to midpoint of line segments
    /// </summary>
    SnapResult? SnapToMidpoint(Point2D point, IEnumerable<Polyline> polylines, CAD2DModel.Camera.Camera2D camera);
    
    /// <summary>
    /// Snap to grid
    /// </summary>
    SnapResult SnapToGrid(Point2D point, CAD2DModel.Camera.Camera2D camera);
    
    /// <summary>
    /// Apply orthogonal constraint relative to a reference point
    /// </summary>
    SnapResult SnapToOrtho(Point2D point, Point2D referencePoint);
    
    /// <summary>
    /// Snap to nearest point on geometry
    /// </summary>
    SnapResult? SnapToNearest(Point2D point, IEnumerable<Polyline> polylines, CAD2DModel.Camera.Camera2D camera);
}

using CAD2DModel.Geometry;

namespace CAD2DModel.Annotations;

/// <summary>
/// Base interface for all annotation entities
/// </summary>
public interface IAnnotation : IEntity
{
    /// <summary>
    /// Color of the annotation
    /// </summary>
    Color Color { get; set; }
    
    /// <summary>
    /// Line weight in pixels
    /// </summary>
    float LineWeight { get; set; }
    
    /// <summary>
    /// Line style (solid, dashed, etc.)
    /// </summary>
    LineStyle LineStyle { get; set; }
    
    /// <summary>
    /// Whether the annotation is currently selected
    /// </summary>
    bool IsSelected { get; set; }
    
    /// <summary>
    /// Whether the annotation is in edit mode (showing control points)
    /// </summary>
    bool IsEditing { get; set; }
    
    /// <summary>
    /// Gets the bounding rectangle of the annotation in world coordinates
    /// </summary>
    Rect2D GetBounds();
    
    /// <summary>
    /// Gets all control points for this annotation
    /// </summary>
    IReadOnlyList<IControlPoint> GetControlPoints();
    
    /// <summary>
    /// Hit test to check if a point is on or near the annotation
    /// </summary>
    /// <param name="worldPoint">Point to test in world coordinates</param>
    /// <param name="tolerance">Tolerance in world units</param>
    /// <returns>True if the point hits the annotation</returns>
    bool HitTest(Point2D worldPoint, double tolerance);
    
    /// <summary>
    /// Update a control point's location
    /// </summary>
    /// <param name="controlPoint">The control point to update</param>
    /// <param name="newLocation">New location in world coordinates</param>
    void UpdateControlPoint(IControlPoint controlPoint, Point2D newLocation);
}

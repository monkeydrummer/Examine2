using CAD2DModel.Geometry;
using CAD2DModel.Interaction;

namespace CAD2DModel.Annotations;

/// <summary>
/// Interface for annotation control points
/// </summary>
public interface IControlPoint
{
    /// <summary>
    /// Gets the annotation that owns this control point
    /// </summary>
    IAnnotation Annotation { get; }
    
    /// <summary>
    /// Gets the type of control point
    /// </summary>
    ControlPointType Type { get; }
    
    /// <summary>
    /// Gets or sets the location of the control point in world coordinates
    /// </summary>
    Point2D Location { get; set; }
    
    /// <summary>
    /// Gets the cursor type to display when hovering over this control point
    /// </summary>
    Cursor CursorType { get; }
    
    /// <summary>
    /// Gets whether this control point can be moved
    /// </summary>
    bool IsMovable { get; }
    
    /// <summary>
    /// Gets the index of this control point (for polylines/polygons with multiple vertices)
    /// </summary>
    int Index { get; }
}

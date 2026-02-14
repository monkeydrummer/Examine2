using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Annotations;

/// <summary>
/// Abstract base class for all annotations
/// </summary>
public abstract partial class AnnotationBase : ObservableObject, IAnnotation
{
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private bool _isVisible = true;
    
    [ObservableProperty]
    private Color _color = Color.Black;
    
    [ObservableProperty]
    private float _lineWeight = 1.0f;
    
    [ObservableProperty]
    private LineStyle _lineStyle = LineStyle.Solid;
    
    [ObservableProperty]
    private bool _isSelected;
    
    [ObservableProperty]
    private bool _isEditing;
    
    /// <summary>
    /// Gets the bounding rectangle of the annotation in world coordinates
    /// </summary>
    public abstract Rect2D GetBounds();
    
    /// <summary>
    /// Gets all control points for this annotation
    /// </summary>
    public abstract IReadOnlyList<IControlPoint> GetControlPoints();
    
    /// <summary>
    /// Hit test to check if a point is on or near the annotation
    /// </summary>
    public abstract bool HitTest(Point2D worldPoint, double tolerance);
    
    /// <summary>
    /// Update a control point's location
    /// </summary>
    public abstract void UpdateControlPoint(IControlPoint controlPoint, Point2D newLocation);
    
    /// <summary>
    /// Helper method to calculate distance from point to line segment
    /// </summary>
    protected double DistanceToLineSegment(Point2D point, Point2D lineStart, Point2D lineEnd)
    {
        // Calculate the distance from point to the line segment
        double dx = lineEnd.X - lineStart.X;
        double dy = lineEnd.Y - lineStart.Y;
        
        if (dx == 0 && dy == 0)
        {
            // Line segment is a point
            return point.DistanceTo(lineStart);
        }
        
        // Calculate the parameter t for the closest point on the line
        double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
        
        // Clamp t to [0, 1] to stay on the segment
        t = Math.Max(0, Math.Min(1, t));
        
        // Calculate the closest point on the segment
        Point2D closest = new Point2D(
            lineStart.X + t * dx,
            lineStart.Y + t * dy
        );
        
        return point.DistanceTo(closest);
    }
    
    /// <summary>
    /// Helper method to check if a point is inside a rectangle
    /// </summary>
    protected bool IsPointInRectangle(Point2D point, Rect2D rectangle)
    {
        return point.X >= rectangle.X && 
               point.X <= rectangle.X + rectangle.Width &&
               point.Y >= rectangle.Y && 
               point.Y <= rectangle.Y + rectangle.Height;
    }
}

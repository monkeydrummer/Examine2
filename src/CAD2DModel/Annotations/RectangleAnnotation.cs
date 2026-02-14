using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Annotations;

/// <summary>
/// Rectangle annotation with 8 control points (4 corners + 4 edge midpoints)
/// </summary>
public partial class RectangleAnnotation : AnnotationBase
{
    [ObservableProperty]
    private Point2D _topLeft;
    
    [ObservableProperty]
    private Point2D _bottomRight;
    
    [ObservableProperty]
    private bool _isFilled;
    
    [ObservableProperty]
    private Color _fillColor = new Color(128, 128, 128, 100);
    
    [ObservableProperty]
    private bool _isHatched;
    
    [ObservableProperty]
    private HatchStyle _hatchStyle = HatchStyle.None;
    
    [ObservableProperty]
    private Color _hatchColor = Color.Black;
    
    [ObservableProperty]
    private double _hatchSpacing = 5.0;
    
    [ObservableProperty]
    private double _rotationDegrees;
    
    public RectangleAnnotation()
    {
        _topLeft = new Point2D(0, 0);
        _bottomRight = new Point2D(0, 0);
    }
    
    public RectangleAnnotation(Point2D corner1, Point2D corner2)
    {
        // Ensure topLeft is actually top-left and bottomRight is bottom-right
        double minX = Math.Min(corner1.X, corner2.X);
        double minY = Math.Min(corner1.Y, corner2.Y);
        double maxX = Math.Max(corner1.X, corner2.X);
        double maxY = Math.Max(corner1.Y, corner2.Y);
        
        _topLeft = new Point2D(minX, minY);
        _bottomRight = new Point2D(maxX, maxY);
    }
    
    /// <summary>
    /// Gets the width of the rectangle
    /// </summary>
    public double Width => Math.Abs(BottomRight.X - TopLeft.X);
    
    /// <summary>
    /// Gets the height of the rectangle
    /// </summary>
    public double Height => Math.Abs(BottomRight.Y - TopLeft.Y);
    
    /// <summary>
    /// Gets the center point of the rectangle
    /// </summary>
    public Point2D Center => new Point2D(
        (TopLeft.X + BottomRight.X) / 2.0,
        (TopLeft.Y + BottomRight.Y) / 2.0
    );
    
    /// <summary>
    /// Gets the four corner points
    /// </summary>
    public Point2D TopRight => new Point2D(BottomRight.X, TopLeft.Y);
    public Point2D BottomLeft => new Point2D(TopLeft.X, BottomRight.Y);
    
    /// <summary>
    /// Gets the four edge midpoints
    /// </summary>
    public Point2D TopEdgeMidpoint => new Point2D((TopLeft.X + BottomRight.X) / 2.0, TopLeft.Y);
    public Point2D BottomEdgeMidpoint => new Point2D((TopLeft.X + BottomRight.X) / 2.0, BottomRight.Y);
    public Point2D LeftEdgeMidpoint => new Point2D(TopLeft.X, (TopLeft.Y + BottomRight.Y) / 2.0);
    public Point2D RightEdgeMidpoint => new Point2D(BottomRight.X, (TopLeft.Y + BottomRight.Y) / 2.0);
    
    public override Rect2D GetBounds()
    {
        return new Rect2D(TopLeft.X, TopLeft.Y, Width, Height);
    }
    
    public override IReadOnlyList<IControlPoint> GetControlPoints()
    {
        return new List<IControlPoint>
        {
            // Corner control points
            new ControlPoint(this, ControlPointType.TopLeft, TopLeft, 0),
            new ControlPoint(this, ControlPointType.TopRight, TopRight, 1),
            new ControlPoint(this, ControlPointType.BottomRight, BottomRight, 2),
            new ControlPoint(this, ControlPointType.BottomLeft, BottomLeft, 3),
            
            // Edge midpoint control points
            new ControlPoint(this, ControlPointType.TopEdge, TopEdgeMidpoint, 4),
            new ControlPoint(this, ControlPointType.RightEdge, RightEdgeMidpoint, 5),
            new ControlPoint(this, ControlPointType.BottomEdge, BottomEdgeMidpoint, 6),
            new ControlPoint(this, ControlPointType.LeftEdge, LeftEdgeMidpoint, 7)
        };
    }
    
    public override bool HitTest(Point2D worldPoint, double tolerance)
    {
        Rect2D bounds = GetBounds();
        
        if (IsFilled)
        {
            // If filled, check if point is inside
            return IsPointInRectangle(worldPoint, bounds);
        }
        else
        {
            // Check if point is near any of the four edges
            bool nearTop = DistanceToLineSegment(worldPoint, TopLeft, TopRight) <= tolerance;
            bool nearRight = DistanceToLineSegment(worldPoint, TopRight, BottomRight) <= tolerance;
            bool nearBottom = DistanceToLineSegment(worldPoint, BottomRight, BottomLeft) <= tolerance;
            bool nearLeft = DistanceToLineSegment(worldPoint, BottomLeft, TopLeft) <= tolerance;
            
            return nearTop || nearRight || nearBottom || nearLeft;
        }
    }
    
    public override void UpdateControlPoint(IControlPoint controlPoint, Point2D newLocation)
    {
        switch (controlPoint.Type)
        {
            case ControlPointType.TopLeft:
                TopLeft = newLocation;
                break;
                
            case ControlPointType.TopRight:
                TopLeft = new Point2D(TopLeft.X, newLocation.Y);
                BottomRight = new Point2D(newLocation.X, BottomRight.Y);
                break;
                
            case ControlPointType.BottomRight:
                BottomRight = newLocation;
                break;
                
            case ControlPointType.BottomLeft:
                TopLeft = new Point2D(newLocation.X, TopLeft.Y);
                BottomRight = new Point2D(BottomRight.X, newLocation.Y);
                break;
                
            case ControlPointType.TopEdge:
                TopLeft = new Point2D(TopLeft.X, newLocation.Y);
                break;
                
            case ControlPointType.BottomEdge:
                BottomRight = new Point2D(BottomRight.X, newLocation.Y);
                break;
                
            case ControlPointType.LeftEdge:
                TopLeft = new Point2D(newLocation.X, TopLeft.Y);
                break;
                
            case ControlPointType.RightEdge:
                BottomRight = new Point2D(newLocation.X, BottomRight.Y);
                break;
        }
    }
}

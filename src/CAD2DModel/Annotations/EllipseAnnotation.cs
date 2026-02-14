using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Annotations;

/// <summary>
/// Ellipse or circle annotation
/// </summary>
public partial class EllipseAnnotation : AnnotationBase
{
    [ObservableProperty]
    private Point2D _center;
    
    [ObservableProperty]
    private double _radiusX;
    
    [ObservableProperty]
    private double _radiusY;
    
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
    
    public EllipseAnnotation()
    {
        _center = new Point2D(0, 0);
        _radiusX = 50;
        _radiusY = 50;
    }
    
    public EllipseAnnotation(Point2D center, double radiusX, double radiusY)
    {
        _center = center;
        _radiusX = radiusX;
        _radiusY = radiusY;
    }
    
    /// <summary>
    /// True if this is a perfect circle
    /// </summary>
    public bool IsCircle => Math.Abs(RadiusX - RadiusY) < 0.001;
    
    public override Rect2D GetBounds()
    {
        return new Rect2D(
            Center.X - RadiusX,
            Center.Y - RadiusY,
            RadiusX * 2,
            RadiusY * 2
        );
    }
    
    public override IReadOnlyList<IControlPoint> GetControlPoints()
    {
        // 4 control points at cardinal directions
        return new List<IControlPoint>
        {
            new ControlPoint(this, ControlPointType.Middle, Center, 0),
            new ControlPoint(this, ControlPointType.RightEdge, new Point2D(Center.X + RadiusX, Center.Y), 1),
            new ControlPoint(this, ControlPointType.TopEdge, new Point2D(Center.X, Center.Y - RadiusY), 2),
            new ControlPoint(this, ControlPointType.LeftEdge, new Point2D(Center.X - RadiusX, Center.Y), 3),
            new ControlPoint(this, ControlPointType.BottomEdge, new Point2D(Center.X, Center.Y + RadiusY), 4)
        };
    }
    
    public override bool HitTest(Point2D worldPoint, double tolerance)
    {
        if (IsFilled)
        {
            // Check if point is inside ellipse
            double dx = (worldPoint.X - Center.X) / RadiusX;
            double dy = (worldPoint.Y - Center.Y) / RadiusY;
            return (dx * dx + dy * dy) <= 1.0;
        }
        else
        {
            // Check if point is near the ellipse edge
            double dx = (worldPoint.X - Center.X) / RadiusX;
            double dy = (worldPoint.Y - Center.Y) / RadiusY;
            double distanceRatio = Math.Sqrt(dx * dx + dy * dy);
            
            return Math.Abs(distanceRatio - 1.0) <= (tolerance / Math.Min(RadiusX, RadiusY));
        }
    }
    
    public override void UpdateControlPoint(IControlPoint controlPoint, Point2D newLocation)
    {
        switch (controlPoint.Type)
        {
            case ControlPointType.Middle:
                Center = newLocation;
                break;
                
            case ControlPointType.RightEdge:
                RadiusX = Math.Abs(newLocation.X - Center.X);
                break;
                
            case ControlPointType.LeftEdge:
                RadiusX = Math.Abs(newLocation.X - Center.X);
                break;
                
            case ControlPointType.TopEdge:
                RadiusY = Math.Abs(newLocation.Y - Center.Y);
                break;
                
            case ControlPointType.BottomEdge:
                RadiusY = Math.Abs(newLocation.Y - Center.Y);
                break;
        }
    }
}

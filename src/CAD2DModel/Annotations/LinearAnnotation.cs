using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Annotations;

/// <summary>
/// Base class for linear annotations (line, ruler, arrow)
/// </summary>
public partial class LinearAnnotation : AnnotationBase
{
    [ObservableProperty]
    private Point2D _startPoint;
    
    [ObservableProperty]
    private Point2D _endPoint;
    
    [ObservableProperty]
    private bool _arrowAtHead;
    
    [ObservableProperty]
    private bool _arrowAtTail;
    
    [ObservableProperty]
    private ArrowStyle _arrowHeadStyle = ArrowStyle.FilledTriangle;
    
    [ObservableProperty]
    private ArrowStyle _arrowTailStyle = ArrowStyle.FilledTriangle;
    
    [ObservableProperty]
    private double _arrowSize = 10.0;
    
    [ObservableProperty]
    private string _text = string.Empty;
    
    [ObservableProperty]
    private string _fontFamily = "Arial";
    
    [ObservableProperty]
    private float _fontSize = 12.0f;
    
    [ObservableProperty]
    private bool _fontBold;
    
    [ObservableProperty]
    private bool _fontItalic;
    
    [ObservableProperty]
    private Color _textColor = Color.Black;
    
    [ObservableProperty]
    private bool _drawTextBackground;
    
    [ObservableProperty]
    private bool _textOutline;
    
    [ObservableProperty]
    private Color _textBackgroundColor = new Color(255, 255, 255, 200);
    
    public LinearAnnotation()
    {
        _startPoint = new Point2D(0, 0);
        _endPoint = new Point2D(0, 0);
    }
    
    public LinearAnnotation(Point2D start, Point2D end)
    {
        _startPoint = start;
        _endPoint = end;
    }
    
    /// <summary>
    /// Gets the length of the line
    /// </summary>
    public double Length => StartPoint.DistanceTo(EndPoint);
    
    /// <summary>
    /// Gets the angle of the line in degrees (0-360, measured counter-clockwise from positive X axis)
    /// </summary>
    public double AngleDegrees
    {
        get
        {
            double dx = EndPoint.X - StartPoint.X;
            double dy = EndPoint.Y - StartPoint.Y;
            double angleRad = Math.Atan2(dy, dx);
            double angleDeg = angleRad * 180.0 / Math.PI;
            if (angleDeg < 0) angleDeg += 360.0;
            return angleDeg;
        }
    }
    
    public override Rect2D GetBounds()
    {
        double minX = Math.Min(StartPoint.X, EndPoint.X);
        double minY = Math.Min(StartPoint.Y, EndPoint.Y);
        double maxX = Math.Max(StartPoint.X, EndPoint.X);
        double maxY = Math.Max(StartPoint.Y, EndPoint.Y);
        
        // Add some padding for arrows and text
        double padding = ArrowSize * 2;
        
        return new Rect2D(
            minX - padding,
            minY - padding,
            (maxX - minX) + 2 * padding,
            (maxY - minY) + 2 * padding
        );
    }
    
    public override IReadOnlyList<IControlPoint> GetControlPoints()
    {
        return new List<IControlPoint>
        {
            new ControlPoint(this, ControlPointType.Start, StartPoint),
            new ControlPoint(this, ControlPointType.End, EndPoint)
        };
    }
    
    public override bool HitTest(Point2D worldPoint, double tolerance)
    {
        return DistanceToLineSegment(worldPoint, StartPoint, EndPoint) <= tolerance;
    }
    
    public override void UpdateControlPoint(IControlPoint controlPoint, Point2D newLocation)
    {
        switch (controlPoint.Type)
        {
            case ControlPointType.Start:
                StartPoint = newLocation;
                break;
            case ControlPointType.End:
                EndPoint = newLocation;
                break;
        }
    }
}

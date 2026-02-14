using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Annotations;

/// <summary>
/// Angular dimension annotation with arc display
/// </summary>
public partial class AngularDimensionAnnotation : AnnotationBase
{
    [ObservableProperty]
    private Point2D _centerPoint;
    
    [ObservableProperty]
    private Point2D _firstArmPoint;
    
    [ObservableProperty]
    private Point2D _secondArmPoint;
    
    [ObservableProperty]
    private double _arcRadius = 30.0;
    
    [ObservableProperty]
    private int _decimalPlaces = 1;
    
    [ObservableProperty]
    private bool _arrowAtStart = true;
    
    [ObservableProperty]
    private bool _arrowAtEnd = true;
    
    [ObservableProperty]
    private ArrowStyle _arrowStyle = ArrowStyle.FilledTriangle;
    
    [ObservableProperty]
    private double _arrowSize = 8.0;
    
    [ObservableProperty]
    private string _fontFamily = "Arial";
    
    [ObservableProperty]
    private float _fontSize = 9f;
    
    [ObservableProperty]
    private bool _fontBold;
    
    [ObservableProperty]
    private bool _fontItalic;
    
    [ObservableProperty]
    private Color _textColor = Color.Black;
    
    [ObservableProperty]
    private bool _drawTextBackground = true;
    
    [ObservableProperty]
    private Color _textBackgroundColor = new Color(255, 255, 255, 200);
    
    public AngularDimensionAnnotation()
    {
        _centerPoint = new Point2D(0, 0);
        _firstArmPoint = new Point2D(50, 0);
        _secondArmPoint = new Point2D(0, 50);
        
        Color = Color.Black;
        LineWeight = 1.0f;
    }
    
    public AngularDimensionAnnotation(Point2D center, Point2D firstArm, Point2D secondArm)
    {
        _centerPoint = center;
        _firstArmPoint = firstArm;
        _secondArmPoint = secondArm;
        
        Color = Color.Black;
        LineWeight = 1.0f;
    }
    
    /// <summary>
    /// Calculate the angle between the two arms in degrees
    /// </summary>
    public double CalculateAngle()
    {
        // Vector from center to first arm
        double dx1 = FirstArmPoint.X - CenterPoint.X;
        double dy1 = FirstArmPoint.Y - CenterPoint.Y;
        double angle1 = Math.Atan2(dy1, dx1);
        
        // Vector from center to second arm
        double dx2 = SecondArmPoint.X - CenterPoint.X;
        double dy2 = SecondArmPoint.Y - CenterPoint.Y;
        double angle2 = Math.Atan2(dy2, dx2);
        
        // Calculate angle difference
        double angleDiff = angle2 - angle1;
        
        // Normalize to 0-360 degrees
        double angleDegrees = angleDiff * 180.0 / Math.PI;
        if (angleDegrees < 0) angleDegrees += 360.0;
        if (angleDegrees > 360.0) angleDegrees -= 360.0;
        
        // Return the smaller angle (0-180)
        if (angleDegrees > 180.0)
        {
            angleDegrees = 360.0 - angleDegrees;
        }
        
        return angleDegrees;
    }
    
    /// <summary>
    /// Get the start and sweep angles for the arc in degrees
    /// </summary>
    public (double StartAngle, double SweepAngle) GetArcAngles()
    {
        // Vector from center to first arm
        double dx1 = FirstArmPoint.X - CenterPoint.X;
        double dy1 = FirstArmPoint.Y - CenterPoint.Y;
        double angle1 = Math.Atan2(dy1, dx1) * 180.0 / Math.PI;
        
        // Vector from center to second arm
        double dx2 = SecondArmPoint.X - CenterPoint.X;
        double dy2 = SecondArmPoint.Y - CenterPoint.Y;
        double angle2 = Math.Atan2(dy2, dx2) * 180.0 / Math.PI;
        
        // Normalize angles to 0-360
        if (angle1 < 0) angle1 += 360.0;
        if (angle2 < 0) angle2 += 360.0;
        
        // Calculate sweep angle (always counter-clockwise from angle1 to angle2)
        double sweepAngle = angle2 - angle1;
        if (sweepAngle < 0) sweepAngle += 360.0;
        
        // Use the smaller arc
        if (sweepAngle > 180.0)
        {
            sweepAngle = sweepAngle - 360.0;
        }
        
        return (angle1, sweepAngle);
    }
    
    /// <summary>
    /// Get the formatted angle text
    /// </summary>
    public string GetAngleText()
    {
        double angle = CalculateAngle();
        string format = $"F{DecimalPlaces}";
        return $"{angle.ToString(format)}°";
    }
    
    public override Rect2D GetBounds()
    {
        // Create bounding box around center and the arc
        double minX = Math.Min(CenterPoint.X, Math.Min(FirstArmPoint.X, SecondArmPoint.X)) - ArcRadius;
        double minY = Math.Min(CenterPoint.Y, Math.Min(FirstArmPoint.Y, SecondArmPoint.Y)) - ArcRadius;
        double maxX = Math.Max(CenterPoint.X, Math.Max(FirstArmPoint.X, SecondArmPoint.X)) + ArcRadius;
        double maxY = Math.Max(CenterPoint.Y, Math.Max(FirstArmPoint.Y, SecondArmPoint.Y)) + ArcRadius;
        
        return new Rect2D(minX - 20, minY - 20, (maxX - minX) + 40, (maxY - minY) + 40);
    }
    
    public override IReadOnlyList<IControlPoint> GetControlPoints()
    {
        return new List<IControlPoint>
        {
            new ControlPoint(this, ControlPointType.Middle, CenterPoint, 0),
            new ControlPoint(this, ControlPointType.Start, FirstArmPoint, 1),
            new ControlPoint(this, ControlPointType.End, SecondArmPoint, 2)
        };
    }
    
    public override bool HitTest(Point2D worldPoint, double tolerance)
    {
        // Check if point is near the arc
        double distanceToCenter = CenterPoint.DistanceTo(worldPoint);
        double arcDistanceDiff = Math.Abs(distanceToCenter - ArcRadius);
        
        if (arcDistanceDiff > tolerance)
            return false;
        
        // Check if point is within the arc sweep
        var angles = GetArcAngles();
        double dx = worldPoint.X - CenterPoint.X;
        double dy = worldPoint.Y - CenterPoint.Y;
        double pointAngle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        if (pointAngle < 0) pointAngle += 360.0;
        
        // Check if point angle is within arc sweep
        double relativeAngle = pointAngle - angles.StartAngle;
        if (relativeAngle < 0) relativeAngle += 360.0;
        
        bool withinArc = angles.SweepAngle > 0 
            ? (relativeAngle >= 0 && relativeAngle <= angles.SweepAngle)
            : (relativeAngle >= 360.0 + angles.SweepAngle && relativeAngle <= 360.0);
        
        return withinArc;
    }
    
    public override void UpdateControlPoint(IControlPoint controlPoint, Point2D newLocation)
    {
        switch (controlPoint.Type)
        {
            case ControlPointType.Middle:
                CenterPoint = newLocation;
                break;
            case ControlPointType.Start:
                FirstArmPoint = newLocation;
                break;
            case ControlPointType.End:
                SecondArmPoint = newLocation;
                break;
        }
    }
}

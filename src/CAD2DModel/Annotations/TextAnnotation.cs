using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Annotations;

/// <summary>
/// Text annotation with position and rotation
/// </summary>
public partial class TextAnnotation : AnnotationBase
{
    [ObservableProperty]
    private Point2D _position;
    
    [ObservableProperty]
    private string _text = "Text";
    
    [ObservableProperty]
    private string _fontFamily = "Arial";
    
    [ObservableProperty]
    private float _fontSize = 12.0f;
    
    [ObservableProperty]
    private bool _fontBold;
    
    [ObservableProperty]
    private bool _fontItalic;
    
    [ObservableProperty]
    private double _rotationDegrees;
    
    [ObservableProperty]
    private Color _textColor = Color.Black;
    
    [ObservableProperty]
    private bool _drawBackground;
    
    [ObservableProperty]
    private Color _backgroundColor = new Color(255, 255, 255, 200);
    
    [ObservableProperty]
    private bool _textOutline;
    
    [ObservableProperty]
    private bool _hasLeader;
    
    [ObservableProperty]
    private Point2D? _leaderEndPoint;
    
    [ObservableProperty]
    private ArrowStyle _leaderArrowStyle = ArrowStyle.FilledTriangle;
    
    [ObservableProperty]
    private double _arrowSize = 8.0;
    
    /// <summary>
    /// Text alignment horizontal
    /// </summary>
    [ObservableProperty]
    private TextAlign _textAlign = TextAlign.Left;
    
    public TextAnnotation()
    {
        _position = new Point2D(0, 0);
    }
    
    public TextAnnotation(Point2D position, string text = "Text")
    {
        _position = position;
        _text = text;
    }
    
    public override Rect2D GetBounds()
    {
        // Estimate text bounds - will be refined during rendering
        double estimatedWidth = Text.Length * FontSize * 0.6; // Rough estimate
        double estimatedHeight = FontSize * 1.5;
        
        // Account for rotation (use bounding box of rotated rectangle)
        double angleRad = RotationDegrees * Math.PI / 180.0;
        double cos = Math.Abs(Math.Cos(angleRad));
        double sin = Math.Abs(Math.Sin(angleRad));
        
        double rotatedWidth = estimatedWidth * cos + estimatedHeight * sin;
        double rotatedHeight = estimatedWidth * sin + estimatedHeight * cos;
        
        double minX = Position.X;
        double minY = Position.Y - rotatedHeight;
        
        // Include leader if present
        if (HasLeader && LeaderEndPoint.HasValue)
        {
            minX = Math.Min(minX, LeaderEndPoint.Value.X);
            minY = Math.Min(minY, LeaderEndPoint.Value.Y);
            rotatedWidth = Math.Max(rotatedWidth, Math.Abs(LeaderEndPoint.Value.X - Position.X));
            rotatedHeight = Math.Max(rotatedHeight, Math.Abs(LeaderEndPoint.Value.Y - Position.Y));
        }
        
        return new Rect2D(minX - 10, minY - 10, rotatedWidth + 20, rotatedHeight + 20);
    }
    
    public override IReadOnlyList<IControlPoint> GetControlPoints()
    {
        var controlPoints = new List<IControlPoint>
        {
            new ControlPoint(this, ControlPointType.Start, Position)
        };
        
        // Add rotation handle (offset from text position)
        double handleDistance = FontSize * 3;
        double angleRad = RotationDegrees * Math.PI / 180.0;
        Point2D rotationHandle = new Point2D(
            Position.X + handleDistance * Math.Cos(angleRad),
            Position.Y + handleDistance * Math.Sin(angleRad)
        );
        controlPoints.Add(new ControlPoint(this, ControlPointType.Rotation, rotationHandle));
        
        // Add leader end point if present
        if (HasLeader && LeaderEndPoint.HasValue)
        {
            controlPoints.Add(new ControlPoint(this, ControlPointType.End, LeaderEndPoint.Value));
        }
        
        return controlPoints;
    }
    
    public override bool HitTest(Point2D worldPoint, double tolerance)
    {
        // Simple hit test - check if point is near the text position
        // This is a rough approximation; actual rendering will have exact bounds
        Rect2D bounds = GetBounds();
        
        // Expand bounds by tolerance
        Rect2D testBounds = new Rect2D(
            bounds.X - tolerance,
            bounds.Y - tolerance,
            bounds.Width + 2 * tolerance,
            bounds.Height + 2 * tolerance
        );
        
        bool hitText = IsPointInRectangle(worldPoint, testBounds);
        
        // Check leader line if present
        if (!hitText && HasLeader && LeaderEndPoint.HasValue)
        {
            hitText = DistanceToLineSegment(worldPoint, Position, LeaderEndPoint.Value) <= tolerance;
        }
        
        return hitText;
    }
    
    public override void UpdateControlPoint(IControlPoint controlPoint, Point2D newLocation)
    {
        switch (controlPoint.Type)
        {
            case ControlPointType.Start:
                Position = newLocation;
                break;
                
            case ControlPointType.Rotation:
                // Update rotation based on angle to the new handle position
                double dx = newLocation.X - Position.X;
                double dy = newLocation.Y - Position.Y;
                RotationDegrees = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                break;
                
            case ControlPointType.End:
                if (HasLeader)
                {
                    LeaderEndPoint = newLocation;
                }
                break;
        }
    }
}

using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Annotations;

/// <summary>
/// Linear dimension annotation with offset line (engineering-style dimension)
/// </summary>
public partial class DimensionAnnotation : LinearAnnotation
{
    [ObservableProperty]
    private Point2D _offsetPoint;
    
    [ObservableProperty]
    private double _extensionLineOvershoot = 2.0;
    
    [ObservableProperty]
    private double _dimensionLineOffset = 10.0;
    
    [ObservableProperty]
    private int _decimalPlaces = 2;
    
    [ObservableProperty]
    private bool _showUnits = true;
    
    [ObservableProperty]
    private string _units = "mm";
    
    [ObservableProperty]
    private DimensionStyle _dimensionStyleType = DimensionStyle.Linear;
    
    public DimensionAnnotation() : base()
    {
        _offsetPoint = new Point2D(0, 0);
        InitializeDimensionDefaults();
    }
    
    public DimensionAnnotation(Point2D start, Point2D end, Point2D offsetPoint) : base(start, end)
    {
        _offsetPoint = offsetPoint;
        InitializeDimensionDefaults();
    }
    
    private void InitializeDimensionDefaults()
    {
        // Dimension appearance defaults
        ArrowAtHead = true;
        ArrowAtTail = true;
        ArrowHeadStyle = ArrowStyle.FilledTriangle;
        ArrowTailStyle = ArrowStyle.FilledTriangle;
        ArrowSize = 8.0;
        
        Color = Color.Black;
        LineWeight = 1.0f;
        FontSize = 9f;
        FontFamily = "Arial";
        DrawTextBackground = true;
    }
    
    /// <summary>
    /// Get dimension points for rendering (extension lines and dimension line)
    /// </summary>
    public (Point2D ExtLine1Start, Point2D ExtLine1End, Point2D ExtLine2Start, Point2D ExtLine2End, 
            Point2D DimLineStart, Point2D DimLineEnd) GetDimensionPoints()
    {
        // Calculate the dimension line direction (perpendicular to measured line)
        double dx = EndPoint.X - StartPoint.X;
        double dy = EndPoint.Y - StartPoint.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        
        if (length < 0.001)
        {
            // Degenerate case
            return (StartPoint, StartPoint, EndPoint, EndPoint, StartPoint, EndPoint);
        }
        
        // Unit vector along measured line
        double ux = dx / length;
        double uy = dy / length;
        
        // Perpendicular vector (rotated 90 degrees)
        double perpX = -uy;
        double perpY = ux;
        
        // Calculate offset distance from measured line to dimension line
        // Use the offset point to determine direction and distance
        double offsetDist = DimensionLineOffset;
        
        // Determine if offset point is above or below the line
        double cross = (OffsetPoint.X - StartPoint.X) * perpY - (OffsetPoint.Y - StartPoint.Y) * perpX;
        if (cross < 0)
        {
            offsetDist = -offsetDist;
        }
        
        // Extension line endpoints (from measured points to beyond dimension line)
        Point2D extLine1Start = StartPoint;
        Point2D extLine1End = new Point2D(
            StartPoint.X + perpX * (offsetDist + ExtensionLineOvershoot),
            StartPoint.Y + perpY * (offsetDist + ExtensionLineOvershoot)
        );
        
        Point2D extLine2Start = EndPoint;
        Point2D extLine2End = new Point2D(
            EndPoint.X + perpX * (offsetDist + ExtensionLineOvershoot),
            EndPoint.Y + perpY * (offsetDist + ExtensionLineOvershoot)
        );
        
        // Dimension line endpoints
        Point2D dimLineStart = new Point2D(
            StartPoint.X + perpX * offsetDist,
            StartPoint.Y + perpY * offsetDist
        );
        
        Point2D dimLineEnd = new Point2D(
            EndPoint.X + perpX * offsetDist,
            EndPoint.Y + perpY * offsetDist
        );
        
        return (extLine1Start, extLine1End, extLine2Start, extLine2End, dimLineStart, dimLineEnd);
    }
    
    /// <summary>
    /// Get the formatted dimension text
    /// </summary>
    public string GetDimensionText()
    {
        double distance = Length;
        
        // Format the number
        string format = $"F{DecimalPlaces}";
        string measurement = distance.ToString(format);
        
        // Add units if requested
        if (ShowUnits)
        {
            measurement += " " + Units;
        }
        
        return measurement;
    }
    
    public override IReadOnlyList<IControlPoint> GetControlPoints()
    {
        return new List<IControlPoint>
        {
            new ControlPoint(this, ControlPointType.Start, StartPoint, 0),
            new ControlPoint(this, ControlPointType.End, EndPoint, 1),
            new ControlPoint(this, ControlPointType.Third, OffsetPoint, 2)
        };
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
            case ControlPointType.Third:
                OffsetPoint = newLocation;
                break;
        }
    }
    
    /// <summary>
    /// Override to automatically update text with measurement
    /// </summary>
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        // Update text when relevant properties change
        if (e.PropertyName == nameof(StartPoint) ||
            e.PropertyName == nameof(EndPoint) ||
            e.PropertyName == nameof(DecimalPlaces) ||
            e.PropertyName == nameof(ShowUnits))
        {
            Text = GetDimensionText();
        }
    }
}

using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for adding ruler annotations
/// </summary>
public class AddRulerMode : AddAnnotationModeBase
{
    public AddRulerMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Ruler";
    
    public override string StatusPrompt
    {
        get
        {
            return _capturedPoints.Count switch
            {
                0 => "Click first measurement point",
                1 => "Click second measurement point",
                2 => "Click to place ruler dimension line",
                _ => "Creating ruler..."
            };
        }
    }
    
    protected override int RequiredPointCount => 3;
    
    protected override IAnnotation CreateAnnotation(List<Point2D> points)
    {
        // Use first two points for measurement, third point for offset/positioning
        var ruler = new RulerAnnotation(points[0], points[1]);
        ruler.ShowUnits = true;
        ruler.DecimalPlaces = 2;
        ruler.Text = ruler.GetMeasurementText();
        
        // The third point determines where the dimension line is drawn (offset from the measured line)
        // This is visually similar to how DimensionAnnotation works
        return ruler;
    }
    
    protected override void DrawPreview(IRenderContext context)
    {
        if (_capturedPoints.Count == 1)
        {
            // Draw preview line from first point to current mouse position
            context.DrawLine(_capturedPoints[0], _currentMousePosition, 150, 150, 150, 1, true);
        }
        else if (_capturedPoints.Count == 2)
        {
            // Draw the measured line
            context.DrawLine(_capturedPoints[0], _capturedPoints[1], 150, 150, 150, 1, false);
            
            // Draw preview of offset dimension line
            // Calculate perpendicular direction
            double dx = _capturedPoints[1].X - _capturedPoints[0].X;
            double dy = _capturedPoints[1].Y - _capturedPoints[0].Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            
            if (length > 0.001)
            {
                double perpX = -dy / length;
                double perpY = dx / length;
                
                // Determine offset based on mouse position
                double offsetSign = (((_currentMousePosition.X - _capturedPoints[0].X) * perpY - 
                                      (_currentMousePosition.Y - _capturedPoints[0].Y) * perpX) > 0) ? 1 : -1;
                double offsetDist = 20.0 * offsetSign;
                
                // Extension lines
                Point2D ext1End = new Point2D(
                    _capturedPoints[0].X + perpX * offsetDist * 1.2,
                    _capturedPoints[0].Y + perpY * offsetDist * 1.2
                );
                Point2D ext2End = new Point2D(
                    _capturedPoints[1].X + perpX * offsetDist * 1.2,
                    _capturedPoints[1].Y + perpY * offsetDist * 1.2
                );
                
                context.DrawLine(_capturedPoints[0], ext1End, 150, 150, 150, 0.5f, true);
                context.DrawLine(_capturedPoints[1], ext2End, 150, 150, 150, 0.5f, true);
                
                // Dimension line with measurement
                Point2D dimStart = new Point2D(
                    _capturedPoints[0].X + perpX * offsetDist,
                    _capturedPoints[0].Y + perpY * offsetDist
                );
                Point2D dimEnd = new Point2D(
                    _capturedPoints[1].X + perpX * offsetDist,
                    _capturedPoints[1].Y + perpY * offsetDist
                );
                
                context.DrawLine(dimStart, dimEnd, 150, 150, 150, 1, false);
                
                // Draw end caps
                context.DrawLine(
                    new Point2D(dimStart.X - perpY * 5, dimStart.Y + perpX * 5),
                    new Point2D(dimStart.X + perpY * 5, dimStart.Y - perpX * 5),
                    150, 150, 150, 1, false);
                context.DrawLine(
                    new Point2D(dimEnd.X - perpY * 5, dimEnd.Y + perpX * 5),
                    new Point2D(dimEnd.X + perpY * 5, dimEnd.Y - perpX * 5),
                    150, 150, 150, 1, false);
            }
        }
        
        // Show snap indicator
        DrawSnapIndicatorIfSnapped(context);
    }
}

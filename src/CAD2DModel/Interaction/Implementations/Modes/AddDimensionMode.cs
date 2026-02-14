using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for adding linear dimension annotations (3 points: start, end, offset)
/// </summary>
public class AddDimensionMode : AddAnnotationModeBase
{
    public AddDimensionMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Dimension";
    
    public override string StatusPrompt
    {
        get
        {
            return _capturedPoints.Count switch
            {
                0 => "Click first extension line origin",
                1 => "Click second extension line origin",
                2 => "Click to place dimension line",
                _ => "Creating dimension..."
            };
        }
    }
    
    protected override int RequiredPointCount => 3;
    
    protected override IAnnotation CreateAnnotation(List<Point2D> points)
    {
        var dimension = new DimensionAnnotation(points[0], points[1], points[2]);
        dimension.ShowUnits = true;
        dimension.DecimalPlaces = 2;
        dimension.Text = dimension.GetDimensionText();
        return dimension;
    }
    
    protected override void DrawPreview(IRenderContext context)
    {
        if (_capturedPoints.Count == 1)
        {
            // Draw preview line from first point to current position
            context.DrawLine(_capturedPoints[0], _currentMousePosition, 150, 150, 150, 1, true);
        }
        else if (_capturedPoints.Count == 2)
        {
            // Draw preview of dimension with extension lines
            context.DrawLine(_capturedPoints[0], _capturedPoints[1], 150, 150, 150, 1, false);
            
            // Draw preview extension lines and dimension line
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
                
                context.DrawLine(_capturedPoints[0], ext1End, 150, 150, 150, 0.5f, false);
                context.DrawLine(_capturedPoints[1], ext2End, 150, 150, 150, 0.5f, false);
                
                // Dimension line
                Point2D dimStart = new Point2D(
                    _capturedPoints[0].X + perpX * offsetDist,
                    _capturedPoints[0].Y + perpY * offsetDist
                );
                Point2D dimEnd = new Point2D(
                    _capturedPoints[1].X + perpX * offsetDist,
                    _capturedPoints[1].Y + perpY * offsetDist
                );
                
                context.DrawLine(dimStart, dimEnd, 150, 150, 150, 1, false);
                context.DrawArrowHead(dimEnd, dimStart, 150, 150, 150, 8.0, true);
                context.DrawArrowHead(dimStart, dimEnd, 150, 150, 150, 8.0, true);
            }
        }
        
        // Show snap indicator
        DrawSnapIndicatorIfSnapped(context);
    }
}

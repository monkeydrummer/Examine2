using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for adding angular dimension annotations (3 points: center, first arm, second arm)
/// </summary>
public class AddAngularDimensionMode : AddAnnotationModeBase
{
    public AddAngularDimensionMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Angular Dimension";
    
    public override string StatusPrompt
    {
        get
        {
            return _capturedPoints.Count switch
            {
                0 => "Click angle vertex (center point)",
                1 => "Click first line point",
                2 => "Click second line point to complete angle",
                _ => "Creating angular dimension..."
            };
        }
    }
    
    protected override int RequiredPointCount => 3;
    
    protected override IAnnotation CreateAnnotation(List<Point2D> points)
    {
        var angularDim = new AngularDimensionAnnotation(points[0], points[1], points[2]);
        angularDim.ArcRadius = 30.0;
        return angularDim;
    }
    
    protected override void DrawPreview(IRenderContext context)
    {
        if (_capturedPoints.Count == 1)
        {
            // Draw preview line from center to mouse
            context.DrawLine(_capturedPoints[0], _currentMousePosition, 150, 150, 150, 1, true);
        }
        else if (_capturedPoints.Count == 2)
        {
            // Draw first arm
            context.DrawLine(_capturedPoints[0], _capturedPoints[1], 150, 150, 150, 1, false);
            
            // Draw preview second arm
            context.DrawLine(_capturedPoints[0], _currentMousePosition, 150, 150, 150, 1, true);
            
            // Draw preview arc
            double radius = 30.0;
            double dx1 = _capturedPoints[1].X - _capturedPoints[0].X;
            double dy1 = _capturedPoints[1].Y - _capturedPoints[0].Y;
            double angle1 = Math.Atan2(dy1, dx1) * 180.0 / Math.PI;
            
            double dx2 = _currentMousePosition.X - _capturedPoints[0].X;
            double dy2 = _currentMousePosition.Y - _capturedPoints[0].Y;
            double angle2 = Math.Atan2(dy2, dx2) * 180.0 / Math.PI;
            
            if (angle1 < 0) angle1 += 360.0;
            if (angle2 < 0) angle2 += 360.0;
            
            double sweepAngle = angle2 - angle1;
            if (sweepAngle < 0) sweepAngle += 360.0;
            if (sweepAngle > 180.0) sweepAngle = sweepAngle - 360.0;
            
            context.DrawArc(_capturedPoints[0], radius, angle1, sweepAngle, 150, 150, 150, 1);
        }
        
        // Show snap indicator
        DrawSnapIndicatorIfSnapped(context);
    }
}

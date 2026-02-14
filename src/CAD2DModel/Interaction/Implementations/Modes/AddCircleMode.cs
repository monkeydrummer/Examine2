using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for adding circle annotations (2 points: center and edge)
/// </summary>
public class AddCircleMode : AddAnnotationModeBase
{
    public AddCircleMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Circle";
    
    public override string StatusPrompt
    {
        get
        {
            return _capturedPoints.Count switch
            {
                0 => "Click circle center",
                1 => "Click to set radius",
                _ => "Creating circle..."
            };
        }
    }
    
    protected override int RequiredPointCount => 2;
    
    protected override IAnnotation CreateAnnotation(List<Point2D> points)
    {
        double dx = points[1].X - points[0].X;
        double dy = points[1].Y - points[0].Y;
        double radius = Math.Sqrt(dx * dx + dy * dy);
        
        return new EllipseAnnotation(points[0], radius, radius);
    }
    
    protected override void DrawPreview(IRenderContext context)
    {
        if (_capturedPoints.Count == 1)
        {
            double dx = _currentMousePosition.X - _capturedPoints[0].X;
            double dy = _currentMousePosition.Y - _capturedPoints[0].Y;
            double radius = Math.Sqrt(dx * dx + dy * dy);
            
            context.DrawCircle(_capturedPoints[0], radius, 150, 150, 150, 1, false);
            
            // Draw radius line
            context.DrawLine(_capturedPoints[0], _currentMousePosition, 100, 100, 100, 0.5f, true);
            DrawSnapIndicatorIfSnapped(context);
        }
    }
}

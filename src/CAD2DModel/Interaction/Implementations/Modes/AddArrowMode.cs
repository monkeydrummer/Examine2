using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for adding arrow annotations
/// </summary>
public class AddArrowMode : AddAnnotationModeBase
{
    public AddArrowMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Arrow";
    
    public override string StatusPrompt
    {
        get
        {
            return _capturedPoints.Count switch
            {
                0 => "Click arrow start point",
                1 => "Click arrow end point (arrow head location)",
                _ => "Creating arrow..."
            };
        }
    }
    
    protected override int RequiredPointCount => 2;
    
    protected override IAnnotation CreateAnnotation(List<Point2D> points)
    {
        var arrow = new ArrowAnnotation(points[0], points[1]);
        return arrow;
    }
    
    protected override void DrawPreview(IRenderContext context)
    {
        if (_capturedPoints.Count == 1)
        {
            // Draw preview line
            context.DrawLine(_capturedPoints[0], _currentMousePosition, 150, 150, 150, 2, false);
            
            // Draw preview arrow head
            context.DrawArrowHead(_capturedPoints[0], _currentMousePosition, 150, 150, 150, 12.0, true);
            
            // Show snap indicator
            DrawSnapIndicatorIfSnapped(context);
        }
    }
}

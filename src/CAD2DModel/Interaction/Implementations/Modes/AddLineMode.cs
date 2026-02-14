using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for adding simple line annotations
/// </summary>
public class AddLineMode : AddAnnotationModeBase
{
    public AddLineMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Line";
    
    public override string StatusPrompt
    {
        get
        {
            return _capturedPoints.Count switch
            {
                0 => "Click first point of line",
                1 => "Click second point to complete line",
                _ => "Creating line..."
            };
        }
    }
    
    protected override int RequiredPointCount => 2;
    
    protected override IAnnotation CreateAnnotation(List<Point2D> points)
    {
        return new LinearAnnotation(points[0], points[1]);
    }
    
    protected override void DrawPreview(IRenderContext context)
    {
        if (_capturedPoints.Count == 1)
        {
            context.DrawLine(_capturedPoints[0], _currentMousePosition, 150, 150, 150, 1, false);
            DrawSnapIndicatorIfSnapped(context);
        }
    }
}

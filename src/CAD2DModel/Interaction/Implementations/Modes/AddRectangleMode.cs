using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for adding rectangle annotations (2 points: opposite corners)
/// </summary>
public class AddRectangleMode : AddAnnotationModeBase
{
    public AddRectangleMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Rectangle";
    
    public override string StatusPrompt
    {
        get
        {
            return _capturedPoints.Count switch
            {
                0 => "Click first corner of rectangle",
                1 => "Click opposite corner to complete rectangle",
                _ => "Creating rectangle..."
            };
        }
    }
    
    protected override int RequiredPointCount => 2;
    
    protected override IAnnotation CreateAnnotation(List<Point2D> points)
    {
        double minX = Math.Min(points[0].X, points[1].X);
        double minY = Math.Min(points[0].Y, points[1].Y);
        double maxX = Math.Max(points[0].X, points[1].X);
        double maxY = Math.Max(points[0].Y, points[1].Y);
        
        return new RectangleAnnotation(
            new Point2D(minX, minY),
            new Point2D(maxX, maxY)
        );
    }
    
    protected override void DrawPreview(IRenderContext context)
    {
        if (_capturedPoints.Count == 1)
        {
            double minX = Math.Min(_capturedPoints[0].X, _currentMousePosition.X);
            double minY = Math.Min(_capturedPoints[0].Y, _currentMousePosition.Y);
            double width = Math.Abs(_currentMousePosition.X - _capturedPoints[0].X);
            double height = Math.Abs(_currentMousePosition.Y - _capturedPoints[0].Y);
            
            context.DrawRectangle(new Point2D(minX, minY), width, height, 150, 150, 150, 1, false);
            DrawSnapIndicatorIfSnapped(context);
        }
    }
}

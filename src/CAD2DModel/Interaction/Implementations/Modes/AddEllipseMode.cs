using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for adding ellipse annotations (3 points: center, edge for radius X, edge for radius Y)
/// </summary>
public class AddEllipseMode : AddAnnotationModeBase
{
    public AddEllipseMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Ellipse";
    
    public override string StatusPrompt
    {
        get
        {
            return _capturedPoints.Count switch
            {
                0 => "Click ellipse center",
                1 => "Click to set horizontal radius",
                2 => "Click to set vertical radius",
                _ => "Creating ellipse..."
            };
        }
    }
    
    protected override int RequiredPointCount => 3;
    
    protected override IAnnotation CreateAnnotation(List<Point2D> points)
    {
        double radiusX = Math.Abs(points[1].X - points[0].X);
        double radiusY = Math.Abs(points[2].Y - points[0].Y);
        
        return new EllipseAnnotation(points[0], radiusX, radiusY);
    }
    
    protected override void DrawPreview(IRenderContext context)
    {
        if (_capturedPoints.Count == 1)
        {
            double radiusX = Math.Abs(_currentMousePosition.X - _capturedPoints[0].X);
            
            // Draw circle preview for first radius
            context.DrawCircle(_capturedPoints[0], radiusX, 150, 150, 150, 1, true);
            
            // Draw radius line
            context.DrawLine(_capturedPoints[0], 
                new Point2D(_currentMousePosition.X, _capturedPoints[0].Y), 
                100, 100, 100, 0.5f, true);
        }
        else if (_capturedPoints.Count == 2)
        {
            double radiusX = Math.Abs(_capturedPoints[1].X - _capturedPoints[0].X);
            double radiusY = Math.Abs(_currentMousePosition.Y - _capturedPoints[0].Y);
            
            // Draw ellipse preview
            // Note: IRenderContext would need an ellipse drawing method, for now use approximation
            context.DrawCircle(_capturedPoints[0], radiusX, 150, 150, 150, 1, true);
            
            // Draw vertical radius line
            context.DrawLine(_capturedPoints[0], 
                new Point2D(_capturedPoints[0].X, _currentMousePosition.Y), 
                100, 100, 100, 0.5f, true);
        }
        
        DrawSnapIndicatorIfSnapped(context);
    }
}

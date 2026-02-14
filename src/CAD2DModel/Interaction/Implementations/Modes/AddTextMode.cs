using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for adding text annotations (1 point for position, then text entry)
/// </summary>
public class AddTextMode : AddAnnotationModeBase
{
    private string _textToAdd = "Sample Text";
    
    public AddTextMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Text";
    
    public override string StatusPrompt
    {
        get
        {
            return _capturedPoints.Count switch
            {
                0 => "Click text position",
                _ => "Creating text..."
            };
        }
    }
    
    protected override int RequiredPointCount => 1;
    
    /// <summary>
    /// Set the text content before entering this mode, or it will use "Sample Text"
    /// </summary>
    public void SetText(string text)
    {
        _textToAdd = text ?? "Sample Text";
    }
    
    protected override IAnnotation CreateAnnotation(List<Point2D> points)
    {
        var textAnnotation = new TextAnnotation(points[0], _textToAdd);
        textAnnotation.FontSize = 12.0f;
        textAnnotation.TextAlign = TextAlign.Left;
        textAnnotation.DrawBackground = false;
        return textAnnotation;
    }
    
    protected override void DrawPreview(IRenderContext context)
    {
        // Text doesn't have a preview since it's created in one click
        // Optionally could show where the text will be placed
        if (_capturedPoints.Count == 0)
        {
            DrawSnapIndicatorIfSnapped(context);
        }
    }
    
    // Override to prompt for text after clicking position
    protected override void CreateAndAddAnnotation()
    {
        // In a real implementation, this would show a dialog to enter text
        // For now, we'll create with default text
        // TODO: Show text input dialog before creating annotation
        
        base.CreateAndAddAnnotation();
    }
}

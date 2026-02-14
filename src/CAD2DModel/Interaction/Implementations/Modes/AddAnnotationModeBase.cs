using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;
using System.Windows.Input;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Base class for modes that add annotations interactively
/// </summary>
public abstract class AddAnnotationModeBase : InteractionModeBase
{
    protected readonly IModeManager _modeManager;
    protected readonly ICommandManager _commandManager;
    protected readonly IGeometryModel _geometryModel;
    protected readonly ISnapService _snapService;
    
    protected readonly List<Point2D> _capturedPoints = new();
    protected Point2D _currentMousePosition;
    protected Camera.Camera2D? _camera;
    
    public AddAnnotationModeBase(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
    {
        _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
        _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        _geometryModel = geometryModel ?? throw new ArgumentNullException(nameof(geometryModel));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
    }
    
    /// <summary>
    /// Default cursor for annotation creation (crosshair)
    /// </summary>
    public override Cursor Cursor => CAD2DModel.Interaction.Cursor.Cross;
    
    /// <summary>
    /// Number of points required to create this annotation
    /// </summary>
    protected abstract int RequiredPointCount { get; }
    
    /// <summary>
    /// Create the annotation from the captured points
    /// </summary>
    protected abstract IAnnotation CreateAnnotation(List<Point2D> points);
    
    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _camera = context.Camera;
        _capturedPoints.Clear();
        State = ModeState.WaitingForInput;
    }
    
    public override void OnMouseMove(Point2D worldPoint, ModifierKeys modifiers)
    {
        // Apply snapping (including to annotation control points)
        if (_camera != null)
        {
            var snapResult = _snapService.Snap(worldPoint, _geometryModel.Entities, _geometryModel.Annotations, _camera);
            _currentMousePosition = snapResult.SnappedPoint;
        }
        else
        {
            _currentMousePosition = worldPoint;
        }
        
        // Always show preview/snap indicator (even before first click)
        State = ModeState.Active;
        
        // Trigger redraw to update preview and snap indicators
        _geometryModel.NotifyGeometryChanged();
    }
    
    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button == MouseButton.Left)
        {
            // Apply snapping (including to annotation control points)
            Point2D snappedPoint = worldPoint;
            if (_camera != null)
            {
                var snapResult = _snapService.Snap(worldPoint, _geometryModel.Entities, _geometryModel.Annotations, _camera);
                snappedPoint = snapResult.SnappedPoint;
            }
            
            // Capture the point
            _capturedPoints.Add(snappedPoint);
            
            // Check if we have enough points
            if (_capturedPoints.Count >= RequiredPointCount)
            {
                CreateAndAddAnnotation();
            }
        }
        else if (button == MouseButton.Right)
        {
            // Cancel
            CancelAndReturnToIdle();
        }
    }
    
    public override void OnKeyDown(Key key, ModifierKeys modifiers)
    {
        if (key == Key.Escape)
        {
            CancelAndReturnToIdle();
        }
        else if (key == Key.Enter && CanComplete())
        {
            CreateAndAddAnnotation();
        }
    }
    
    protected virtual bool CanComplete()
    {
        return _capturedPoints.Count >= RequiredPointCount;
    }
    
    protected virtual void CreateAndAddAnnotation()
    {
        try
        {
            var annotation = CreateAnnotation(_capturedPoints);
            
            // Add to model
            _geometryModel.Annotations.Add(annotation);
            
            // Return to idle mode
            State = ModeState.Completed;
            _modeManager.EnterIdleMode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating annotation: {ex.Message}");
            CancelAndReturnToIdle();
        }
    }
    
    protected void CancelAndReturnToIdle()
    {
        State = ModeState.Cancelled;
        _capturedPoints.Clear();
        _modeManager.EnterIdleMode();
    }
    
    public override void Render(IRenderContext context)
    {
        // Draw captured points
        foreach (var point in _capturedPoints)
        {
            context.DrawControlPoint(point, 0, 200, 0); // Green for captured points
        }
        
        // Always draw snap indicator if we're hovering (even before first click)
        if (_capturedPoints.Count == 0)
        {
            DrawSnapIndicatorIfSnapped(context);
        }
        
        // Draw preview if we have at least one point
        if (_capturedPoints.Count > 0)
        {
            DrawPreview(context);
        }
    }
    
    /// <summary>
    /// Helper method to draw snap indicator in preview methods
    /// </summary>
    protected void DrawSnapIndicatorIfSnapped(IRenderContext context)
    {
        if (_camera != null)
        {
            var snapResult = _snapService.Snap(_currentMousePosition, _geometryModel.Entities, _geometryModel.Annotations, _camera);
            if (snapResult.IsSnapped)
            {
                context.DrawSnapIndicator(snapResult.SnappedPoint, snapResult.SnapType);
            }
        }
    }
    
    /// <summary>
    /// Draw preview of the annotation being created
    /// </summary>
    protected abstract void DrawPreview(IRenderContext context);
}

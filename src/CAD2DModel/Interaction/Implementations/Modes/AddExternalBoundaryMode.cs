using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for creating new ExternalBoundary entities by clicking two corner points
/// Automatically creates a rectangle from the two corners
/// </summary>
public class AddExternalBoundaryMode : InteractionModeBase
{
    private readonly IModeManager _modeManager;
    private readonly ICommandManager _commandManager;
    private readonly IGeometryModel _geometryModel;
    private readonly ISnapService _snapService;
    private ExternalBoundary? _currentBoundary;
    private Point2D? _firstCorner;
    private Point2D _currentMousePosition;
    private SnapResult? _currentSnapResult;
    private Camera.Camera2D? _camera;
    
    public AddExternalBoundaryMode(
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
    
    public override string Name => "Add External Boundary";
    public override Cursor Cursor => Interaction.Cursor.Cross;
    
    public override string StatusPrompt
    {
        get
        {
            if (_firstCorner == null)
                return "Click to place first corner of rectangular external boundary";
            else
                return "Click to place opposite corner to complete rectangle";
        }
    }
    
    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _firstCorner = null;
        _currentBoundary = null;
        _camera = context.Camera;
        State = ModeState.WaitingForInput;
    }
    
    public override void OnExit()
    {
        // Clean up temporary boundary if not completed
        if (_currentBoundary != null && State != ModeState.Completed)
        {
            _geometryModel.RemoveEntity(_currentBoundary);
        }
        
        _firstCorner = null;
        _currentBoundary = null;
        base.OnExit();
    }
    
    public override void OnMouseMove(Point2D worldPoint, ModifierKeys modifiers)
    {
        // Track mouse position for rubber-band line preview
        _currentMousePosition = worldPoint;
        
        // Apply snapping for preview (only if camera is available)
        if (_camera != null)
        {
            _currentSnapResult = _snapService.Snap(worldPoint, _geometryModel.Entities, _camera);
            if (_currentSnapResult != null && _currentSnapResult.IsSnapped)
            {
                _currentMousePosition = _currentSnapResult.SnappedPoint;
            }
            else
            {
                _currentSnapResult = null;
            }
            
            // Apply ortho snap if enabled and we have first corner
            if ((_snapService.ActiveSnapModes & SnapMode.Ortho) != 0 && _firstCorner.HasValue)
            {
                var orthoResult = _snapService.SnapToOrtho(_currentMousePosition, _firstCorner.Value);
                if (orthoResult.IsSnapped)
                {
                    _currentMousePosition = orthoResult.SnappedPoint;
                }
            }
        }
    }
    
    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button == MouseButton.Left)
        {
            // Apply snapping
            Point2D point = worldPoint;
            if (_camera != null)
            {
                var snapResult = _snapService.Snap(worldPoint, _geometryModel.Entities, _camera);
                if (snapResult != null && snapResult.IsSnapped)
                {
                    point = snapResult.SnappedPoint;
                }
                
                // Apply ortho snap if enabled and we have first corner
                if ((_snapService.ActiveSnapModes & SnapMode.Ortho) != 0 && _firstCorner.HasValue)
                {
                    var orthoResult = _snapService.SnapToOrtho(point, _firstCorner.Value);
                    if (orthoResult.IsSnapped)
                    {
                        point = orthoResult.SnappedPoint;
                    }
                }
            }
            
            if (_firstCorner == null)
            {
                // First corner - just store it
                _firstCorner = point;
                State = ModeState.Active;
            }
            else
            {
                // Second corner - create rectangle with all 4 corners
                var corner1 = _firstCorner.Value;
                var corner2 = point;
                
                // Create the four corners of the rectangle
                var rectanglePoints = new List<Point2D>
                {
                    corner1,                                    // Bottom-left or specified first corner
                    new Point2D(corner2.X, corner1.Y),          // Bottom-right or top-left
                    corner2,                                     // Top-right or specified second corner
                    new Point2D(corner1.X, corner2.Y)           // Top-left or bottom-right
                };
                
                // Create the boundary with all 4 points
                _currentBoundary = new ExternalBoundary
                {
                    Name = $"External Boundary {_geometryModel.Entities.OfType<ExternalBoundary>().Count() + 1}",
                    IsClosed = true
                };
                
                foreach (var p in rectanglePoints)
                {
                    _currentBoundary.AddVertex(p);
                }
                
                // Apply rules to the boundary
                _geometryModel.ApplyRulesToEntity(_currentBoundary);
                
                // Add to model through command manager for proper undo/redo support
                var command = new AddEntityCommand(_geometryModel, _currentBoundary);
                _commandManager.Execute(command);
                
                State = ModeState.Completed;
                _modeManager.ReturnToIdle();
            }
        }
        else if (button == MouseButton.Right)
        {
            // Right-click cancels
            if (_firstCorner != null)
            {
                _firstCorner = null;
                State = ModeState.WaitingForInput;
            }
        }
    }
    
    public override void OnMouseUp(Point2D worldPoint, MouseButton button)
    {
        // Not needed for this mode
    }
    
    public override void OnKeyDown(Key key, ModifierKeys modifiers)
    {
        switch (key)
        {
            case Key.Escape:
                // Cancel boundary creation
                if (_currentBoundary != null)
                {
                    _geometryModel.RemoveEntity(_currentBoundary);
                    _currentBoundary = null;
                }
                _firstCorner = null;
                _modeManager.ReturnToIdle();
                break;
        }
    }
    
    public override void Render(IRenderContext context)
    {
        if (_firstCorner != null)
        {
            // Draw preview rectangle from first corner to current mouse position
            var corner1 = _firstCorner.Value;
            var corner2 = _currentMousePosition;
            
            // Draw the four sides of the rectangle
            context.DrawLine(corner1, new Point2D(corner2.X, corner1.Y), 128, 128, 128, 1, dashed: true);
            context.DrawLine(new Point2D(corner2.X, corner1.Y), corner2, 128, 128, 128, 1, dashed: true);
            context.DrawLine(corner2, new Point2D(corner1.X, corner2.Y), 128, 128, 128, 1, dashed: true);
            context.DrawLine(new Point2D(corner1.X, corner2.Y), corner1, 128, 128, 128, 1, dashed: true);
        }
        
        // Draw snap indicator if snapped
        if (_currentSnapResult != null && _currentSnapResult.IsSnapped)
        {
            context.DrawSnapIndicator(_currentSnapResult.SnappedPoint, _currentSnapResult.SnapType);
        }
    }
    
    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        var items = new List<IContextMenuItem>();
        
        items.Add(new SelectModeContextMenuItem
        {
            Text = "Cancel (Esc)",
            Action = () => OnKeyDown(Key.Escape, ModifierKeys.None)
        });
        
        return items;
    }
}

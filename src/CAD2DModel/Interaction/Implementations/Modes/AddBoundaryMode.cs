using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for creating new boundary entities by clicking points
/// </summary>
public class AddBoundaryMode : InteractionModeBase
{
    private readonly IModeManager _modeManager;
    private readonly ICommandManager _commandManager;
    private readonly IGeometryModel _geometryModel;
    private readonly ISnapService _snapService;
    private Boundary? _currentBoundary;
    private readonly List<Point2D> _points = new();
    private Point2D _currentMousePosition;
    private SnapResult? _currentSnapResult;
    private Camera.Camera2D? _camera;
    
    public AddBoundaryMode(
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
    
    public override string Name => "Add Boundary";
    public override Cursor Cursor => Interaction.Cursor.Cross;
    
    public override string StatusPrompt
    {
        get
        {
            if (_points.Count == 0)
                return "Click to place first vertex";
            else if (_points.Count == 1)
                return "Click to place second vertex";
            else if (_points.Count == 2)
                return "Click to place third vertex (minimum for boundary)";
            else
                return $"Click to add vertex ({_points.Count} vertices), Enter to finish, Esc to cancel";
        }
    }
    
    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _points.Clear();
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
        
        _points.Clear();
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
        }
    }
    
    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button == MouseButton.Left)
        {
            // Apply snapping (only if camera is available)
            Point2D point = worldPoint;
            if (_camera != null)
            {
                var snapResult = _snapService.Snap(worldPoint, _geometryModel.Entities, _camera);
                point = snapResult != null ? snapResult.SnappedPoint : worldPoint;
            }
            
            AddPoint(point);
        }
        else if (button == MouseButton.Right)
        {
            // Right-click to finish or cancel
            if (_points.Count >= 3)
            {
                FinishBoundary();
            }
            else
            {
                CancelBoundary();
            }
        }
    }
    
    public override void OnKeyDown(Key key, ModifierKeys modifiers)
    {
        switch (key)
        {
            case Key.Enter:
                if (_points.Count >= 3)
                {
                    FinishBoundary();
                }
                break;
                
            case Key.Escape:
                CancelBoundary();
                break;
                
            case Key.Backspace:
                // Remove last point
                if (_points.Count > 0)
                {
                    _points.RemoveAt(_points.Count - 1);
                    UpdateCurrentBoundary();
                }
                break;
        }
    }
    
    private void AddPoint(Point2D point)
    {
        _points.Add(point);
        
        if (_currentBoundary == null && _points.Count >= 2)
        {
            // Create boundary entity and add to model as we build it
            _currentBoundary = new Boundary
            {
                Name = $"Boundary {DateTime.Now:HHmmss}",
                IsClosed = true
            };
            
            foreach (var pt in _points)
            {
                _currentBoundary.AddVertex(pt);
            }
            
            _geometryModel.AddEntity(_currentBoundary);
        }
        else if (_currentBoundary != null)
        {
            // Add vertex to existing boundary
            _currentBoundary.AddVertex(point);
        }
        
        State = ModeState.Active;
        
        // Trigger state change event to update status prompt
        OnStateChanged(State, State);
    }
    
    private void UpdateCurrentBoundary()
    {
        if (_currentBoundary == null)
            return;
        
        // Remove all vertices and re-add from points list
        while (_currentBoundary.Vertices.Count > 0)
        {
            _currentBoundary.RemoveVertex(_currentBoundary.Vertices[0]);
        }
        
        foreach (var point in _points)
        {
            _currentBoundary.AddVertex(point);
        }
        
        // Remove boundary if less than 2 points remain
        if (_points.Count < 2)
        {
            _geometryModel.RemoveEntity(_currentBoundary);
            _currentBoundary = null;
        }
    }
    
    private void FinishBoundary()
    {
        if (_currentBoundary == null || _points.Count < 3)
            return;
        
        // Create command to add the boundary (for undo/redo)
        var command = new AddPolylineCommand(_geometryModel, _currentBoundary);
        // Note: Boundary already added to model during creation, so this is just for undo/redo tracking
        // In a more sophisticated implementation, we'd handle this better
        
        State = ModeState.Completed;
        _points.Clear();
        _currentBoundary = null;
        
        // Return to idle mode
        _modeManager.ReturnToIdle();
        
        CompleteMod();
    }
    
    private void CancelBoundary()
    {
        // Remove the current boundary if it exists
        if (_currentBoundary != null)
        {
            _geometryModel.RemoveEntity(_currentBoundary);
            _currentBoundary = null;
        }
        
        State = ModeState.Cancelled;
        _points.Clear();
        
        // Return to idle mode
        _modeManager.ReturnToIdle();
    }
    
    public override void Render(IRenderContext context)
    {
        // Draw rubber-band line from last point to current mouse position
        if (_points.Count > 0)
        {
            var lastPoint = _points[_points.Count - 1];
            
            // Draw dashed gray line from last point to mouse
            context.DrawLine(lastPoint, _currentMousePosition, 100, 100, 100, 1, dashed: true);
            
            // Draw all intermediate placed segments as solid lines
            for (int i = 0; i < _points.Count - 1; i++)
            {
                context.DrawLine(_points[i], _points[i + 1], 150, 150, 150, 2, dashed: false);
            }
            
            // Draw closing line preview if we have 3+ points (dashed green)
            if (_points.Count >= 3)
            {
                context.DrawLine(_currentMousePosition, _points[0], 100, 200, 100, 1, dashed: true);
            }
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
        
        if (_points.Count >= 3)
        {
            items.Add(new BoundaryContextMenuItem
            {
                Text = "Finish Boundary",
                Action = () => FinishBoundary()
            });
        }
        
        if (_points.Count > 0)
        {
            items.Add(new BoundaryContextMenuItem
            {
                Text = "Remove Last Point",
                Action = () => RemoveLastPoint()
            });
        }
        
        items.Add(new BoundaryContextMenuItem
        {
            Text = "Cancel",
            Action = () => CancelBoundary()
        });
        
        return items;
    }
    
    private void RemoveLastPoint()
    {
        if (_points.Count > 0)
        {
            _points.RemoveAt(_points.Count - 1);
            UpdateCurrentBoundary();
        }
    }
}

/// <summary>
/// Context menu item for AddBoundaryMode
/// </summary>
internal class BoundaryContextMenuItem : IContextMenuItem
{
    public string Text { get; set; } = string.Empty;
    public Action? Action { get; set; }
    public System.Windows.Input.ICommand? Command { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsSeparator { get; set; }
    public bool IsChecked { get; set; }
    public IEnumerable<IContextMenuItem>? SubItems { get; set; }
}

using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for moving vertices of polylines and boundaries
/// </summary>
public class MoveVertexMode : InteractionModeBase
{
    private readonly IModeManager _modeManager;
    private readonly ICommandManager _commandManager;
    private readonly ISelectionService _selectionService;
    private readonly ISnapService _snapService;
    private readonly IGeometryModel _geometryModel;
    
    private Polyline? _targetPolyline;
    private Vertex? _targetVertex;
    private Point2D _originalLocation;
    private bool _isDragging;
    private Camera.Camera2D? _camera;
    
    public MoveVertexMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        ISelectionService selectionService,
        ISnapService snapService,
        IGeometryModel geometryModel)
    {
        _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
        _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
        _geometryModel = geometryModel ?? throw new ArgumentNullException(nameof(geometryModel));
    }
    
    public override string Name => "Move Vertex";
    public override Cursor Cursor => _isDragging ? Interaction.Cursor.Hand : Interaction.Cursor.Cross;
    
    public override string StatusPrompt
    {
        get
        {
            if (_targetVertex == null)
                return "Click near a vertex to select it";
            else if (_isDragging)
                return "Drag to move vertex, release to finish";
            else
                return "Click and drag to move vertex";
        }
    }
    
    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _camera = context.Camera;
        _targetPolyline = null;
        _targetVertex = null;
        _isDragging = false;
        State = ModeState.WaitingForInput;
    }
    
    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button == MouseButton.Left)
        {
            // Find vertex near click point with camera-aware tolerance
            double tolerance = _camera != null ? 8.0 * _camera.Scale : 0.5; // 8 pixels
            
            foreach (var entity in _geometryModel.Entities)
            {
                if (entity is Polyline polyline)
                {
                    foreach (var vertex in polyline.Vertices)
                    {
                        if (worldPoint.DistanceTo(vertex.Location) <= tolerance)
                        {
                            _targetPolyline = polyline;
                            _targetVertex = vertex;
                            _originalLocation = vertex.Location;
                            _isDragging = true;
                            State = ModeState.Active;
                            return;
                        }
                    }
                }
                else if (entity is Boundary boundary)
                {
                    foreach (var vertex in boundary.Vertices)
                    {
                        if (worldPoint.DistanceTo(vertex.Location) <= tolerance)
                        {
                            _targetPolyline = null;
                            _targetVertex = vertex;
                            _originalLocation = vertex.Location;
                            _isDragging = true;
                            State = ModeState.Active;
                            return;
                        }
                    }
                }
            }
        }
        else if (button == MouseButton.Right)
        {
            // Cancel and return to idle
            CancelMove();
        }
    }
    
    public override void OnMouseMove(Point2D worldPoint, ModifierKeys modifiers)
    {
        if (_isDragging && _targetVertex != null)
        {
            // Apply snapping (only if camera is available)
            Point2D newLocation = worldPoint;
            if (_camera != null)
            {
                var snapResult = _snapService.Snap(worldPoint, _geometryModel.Entities, _camera);
                newLocation = snapResult != null ? snapResult.SnappedPoint : worldPoint;
            }
            
            // Update vertex location
            _targetVertex.Location = newLocation;
        }
    }
    
    public override void OnMouseUp(Point2D worldPoint, MouseButton button)
    {
        if (button == MouseButton.Left && _isDragging && _targetVertex != null)
        {
            // Apply final snapping (only if camera is available)
            Point2D finalLocation = worldPoint;
            if (_camera != null)
            {
                var snapResult = _snapService.Snap(worldPoint, _geometryModel.Entities, _camera);
                finalLocation = snapResult != null ? snapResult.SnappedPoint : worldPoint;
            }
            
            _targetVertex.Location = finalLocation;
            
            // Create undo command (vertex already has new location, command will restore old on undo)
            // Note: We need to set it back to old, then execute command to set to new
            _targetVertex.Location = _originalLocation;
            var command = new MoveVertexCommand(_targetVertex, finalLocation);
            _commandManager.Execute(command);
            
            State = ModeState.Completed;
            _isDragging = false;
            
            // Return to idle after move
            _modeManager.ReturnToIdle();
            
            CompleteMod();
        }
    }
    
    public override void OnKeyDown(Key key, ModifierKeys modifiers)
    {
        switch (key)
        {
            case Key.Escape:
                CancelMove();
                break;
        }
    }
    
    private void CancelMove()
    {
        // Restore original location if dragging
        if (_isDragging && _targetVertex != null)
        {
            _targetVertex.Location = _originalLocation;
        }
        
        State = ModeState.Cancelled;
        _targetPolyline = null;
        _targetVertex = null;
        _isDragging = false;
        
        // Return to idle mode
        _modeManager.ReturnToIdle();
    }
    
    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        var items = new List<IContextMenuItem>();
        
        items.Add(new SelectModeContextMenuItem
        {
            Text = "Cancel Move",
            Action = () => CancelMove()
        });
        
        items.Add(new SelectModeContextMenuItem
        {
            Text = "Exit Move Vertex Mode",
            Action = () => _modeManager.ReturnToIdle()
        });
        
        return items;
    }
}

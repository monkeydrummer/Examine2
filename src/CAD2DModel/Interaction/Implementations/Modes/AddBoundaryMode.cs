using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;
using System.Linq;

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
    private bool _makeIntersectable = false; // User preference for new boundaries
    
    // Arc/Circle drawing state
    private DrawingSubMode _drawingMode = DrawingSubMode.Line;
    private readonly ArcDrawingParameters _arcParams = new();
    private readonly CircleDrawingParameters _circleParams = new();
    private readonly List<Point2D> _arcPoints = new();
    private readonly List<Point2D> _circlePoints = new();
    
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
            return _drawingMode switch
            {
                DrawingSubMode.Arc => GetArcPrompt(),
                DrawingSubMode.Circle => GetCirclePrompt(),
                _ => GetLinePrompt()
            };
        }
    }
    
    private string GetLinePrompt()
    {
        if (_points.Count == 0)
            return "Click to place first vertex, right-click for arc/circle options";
        else if (_points.Count == 1)
            return "Click to place second vertex";
        else if (_points.Count == 2)
            return "Click to place third vertex (minimum for boundary), right-click for options";
        else
            return $"Click to add vertex ({_points.Count} vertices), Enter to finish, right-click for options";
    }
    
    private string GetArcPrompt()
    {
        return _arcParams.DrawMode switch
        {
            ArcDrawMode.ThreePoint => _arcPoints.Count == 0 ? "Click mid-point of arc" : "Click end point of arc",
            ArcDrawMode.StartEndRadius => "Click end point of arc",
            ArcDrawMode.StartEndBulge => "Click end point of arc (adjust bulge in menu)",
            _ => "Drawing arc..."
        };
    }
    
    private string GetCirclePrompt()
    {
        return _circleParams.DrawMode switch
        {
            CircleDrawMode.CenterRadius => _circlePoints.Count == 0 ? "Click center of circle" : "Click to set radius",
            CircleDrawMode.TwoPointDiameter => _circlePoints.Count == 0 ? "Click first point on diameter" : "Click second point on diameter",
            CircleDrawMode.ThreePoint => _circlePoints.Count switch
            {
                0 => "Click first point on circle",
                1 => "Click second point on circle",
                _ => "Click third point on circle"
            },
            _ => "Drawing circle..."
        };
    }
    
    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _points.Clear();
        _currentBoundary = null;
        _camera = context.Camera;
        _drawingMode = DrawingSubMode.Line;
        _arcPoints.Clear();
        _circlePoints.Clear();
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
            
            // Apply ortho snap if enabled and we have at least one point
            if ((_snapService.ActiveSnapModes & SnapMode.Ortho) != 0 && _points.Count > 0)
            {
                var orthoResult = _snapService.SnapToOrtho(_currentMousePosition, _points[_points.Count - 1]);
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
                point = snapResult != null ? snapResult.SnappedPoint : worldPoint;
                
                // Apply ortho snap if enabled and we have at least one point
                if ((_snapService.ActiveSnapModes & SnapMode.Ortho) != 0 && _points.Count > 0)
                {
                    var orthoResult = _snapService.SnapToOrtho(point, _points[_points.Count - 1]);
                    if (orthoResult.IsSnapped)
                    {
                        point = orthoResult.SnappedPoint;
                    }
                }
            }
            
            switch (_drawingMode)
            {
                case DrawingSubMode.Line:
                    AddPoint(point);
                    break;
                    
                case DrawingSubMode.Arc:
                    HandleArcPoint(point);
                    break;
                    
                case DrawingSubMode.Circle:
                    HandleCirclePoint(point);
                    break;
            }
        }
        // Right-click handled by context menu
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
                IsClosed = true,
                Intersectable = _makeIntersectable
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
        
        // Apply geometry rules now that the boundary is complete
        _geometryModel.ApplyRulesToEntity(_currentBoundary);
        
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
    
    private void HandleArcPoint(Point2D point)
    {
        _arcPoints.Add(point);
        
        switch (_arcParams.DrawMode)
        {
            case ArcDrawMode.ThreePoint:
                if (_arcPoints.Count >= 2)
                {
                    var start = _points[_points.Count - 1];
                    var mid = _arcPoints[0];
                    var end = _arcPoints[1];
                    
                    var arc = ArcCalculator.FromThreePoints(start, mid, end);
                    if (arc != null)
                    {
                        var arcPoints = arc.Discretize(_arcParams.Segments);
                        for (int i = 1; i < arcPoints.Count; i++)
                        {
                            AddPoint(arcPoints[i]);
                        }
                    }
                    
                    _arcPoints.Clear();
                    _drawingMode = DrawingSubMode.Line;
                }
                break;
                
            case ArcDrawMode.StartEndRadius:
                if (_arcPoints.Count >= 1)
                {
                    var start = _points[_points.Count - 1];
                    var end = _arcPoints[0];
                    
                    var arc = ArcCalculator.FromStartEndRadius(start, end, _arcParams.Radius, false);
                    if (arc != null)
                    {
                        var arcPoints = arc.Discretize(_arcParams.Segments);
                        for (int i = 1; i < arcPoints.Count; i++)
                        {
                            AddPoint(arcPoints[i]);
                        }
                    }
                    
                    _arcPoints.Clear();
                    _drawingMode = DrawingSubMode.Line;
                }
                break;
                
            case ArcDrawMode.StartEndBulge:
                if (_arcPoints.Count >= 1)
                {
                    var start = _points[_points.Count - 1];
                    var end = _arcPoints[0];
                    
                    var arc = ArcCalculator.FromStartEndBulge(start, end, _arcParams.Bulge);
                    if (arc != null)
                    {
                        var arcPoints = arc.Discretize(_arcParams.Segments);
                        for (int i = 1; i < arcPoints.Count; i++)
                        {
                            AddPoint(arcPoints[i]);
                        }
                    }
                    
                    _arcPoints.Clear();
                    _drawingMode = DrawingSubMode.Line;
                }
                break;
        }
    }
    
    private void HandleCirclePoint(Point2D point)
    {
        _circlePoints.Add(point);
        
        List<Point2D>? circlePoints = null;
        
        switch (_circleParams.DrawMode)
        {
            case CircleDrawMode.CenterRadius:
                if (_circlePoints.Count >= 2)
                {
                    var center = _circlePoints[0];
                    var radiusPoint = _circlePoints[1];
                    double radius = center.DistanceTo(radiusPoint);
                    circlePoints = ArcCalculator.CreateCircle(center, radius, _circleParams.Segments);
                }
                break;
                
            case CircleDrawMode.TwoPointDiameter:
                if (_circlePoints.Count >= 2)
                {
                    circlePoints = ArcCalculator.CreateCircleFromDiameter(_circlePoints[0], _circlePoints[1], _circleParams.Segments);
                }
                break;
                
            case CircleDrawMode.ThreePoint:
                if (_circlePoints.Count >= 3)
                {
                    circlePoints = ArcCalculator.CreateCircleFromThreePoints(_circlePoints[0], _circlePoints[1], _circlePoints[2], _circleParams.Segments);
                }
                break;
        }
        
        if (circlePoints != null)
        {
            // Clear all previous points and add circle
            _points.Clear();
            foreach (var pt in circlePoints)
            {
                _points.Add(pt);
            }
            UpdateCurrentBoundary();
            
            _circlePoints.Clear();
            _drawingMode = DrawingSubMode.Line;
        }
    }
    
    public override void Render(IRenderContext context)
    {
        if (_points.Count == 0)
            return;
        
        var lastPoint = _points[_points.Count - 1];
        
        switch (_drawingMode)
        {
            case DrawingSubMode.Arc:
                RenderArcPreview(context, lastPoint);
                break;
                
            case DrawingSubMode.Circle:
                RenderCirclePreview(context);
                break;
                
            default: // Line mode
                // Draw rubber-band line from last point to mouse
                context.DrawLine(lastPoint, _currentMousePosition, 100, 100, 100, 1, dashed: true);
                
                // Draw closing line preview if we have 3+ points (dashed green)
                if (_points.Count >= 3)
                {
                    context.DrawLine(_currentMousePosition, _points[0], 100, 200, 100, 1, dashed: true);
                }
                break;
        }
        
        // Draw all placed segments
        for (int i = 0; i < _points.Count - 1; i++)
        {
            context.DrawLine(_points[i], _points[i + 1], 150, 150, 150, 2, dashed: false);
        }
        
        // Draw snap indicator
        if (_currentSnapResult != null && _currentSnapResult.IsSnapped)
        {
            context.DrawSnapIndicator(_currentSnapResult.SnappedPoint, _currentSnapResult.SnapType);
        }
    }
    
    private void RenderArcPreview(IRenderContext context, Point2D start)
    {
        Arc? arc = null;
        
        switch (_arcParams.DrawMode)
        {
            case ArcDrawMode.ThreePoint:
                if (_arcPoints.Count == 0)
                {
                    context.DrawLine(start, _currentMousePosition, 100, 150, 255, 1, dashed: true);
                }
                else if (_arcPoints.Count == 1)
                {
                    arc = ArcCalculator.FromThreePoints(start, _arcPoints[0], _currentMousePosition);
                }
                break;
                
            case ArcDrawMode.StartEndRadius:
                arc = ArcCalculator.FromStartEndRadius(start, _currentMousePosition, _arcParams.Radius, false);
                break;
                
            case ArcDrawMode.StartEndBulge:
                arc = ArcCalculator.FromStartEndBulge(start, _currentMousePosition, _arcParams.Bulge);
                break;
        }
        
        if (arc != null)
        {
            var points = arc.Discretize(_arcParams.Segments);
            for (int i = 0; i < points.Count - 1; i++)
            {
                context.DrawLine(points[i], points[i + 1], 100, 150, 255, 1, dashed: true);
            }
        }
    }
    
    private void RenderCirclePreview(IRenderContext context)
    {
        List<Point2D>? preview = null;
        
        switch (_circleParams.DrawMode)
        {
            case CircleDrawMode.CenterRadius:
                if (_circlePoints.Count == 1)
                {
                    var center = _circlePoints[0];
                    double radius = center.DistanceTo(_currentMousePosition);
                    preview = ArcCalculator.CreateCircle(center, radius, _circleParams.Segments);
                }
                break;
                
            case CircleDrawMode.TwoPointDiameter:
                if (_circlePoints.Count == 1)
                {
                    preview = ArcCalculator.CreateCircleFromDiameter(_circlePoints[0], _currentMousePosition, _circleParams.Segments);
                }
                break;
                
            case CircleDrawMode.ThreePoint:
                if (_circlePoints.Count == 2)
                {
                    preview = ArcCalculator.CreateCircleFromThreePoints(_circlePoints[0], _circlePoints[1], _currentMousePosition, _circleParams.Segments);
                }
                break;
        }
        
        if (preview != null)
        {
            for (int i = 0; i < preview.Count; i++)
            {
                int next = (i + 1) % preview.Count;
                context.DrawLine(preview[i], preview[next], 255, 150, 100, 1, dashed: true);
            }
        }
    }
    
    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        var items = new List<IContextMenuItem>();
        
        // Intersectable toggle (always available)
        items.Add(new BoundaryContextMenuItem
        {
            Text = "Make Intersectable",
            IsChecked = _makeIntersectable,
            Action = () => {
                _makeIntersectable = !_makeIntersectable;
                if (_currentBoundary != null)
                {
                    _currentBoundary.Intersectable = _makeIntersectable;
                }
            }
        });
        items.Add(new BoundaryContextMenuItem { IsSeparator = true });
        
        // Drawing mode options (only in line mode with at least one point)
        if (_points.Count > 0 && _drawingMode == DrawingSubMode.Line)
        {
            items.Add(new BoundaryContextMenuItem { Text = "Arc Options", IsSeparator = false });
            items.Add(new BoundaryContextMenuItem
            {
                Text = "  3-Point Arc",
                Action = () => { _drawingMode = DrawingSubMode.Arc; _arcParams.DrawMode = ArcDrawMode.ThreePoint; _arcPoints.Clear(); }
            });
            items.Add(new BoundaryContextMenuItem
            {
                Text = "  Start-End-Radius Arc",
                Action = () => { _drawingMode = DrawingSubMode.Arc; _arcParams.DrawMode = ArcDrawMode.StartEndRadius; _arcPoints.Clear(); }
            });
            items.Add(new BoundaryContextMenuItem
            {
                Text = "  Start-End-Bulge Arc",
                Action = () => { _drawingMode = DrawingSubMode.Arc; _arcParams.DrawMode = ArcDrawMode.StartEndBulge; _arcPoints.Clear(); }
            });
            items.Add(new BoundaryContextMenuItem { IsSeparator = true });
            
            items.Add(new BoundaryContextMenuItem { Text = "Circle Options", IsSeparator = false });
            items.Add(new BoundaryContextMenuItem
            {
                Text = "  Center-Radius Circle",
                Action = () => { _drawingMode = DrawingSubMode.Circle; _circleParams.DrawMode = CircleDrawMode.CenterRadius; _circlePoints.Clear(); }
            });
            items.Add(new BoundaryContextMenuItem
            {
                Text = "  2-Point Diameter Circle",
                Action = () => { _drawingMode = DrawingSubMode.Circle; _circleParams.DrawMode = CircleDrawMode.TwoPointDiameter; _circlePoints.Clear(); }
            });
            items.Add(new BoundaryContextMenuItem
            {
                Text = "  3-Point Circle",
                Action = () => { _drawingMode = DrawingSubMode.Circle; _circleParams.DrawMode = CircleDrawMode.ThreePoint; _circlePoints.Clear(); }
            });
            items.Add(new BoundaryContextMenuItem { IsSeparator = true });
        }
        
        // Cancel arc/circle
        if (_drawingMode != DrawingSubMode.Line)
        {
            items.Add(new BoundaryContextMenuItem
            {
                Text = "Cancel Arc/Circle",
                Action = () => { _drawingMode = DrawingSubMode.Line; _arcPoints.Clear(); _circlePoints.Clear(); }
            });
            items.Add(new BoundaryContextMenuItem { IsSeparator = true });
        }
        
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

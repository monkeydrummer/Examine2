using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;
using System.Windows.Input;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Base class for modes that draw polylines (sequences of line/arc segments)
/// Provides common functionality for point capture, snapping, arc/circle drawing, and preview rendering
/// </summary>
public abstract class PolylineDrawingModeBase : InteractionModeBase
{
    protected readonly IModeManager _modeManager;
    protected readonly ICommandManager _commandManager;
    protected readonly IGeometryModel _geometryModel;
    protected readonly ISnapService _snapService;
    
    protected readonly List<Point2D> _points = new();
    protected Point2D _currentMousePosition;
    protected SnapResult? _currentSnapResult;
    protected Camera.Camera2D? _camera;
    
    // Arc/Circle drawing state
    protected DrawingSubMode _drawingMode = DrawingSubMode.Line;
    protected readonly ArcDrawingParameters _arcParams = new();
    protected readonly CircleDrawingParameters _circleParams = new();
    protected readonly List<Point2D> _arcPoints = new();
    protected readonly List<Point2D> _circlePoints = new();
    
    // Abstract properties to be implemented by derived classes
    protected abstract int MinimumPointCount { get; }
    protected abstract bool IsClosedShape { get; }
    protected abstract string EntityTypeName { get; }
    
    // Abstract methods for entity-specific operations
    protected abstract void CreateTemporaryEntity(List<Point2D> points);
    protected abstract void UpdateTemporaryEntity(List<Point2D> points);
    protected abstract void RemoveTemporaryEntity();
    protected abstract void CreateAndCommitEntity(List<Point2D> points);
    
    public PolylineDrawingModeBase(
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
    
    protected virtual string GetLinePrompt()
    {
        var suffix = GetStatusPromptSuffix();
        
        if (_points.Count == 0)
            return $"Click to place first vertex {suffix}";
        else if (_points.Count < MinimumPointCount)
            return $"Click to place vertex ({_points.Count} vertices) {suffix}";
        else
            return $"Click to add vertex ({_points.Count} vertices) {suffix}";
    }
    
    protected virtual string GetArcPrompt()
    {
        var suffix = GetStatusPromptSuffix();
        
        return _arcParams.DrawMode switch
        {
            ArcDrawMode.ThreePoint => _arcPoints.Count == 0 
                ? $"Click mid-point of arc {suffix}" 
                : $"Click end point of arc {suffix}",
            ArcDrawMode.StartEndRadius => $"Click end point of arc {suffix}",
            ArcDrawMode.StartEndBulge => $"Click end point of arc (adjust bulge in menu) {suffix}",
            _ => $"Drawing arc... {suffix}"
        };
    }
    
    protected virtual string GetCirclePrompt()
    {
        var suffix = GetStatusPromptSuffix();
        
        return _circleParams.DrawMode switch
        {
            CircleDrawMode.CenterRadius => _circlePoints.Count == 0 
                ? $"Click center of circle {suffix}" 
                : $"Click to set radius {suffix}",
            CircleDrawMode.TwoPointDiameter => _circlePoints.Count == 0 
                ? $"Click first point on diameter {suffix}" 
                : $"Click second point on diameter {suffix}",
            CircleDrawMode.ThreePoint => _circlePoints.Count switch
            {
                0 => $"Click first point on circle {suffix}",
                1 => $"Click second point on circle {suffix}",
                _ => $"Click third point on circle {suffix}"
            },
            _ => $"Drawing circle... {suffix}"
        };
    }
    
    protected virtual string GetStatusPromptSuffix()
    {
        var shortcuts = new List<string>();
        
        // Context-specific shortcuts based on current state
        if (_drawingMode == DrawingSubMode.Line)
        {
            if (_points.Count >= MinimumPointCount)
            {
                shortcuts.Add("Enter = finish");
            }
            if (_points.Count > 0)
            {
                shortcuts.Add("U = undo");
            }
            if (_points.Count > 0)
            {
                shortcuts.Add("A = arc");
                shortcuts.Add("C = circle");
            }
        }
        else if (_drawingMode == DrawingSubMode.Arc)
        {
            shortcuts.Add("L = line");
            if (_points.Count > 0)
            {
                shortcuts.Add("U = undo");
            }
        }
        else if (_drawingMode == DrawingSubMode.Circle)
        {
            shortcuts.Add("L = line");
            if (_points.Count > 0)
            {
                shortcuts.Add("U = undo");
            }
        }
        
        if (shortcuts.Count > 0)
        {
            return "[" + string.Join(", ", shortcuts) + "]";
        }
        
        return string.Empty;
    }
    
    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _points.Clear();
        _camera = context.Camera;
        _drawingMode = DrawingSubMode.Line;
        _arcPoints.Clear();
        _circlePoints.Clear();
        State = ModeState.WaitingForInput;
    }
    
    public override void OnExit()
    {
        // Clean up temporary entity if not completed
        if (State != ModeState.Completed)
        {
            RemoveTemporaryEntity();
        }
        
        _points.Clear();
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
                if (_points.Count >= MinimumPointCount)
                {
                    FinishDrawing();
                }
                break;
                
            case Key.Escape:
                CancelDrawing();
                break;
                
            case Key.Backspace:
            case Key.U:
                // Remove last point
                RemoveLastPoint();
                break;
                
            case Key.A:
                // Enter arc mode (only if in line mode and have at least one point)
                if (_drawingMode == DrawingSubMode.Line && _points.Count > 0)
                {
                    _drawingMode = DrawingSubMode.Arc;
                    _arcParams.DrawMode = ArcDrawMode.ThreePoint;
                    _arcPoints.Clear();
                    OnStateChanged(State, State); // Trigger prompt update
                }
                break;
                
            case Key.C:
                // Enter circle mode (only if in line mode)
                if (_drawingMode == DrawingSubMode.Line && _points.Count >= 0)
                {
                    _drawingMode = DrawingSubMode.Circle;
                    _circleParams.DrawMode = CircleDrawMode.CenterRadius;
                    _circlePoints.Clear();
                    OnStateChanged(State, State); // Trigger prompt update
                }
                break;
                
            case Key.L:
                // Return to line mode
                if (_drawingMode != DrawingSubMode.Line)
                {
                    _drawingMode = DrawingSubMode.Line;
                    _arcPoints.Clear();
                    _circlePoints.Clear();
                    OnStateChanged(State, State); // Trigger prompt update
                }
                break;
        }
    }
    
    protected virtual void AddPoint(Point2D point)
    {
        _points.Add(point);
        
        if (_points.Count >= 2)
        {
            UpdateTemporaryEntity(_points);
        }
        
        State = ModeState.Active;
        
        // Trigger state change event to update status prompt
        OnStateChanged(State, State);
    }
    
    protected virtual void RemoveLastPoint()
    {
        if (_points.Count > 0)
        {
            _points.RemoveAt(_points.Count - 1);
            
            if (_points.Count >= 2)
            {
                UpdateTemporaryEntity(_points);
            }
            else if (_points.Count < 2)
            {
                RemoveTemporaryEntity();
            }
            
            OnStateChanged(State, State); // Trigger prompt update
        }
    }
    
    protected virtual void FinishDrawing()
    {
        if (_points.Count < MinimumPointCount)
            return;
        
        // Create and commit the final entity
        // (derived classes handle removing temporary entity and re-adding through command manager)
        CreateAndCommitEntity(_points);
        
        State = ModeState.Completed;
        _points.Clear();
        
        // Return to idle mode
        _modeManager.ReturnToIdle();
        
        CompleteMod();
    }
    
    protected virtual void CancelDrawing()
    {
        // Remove the temporary entity if it exists
        RemoveTemporaryEntity();
        
        State = ModeState.Cancelled;
        _points.Clear();
        
        // Return to idle mode
        _modeManager.ReturnToIdle();
    }
    
    protected virtual void HandleArcPoint(Point2D point)
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
    
    protected virtual void HandleCirclePoint(Point2D point)
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
            UpdateTemporaryEntity(_points);
            
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
                
                // Draw closing line preview if we have enough points and it's a closed shape
                if (IsClosedShape && _points.Count >= MinimumPointCount)
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
    
    protected virtual void RenderArcPreview(IRenderContext context, Point2D start)
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
    
    protected virtual void RenderCirclePreview(IRenderContext context)
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
        
        // Add derived class-specific items first
        AddDerivedContextMenuItems(items, worldPoint);
        
        // Drawing mode options (only in line mode with at least one point)
        if (_points.Count > 0 && _drawingMode == DrawingSubMode.Line)
        {
            items.Add(new DrawingModeContextMenuItem { Text = "Arc Options", IsSeparator = false });
            items.Add(new DrawingModeContextMenuItem
            {
                Text = "  3-Point Arc",
                Action = () => { _drawingMode = DrawingSubMode.Arc; _arcParams.DrawMode = ArcDrawMode.ThreePoint; _arcPoints.Clear(); }
            });
            items.Add(new DrawingModeContextMenuItem
            {
                Text = "  Start-End-Radius Arc",
                Action = () => { _drawingMode = DrawingSubMode.Arc; _arcParams.DrawMode = ArcDrawMode.StartEndRadius; _arcPoints.Clear(); }
            });
            items.Add(new DrawingModeContextMenuItem
            {
                Text = "  Start-End-Bulge Arc",
                Action = () => { _drawingMode = DrawingSubMode.Arc; _arcParams.DrawMode = ArcDrawMode.StartEndBulge; _arcPoints.Clear(); }
            });
            items.Add(new DrawingModeContextMenuItem { IsSeparator = true });
            
            items.Add(new DrawingModeContextMenuItem { Text = "Circle Options", IsSeparator = false });
            items.Add(new DrawingModeContextMenuItem
            {
                Text = "  Center-Radius Circle",
                Action = () => { _drawingMode = DrawingSubMode.Circle; _circleParams.DrawMode = CircleDrawMode.CenterRadius; _circlePoints.Clear(); }
            });
            items.Add(new DrawingModeContextMenuItem
            {
                Text = "  2-Point Diameter Circle",
                Action = () => { _drawingMode = DrawingSubMode.Circle; _circleParams.DrawMode = CircleDrawMode.TwoPointDiameter; _circlePoints.Clear(); }
            });
            items.Add(new DrawingModeContextMenuItem
            {
                Text = "  3-Point Circle",
                Action = () => { _drawingMode = DrawingSubMode.Circle; _circleParams.DrawMode = CircleDrawMode.ThreePoint; _circlePoints.Clear(); }
            });
            items.Add(new DrawingModeContextMenuItem { IsSeparator = true });
        }
        
        // Cancel arc/circle
        if (_drawingMode != DrawingSubMode.Line)
        {
            items.Add(new DrawingModeContextMenuItem
            {
                Text = "Cancel Arc/Circle",
                Action = () => { _drawingMode = DrawingSubMode.Line; _arcPoints.Clear(); _circlePoints.Clear(); }
            });
            items.Add(new DrawingModeContextMenuItem { IsSeparator = true });
        }
        
        if (_points.Count >= MinimumPointCount)
        {
            items.Add(new DrawingModeContextMenuItem
            {
                Text = $"Finish {EntityTypeName}",
                Action = () => FinishDrawing()
            });
        }
        
        if (_points.Count > 0)
        {
            items.Add(new DrawingModeContextMenuItem
            {
                Text = "Remove Last Point",
                Action = () => RemoveLastPoint()
            });
        }
        
        items.Add(new DrawingModeContextMenuItem
        {
            Text = "Cancel",
            Action = () => CancelDrawing()
        });
        
        return items;
    }
    
    /// <summary>
    /// Override this to add derived class-specific context menu items
    /// </summary>
    protected virtual void AddDerivedContextMenuItems(List<IContextMenuItem> items, Point2D worldPoint)
    {
        // Override in derived classes to add specific items
    }
}

/// <summary>
/// Context menu item for drawing modes
/// </summary>
internal class DrawingModeContextMenuItem : IContextMenuItem
{
    public string Text { get; set; } = string.Empty;
    public Action? Action { get; set; }
    public System.Windows.Input.ICommand? Command { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsSeparator { get; set; }
    public bool IsChecked { get; set; }
    public IEnumerable<IContextMenuItem>? SubItems { get; set; }
}

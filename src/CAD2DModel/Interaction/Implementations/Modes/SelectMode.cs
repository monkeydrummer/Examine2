using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for selecting entities by clicking or box selection
/// </summary>
public class SelectMode : InteractionModeBase
{
    private readonly IModeManager _modeManager;
    private readonly ICommandManager _commandManager;
    private readonly ISelectionService _selectionService;
    private readonly IGeometryModel _geometryModel;
    
    private Point2D? _boxStart;
    private Point2D _currentMousePosition;
    private Camera.Camera2D? _camera;
    
    /// <summary>
    /// Filter for what types of entities can be selected
    /// </summary>
    public SelectionFilter Filter { get; set; } = SelectionFilter.All;
    
    public SelectMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        ISelectionService selectionService,
        IGeometryModel geometryModel)
    {
        _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
        _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        _geometryModel = geometryModel ?? throw new ArgumentNullException(nameof(geometryModel));
    }
    
    public override string Name => "Select";
    public override Cursor Cursor => Interaction.Cursor.PickBox;
    
    public override string StatusPrompt
    {
        get
        {
            if (_boxStart.HasValue)
                return "Release to complete box selection, Esc to cancel";
            else if (_selectionService.SelectedEntities.Count > 0)
                return $"{_selectionService.SelectedEntities.Count} selected - Click to select, drag for box selection, Ctrl+click to add/remove";
            else
                return "Click to select entity, drag for box selection, Ctrl+click to add to selection";
        }
    }
    
    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _boxStart = null;
        _camera = context.Camera;
        State = ModeState.WaitingForInput;
    }
    
    public override void OnExit()
    {
        _boxStart = null;
        base.OnExit();
    }
    
    public override void OnMouseMove(Point2D worldPoint, ModifierKeys modifiers)
    {
        _currentMousePosition = worldPoint;
        
        if (_boxStart.HasValue)
        {
            // Update selection box preview
            State = ModeState.Active;
        }
    }
    
    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button == MouseButton.Left)
        {
            // Start box selection or single click selection
            _boxStart = worldPoint;
            _currentMousePosition = worldPoint;
        }
        else if (button == MouseButton.Right)
        {
            // Right-click to finish or show context menu
            if (_selectionService.SelectedEntities.Count > 0)
            {
                // Context menu would go here
            }
        }
    }
    
    public override void OnMouseUp(Point2D worldPoint, MouseButton button)
    {
        if (button == MouseButton.Left && _boxStart.HasValue)
        {
            var startPoint = _boxStart.Value;
            var endPoint = worldPoint;
            
            // Check if this was a click (very small movement) or a drag
            double distance = startPoint.DistanceTo(endPoint);
            double clickTolerance = _camera != null ? 5.0 * _camera.Scale : 5.0; // 5 pixels
            
            // TODO: Get modifiers from keyboard state
            bool addToSelection = false; // For now, we'll handle this via Ctrl+click in OnMouseDown
            
            if (distance < clickTolerance)
            {
                // Single click selection
                PerformClickSelection(worldPoint, addToSelection);
            }
            else
            {
                // Box selection
                PerformBoxSelection(startPoint, endPoint, addToSelection);
            }
            
            _boxStart = null;
            State = ModeState.WaitingForInput;
            OnStateChanged(State, State); // Update status prompt
        }
    }
    
    public override void OnKeyDown(Key key, ModifierKeys modifiers)
    {
        switch (key)
        {
            case Key.Escape:
                if (_boxStart.HasValue)
                {
                    // Cancel box selection
                    _boxStart = null;
                    State = ModeState.WaitingForInput;
                }
                else if (_selectionService.SelectedEntities.Count > 0)
                {
                    // Clear selection
                    _selectionService.ClearSelection();
                }
                break;
                
            case Key.A:
                if ((modifiers & ModifierKeys.Control) != 0)
                {
                    // Select all
                    _selectionService.Select(_geometryModel.Entities);
                }
                break;
                
            case Key.Delete:
                // Delete selected entities
                if (_selectionService.SelectedEntities.Count > 0)
                {
                    DeleteSelectedEntities();
                }
                break;
        }
    }
    
    public override void Render(IRenderContext context)
    {
        // Draw selection box if active
        if (_boxStart.HasValue && State == ModeState.Active)
        {
            var start = _boxStart.Value;
            var end = _currentMousePosition;
            
            // Determine selection mode based on drag direction
            bool crossingMode = end.X < start.X; // Right-to-left = crossing mode
            byte r = crossingMode ? (byte)0 : (byte)0;
            byte g = crossingMode ? (byte)100 : (byte)100;
            byte b = crossingMode ? (byte)255 : (byte)0;
            
            // Draw box outline
            context.DrawLine(new Point2D(start.X, start.Y), new Point2D(end.X, start.Y), r, g, b, 1, dashed: true);
            context.DrawLine(new Point2D(end.X, start.Y), new Point2D(end.X, end.Y), r, g, b, 1, dashed: true);
            context.DrawLine(new Point2D(end.X, end.Y), new Point2D(start.X, end.Y), r, g, b, 1, dashed: true);
            context.DrawLine(new Point2D(start.X, end.Y), new Point2D(start.X, start.Y), r, g, b, 1, dashed: true);
        }
        
        // Draw selection highlights on selected entities
        foreach (var entity in _selectionService.SelectedEntities)
        {
            if (entity is Polyline polyline)
            {
                for (int i = 0; i < polyline.GetSegmentCount(); i++)
                {
                    var segment = polyline.GetSegment(i);
                    context.DrawLine(segment.Start, segment.End, 255, 165, 0, 3, dashed: false); // Orange highlight
                }
            }
            else if (entity is Boundary boundary)
            {
                for (int i = 0; i < boundary.GetSegmentCount(); i++)
                {
                    var segment = boundary.GetSegment(i);
                    context.DrawLine(segment.Start, segment.End, 255, 165, 0, 3, dashed: false); // Orange highlight
                }
            }
        }
    }
    
    private void PerformClickSelection(Point2D point, bool addToSelection)
    {
        if (_camera == null)
            return;
        
        // Convert pixel tolerance to world units
        double worldTolerance = 5.0 * _camera.Scale; // 5 pixels
        
        // Filter entities based on selection filter
        var filteredEntities = FilterEntities(_geometryModel.Entities);
        
        var hitEntity = _selectionService.HitTest(point, worldTolerance, filteredEntities);
        
        if (hitEntity != null)
        {
            if (addToSelection)
            {
                // Toggle selection
                _selectionService.ToggleSelection(hitEntity);
            }
            else
            {
                // Replace selection
                _selectionService.Select(hitEntity);
            }
        }
        else if (!addToSelection)
        {
            // Clear selection if clicking empty space without Ctrl
            _selectionService.ClearSelection();
        }
    }
    
    private void PerformBoxSelection(Point2D start, Point2D end, bool addToSelection)
    {
        // Create selection box
        double minX = Math.Min(start.X, end.X);
        double minY = Math.Min(start.Y, end.Y);
        double maxX = Math.Max(start.X, end.X);
        double maxY = Math.Max(start.Y, end.Y);
        
        var selectionBox = new Rect2D(minX, minY, maxX - minX, maxY - minY);
        
        // Determine selection mode: left-to-right = window (entirely inside), right-to-left = crossing
        bool crossingMode = end.X < start.X;
        
        // Filter entities based on selection filter
        var filteredEntities = FilterEntities(_geometryModel.Entities);
        
        var entitiesInBox = _selectionService.SelectInBox(selectionBox, filteredEntities, !crossingMode);
        
        if (addToSelection)
        {
            // Add to existing selection
            _selectionService.Select(entitiesInBox, addToSelection: true);
        }
        else
        {
            // Replace selection
            _selectionService.Select(entitiesInBox);
        }
    }
    
    private IEnumerable<IEntity> FilterEntities(IEnumerable<IEntity> entities)
    {
        if (Filter == SelectionFilter.All)
            return entities;
        
        var filtered = new List<IEntity>();
        
        foreach (var entity in entities)
        {
            bool include = false;
            
            if ((Filter & SelectionFilter.Polylines) != 0 && entity is Polyline)
                include = true;
            
            if ((Filter & SelectionFilter.Boundaries) != 0 && entity is Boundary)
                include = true;
            
            // Vertices filter would require a different approach - maybe showing vertex handles
            // For now, vertices are part of polylines/boundaries
            
            if (include)
                filtered.Add(entity);
        }
        
        return filtered;
    }
    
    private void DeleteSelectedEntities()
    {
        var entitiesToDelete = _selectionService.SelectedEntities.ToList();
        
        foreach (var entity in entitiesToDelete)
        {
            _geometryModel.RemoveEntity(entity);
        }
        
        _selectionService.ClearSelection();
    }
    
    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        // TODO: Implement proper context menu items when UI is ready
        return Enumerable.Empty<IContextMenuItem>();
    }
}


using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Selection;

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
    
    /// <summary>
    /// Size of vertex handles in pixels
    /// </summary>
    private const double VertexHandleSize = 8.0;
    
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
            
            var selectionCount = Filter == SelectionFilter.Vertices 
                ? _selectionService.SelectedVertices.Count 
                : _selectionService.SelectedEntities.Count;
            
            if (selectionCount > 0)
            {
                var itemType = Filter == SelectionFilter.Vertices ? "vertices" : "entities";
                return $"{selectionCount} {itemType} selected - Click to select, drag for box selection, Ctrl+click to add/remove, Del to delete";
            }
            else
            {
                var itemType = Filter == SelectionFilter.Vertices ? "vertex" : "entity";
                return $"Click to select {itemType}, drag for box selection, Ctrl+click to add to selection";
            }
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
                else if (_selectionService.SelectedEntities.Count > 0 || _selectionService.SelectedVertices.Count > 0)
                {
                    // Clear all selections
                    _selectionService.ClearAllSelections();
                }
                else
                {
                    // Return to idle mode
                    _modeManager.ReturnToIdle();
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
                // Delete selected entities or vertices
                if (Filter == SelectionFilter.Vertices && _selectionService.SelectedVertices.Count > 0)
                {
                    DeleteSelectedVertices();
                }
                else if (_selectionService.SelectedEntities.Count > 0)
                {
                    DeleteSelectedEntities();
                }
                break;
        }
    }
    
    public override void Render(IRenderContext context)
    {
        // Draw vertex handles if in vertex selection mode
        if (Filter == SelectionFilter.Vertices && _camera != null)
        {
            DrawVertexHandles(context);
        }
        
        // Draw selection box if active
        if (_boxStart.HasValue && State == ModeState.Active)
        {
            var start = _boxStart.Value;
            var end = _currentMousePosition;
            
            // Determine selection mode based on drag direction
            bool crossingMode = end.X < start.X; // Right-to-left = crossing mode
            // Crossing mode (right-to-left) = GREEN, Window mode (left-to-right) = BLUE
            byte r = (byte)0;
            byte g = crossingMode ? (byte)150 : (byte)100;
            byte b = crossingMode ? (byte)0 : (byte)255;
            
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
        
        // Draw selected vertices
        foreach (var vertex in _selectionService.SelectedVertices)
        {
            DrawVertexHandle(context, vertex.Location, true);
        }
    }
    
    private void PerformClickSelection(Point2D point, bool addToSelection)
    {
        if (_camera == null)
            return;
        
        // Convert pixel tolerance to world units
        double worldTolerance = 5.0 * _camera.Scale; // 5 pixels
        
        if (Filter == SelectionFilter.Vertices)
        {
            // Vertex selection
            var hitVertex = _selectionService.HitTestVertex(point, worldTolerance, _geometryModel.Entities);
            
            if (hitVertex != null)
            {
                if (addToSelection)
                {
                    _selectionService.ToggleVertexSelection(hitVertex);
                }
                else
                {
                    _selectionService.SelectVertex(hitVertex);
                }
            }
            else if (!addToSelection)
            {
                _selectionService.ClearVertexSelection();
            }
        }
        else
        {
            // Entity selection
            var filteredEntities = FilterEntities(_geometryModel.Entities);
            var hitEntity = _selectionService.HitTest(point, worldTolerance, filteredEntities);
            
            if (hitEntity != null)
            {
                if (addToSelection)
                {
                    _selectionService.ToggleSelection(hitEntity);
                }
                else
                {
                    _selectionService.Select(hitEntity);
                }
            }
            else if (!addToSelection)
            {
                _selectionService.ClearSelection();
            }
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
        
        if (Filter == SelectionFilter.Vertices)
        {
            // Vertex box selection
            var verticesInBox = _selectionService.SelectVerticesInBox(selectionBox, _geometryModel.Entities);
            
            if (addToSelection)
            {
                _selectionService.SelectVertices(verticesInBox, addToSelection: true);
            }
            else
            {
                _selectionService.SelectVertices(verticesInBox);
            }
        }
        else
        {
            // Entity box selection
            // Determine selection mode: left-to-right = window (entirely inside), right-to-left = crossing
            bool crossingMode = end.X < start.X;
            
            // Filter entities based on selection filter
            var filteredEntities = FilterEntities(_geometryModel.Entities);
            
            var entitiesInBox = _selectionService.SelectInBox(selectionBox, filteredEntities, !crossingMode);
            
            if (addToSelection)
            {
                _selectionService.Select(entitiesInBox, addToSelection: true);
            }
            else
            {
                _selectionService.Select(entitiesInBox);
            }
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
    
    private void DeleteSelectedVertices()
    {
        var verticesToDelete = _selectionService.SelectedVertices.ToList();
        
        // Group vertices by entity to process them efficiently
        var verticesByEntity = verticesToDelete
            .GroupBy(v => v.Entity)
            .ToList();
        
        foreach (var group in verticesByEntity)
        {
            var entity = group.Key;
            // Sort indices in descending order so we can remove from the end first
            var indices = group.Select(v => v.VertexIndex).OrderByDescending(i => i).ToList();
            
            if (entity is Polyline polyline)
            {
                foreach (var index in indices)
                {
                    if (polyline.Vertices.Count > 2) // Keep at least 2 vertices for a polyline
                    {
                        polyline.RemoveVertexAt(index);
                    }
                }
            }
            else if (entity is Boundary boundary)
            {
                foreach (var index in indices)
                {
                    if (boundary.Vertices.Count > 3) // Keep at least 3 vertices for a boundary
                    {
                        boundary.RemoveVertexAt(index);
                    }
                }
            }
        }
        
        _selectionService.ClearVertexSelection();
    }
    
    private void DrawVertexHandles(IRenderContext context)
    {
        if (_camera == null)
            return;
        
        // Draw vertex handles for all visible entities
        foreach (var entity in _geometryModel.Entities)
        {
            if (!entity.IsVisible)
                continue;
            
            if (entity is Polyline polyline)
            {
                foreach (var vertex in polyline.Vertices)
                {
                    DrawVertexHandle(context, vertex.Location, false);
                }
            }
            else if (entity is Boundary boundary)
            {
                foreach (var vertex in boundary.Vertices)
                {
                    DrawVertexHandle(context, vertex.Location, false);
                }
            }
        }
    }
    
    private void DrawVertexHandle(IRenderContext context, Point2D location, bool isSelected)
    {
        if (_camera == null)
            return;
        
        // Calculate handle size in world units
        double halfSize = (VertexHandleSize / 2.0) * _camera.Scale;
        
        // More vibrant colors for better visibility
        byte r = isSelected ? (byte)255 : (byte)0;
        byte g = isSelected ? (byte)140 : (byte)120;
        byte b = isSelected ? (byte)0 : (byte)255;
        float strokeWidth = isSelected ? 3.0f : 2.0f;
        
        // Draw a square handle with thicker lines
        var topLeft = new Point2D(location.X - halfSize, location.Y - halfSize);
        var topRight = new Point2D(location.X + halfSize, location.Y - halfSize);
        var bottomRight = new Point2D(location.X + halfSize, location.Y + halfSize);
        var bottomLeft = new Point2D(location.X - halfSize, location.Y + halfSize);
        
        context.DrawLine(topLeft, topRight, r, g, b, strokeWidth, dashed: false);
        context.DrawLine(topRight, bottomRight, r, g, b, strokeWidth, dashed: false);
        context.DrawLine(bottomRight, bottomLeft, r, g, b, strokeWidth, dashed: false);
        context.DrawLine(bottomLeft, topLeft, r, g, b, strokeWidth, dashed: false);
        
        // Draw diagonal lines to make it more visible (filled appearance)
        if (isSelected)
        {
            context.DrawLine(topLeft, bottomRight, r, g, b, 1, dashed: false);
            context.DrawLine(topRight, bottomLeft, r, g, b, 1, dashed: false);
        }
    }
    
    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        var items = new List<IContextMenuItem>();
        
        // Selection actions for entities
        if (_selectionService.SelectedEntities.Count > 0)
        {
            items.Add(new SelectModeContextMenuItem
            {
                Text = $"Delete ({_selectionService.SelectedEntities.Count} entities)",
                Action = DeleteSelectedEntities
            });
            
            items.Add(new SelectModeContextMenuItem
            {
                Text = "Clear Entity Selection",
                Action = () => _selectionService.ClearSelection()
            });
            
            items.Add(new SelectModeContextMenuItem { IsSeparator = true });
        }
        
        // Selection actions for vertices
        if (_selectionService.SelectedVertices.Count > 0)
        {
            items.Add(new SelectModeContextMenuItem
            {
                Text = $"Delete ({_selectionService.SelectedVertices.Count} vertices)",
                Action = DeleteSelectedVertices
            });
            
            items.Add(new SelectModeContextMenuItem
            {
                Text = "Clear Vertex Selection",
                Action = () => _selectionService.ClearVertexSelection()
            });
            
            items.Add(new SelectModeContextMenuItem { IsSeparator = true });
        }
        
        // Selection filter options
        items.Add(new SelectModeContextMenuItem
        {
            Text = "Filter: All Entities",
            Action = () => Filter = SelectionFilter.All,
            IsChecked = Filter == SelectionFilter.All
        });
        
        items.Add(new SelectModeContextMenuItem
        {
            Text = "Filter: Boundaries Only",
            Action = () => Filter = SelectionFilter.Boundaries,
            IsChecked = Filter == SelectionFilter.Boundaries
        });
        
        items.Add(new SelectModeContextMenuItem
        {
            Text = "Filter: Polylines Only",
            Action = () => Filter = SelectionFilter.Polylines,
            IsChecked = Filter == SelectionFilter.Polylines
        });
        
        items.Add(new SelectModeContextMenuItem
        {
            Text = "Filter: Vertices Only",
            Action = () => Filter = SelectionFilter.Vertices,
            IsChecked = Filter == SelectionFilter.Vertices
        });
        
        items.Add(new SelectModeContextMenuItem { IsSeparator = true });
        
        // Mode actions
        items.Add(new SelectModeContextMenuItem
        {
            Text = "Exit Selection Mode",
            Action = () => _modeManager.ReturnToIdle()
        });
        
        return items;
    }
}

/// <summary>
/// Context menu item for SelectMode with support for checked items and actions
/// </summary>
public class SelectModeContextMenuItem : IContextMenuItem
{
    public string Text { get; set; } = string.Empty;
    public Action? Action { get; set; }
    public System.Windows.Input.ICommand? Command { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsSeparator { get; set; }
    public bool IsChecked { get; set; }
    public IEnumerable<IContextMenuItem>? SubItems { get; set; }
}


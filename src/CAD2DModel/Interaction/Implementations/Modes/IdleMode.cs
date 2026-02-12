using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Selection;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Default idle mode - handles basic selection and interaction
/// </summary>
public class IdleMode : InteractionModeBase
{
    private readonly IModeManager _modeManager;
    private readonly ICommandManager _commandManager;
    private readonly ISelectionService _selectionService;
    private readonly IGeometryModel _geometryModel;
    private readonly ISnapService _snapService;
    private Point2D? _selectionBoxStart;
    private Point2D _currentMousePosition;
    private Camera.Camera2D? _camera;
    
    public IdleMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        ISelectionService selectionService,
        IGeometryModel geometryModel,
        ISnapService snapService)
    {
        _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
        _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        _geometryModel = geometryModel ?? throw new ArgumentNullException(nameof(geometryModel));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
    }
    
    public override string Name => "Idle";
    public override string StatusPrompt => "Ready";
    
    public override Cursor Cursor
    {
        get
        {
            // Show pickbox cursor when there are selections
            if (_selectionService.SelectedEntities.Count > 0 ||
                _selectionService.SelectedVertices.Count > 0 ||
                _selectionService.SelectedSegments.Count > 0)
            {
                return Interaction.Cursor.PickBox;
            }
            return Interaction.Cursor.Arrow;
        }
    }
    
    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _camera = context.Camera;
    }
    
    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button == MouseButton.Left)
        {
            // Use camera-aware tolerance
            double worldTolerance = _camera != null ? 5.0 * _camera.Scale : 0.5;
            
            // Try to select an entity
            var hitEntity = _selectionService.HitTest(worldPoint, worldTolerance, _geometryModel.Entities);
            
            if (hitEntity != null)
            {
                // Select the entity
                bool addToSelection = modifiers.HasFlag(ModifierKeys.Control);
                _selectionService.Select(hitEntity, addToSelection);
            }
            else
            {
                // Start selection box (or will be treated as click-to-clear in OnMouseUp)
                _selectionBoxStart = worldPoint;
                _currentMousePosition = worldPoint;
            }
        }
    }
    
    public override void OnMouseMove(Point2D worldPoint, ModifierKeys modifiers)
    {
        // Update selection box if dragging
        if (_selectionBoxStart.HasValue)
        {
            _currentMousePosition = worldPoint;
        }
    }
    
    public override void OnMouseUp(Point2D worldPoint, MouseButton button)
    {
        if (button == MouseButton.Left && _selectionBoxStart.HasValue)
        {
            var boxStart = _selectionBoxStart.Value;
            
            // Check if this was a click (small movement) or a drag
            double distance = boxStart.DistanceTo(worldPoint);
            double clickTolerance = _camera != null ? 5.0 * _camera.Scale : 0.5; // 5 pixels
            
            if (distance < clickTolerance)
            {
                // Click on empty space - clear selection
                _selectionService.ClearAllSelections();
            }
            else
            {
                // Complete selection box
                var box = new Rect2D(
                    Math.Min(boxStart.X, worldPoint.X),
                    Math.Min(boxStart.Y, worldPoint.Y),
                    Math.Abs(worldPoint.X - boxStart.X),
                    Math.Abs(worldPoint.Y - boxStart.Y));
                
                // Determine if crossing or window selection
                bool crossing = boxStart.X > worldPoint.X; // Crossing selection (right to left)
                
                var selectedEntities = _selectionService.SelectInBox(box, _geometryModel.Entities, crossing);
                
                bool addToSelection = false; // First select replaces selection
                foreach (var entity in selectedEntities)
                {
                    _selectionService.Select(entity, addToSelection);
                    addToSelection = true; // After first, always add
                }
            }
            
            _selectionBoxStart = null;
        }
    }
    
    public override void OnKeyDown(Key key, ModifierKeys modifiers)
    {
        switch (key)
        {
            case Key.Delete:
                // Delete selected entities using command for undo/redo support
                if (_selectionService.SelectedEntities.Any())
                {
                    var command = new DeleteMultipleEntitiesCommand(_geometryModel, _selectionService.SelectedEntities.ToList());
                    _commandManager.Execute(command);
                    _selectionService.ClearSelection();
                }
                break;
                
            case Key.Escape:
                // Clear selection
                _selectionService.ClearSelection();
                break;
        }
    }
    
    public override void Render(IRenderContext context)
    {
        // Draw selection box if active
        if (_selectionBoxStart.HasValue)
        {
            var start = _selectionBoxStart.Value;
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
    
    private void DrawVertexHandle(IRenderContext context, Point2D location, bool isSelected)
    {
        if (_camera == null)
            return;
        
        // Calculate handle size in world units (larger and more visible)
        double halfSize = (8.0 / 2.0) * _camera.Scale; // 8 pixels
        
        byte r = isSelected ? (byte)255 : (byte)100;
        byte g = isSelected ? (byte)165 : (byte)100;
        byte b = isSelected ? (byte)0 : (byte)255;
        
        // Draw a filled square handle (draw 4 lines to create filled appearance)
        var topLeft = new Point2D(location.X - halfSize, location.Y - halfSize);
        var topRight = new Point2D(location.X + halfSize, location.Y - halfSize);
        var bottomRight = new Point2D(location.X + halfSize, location.Y + halfSize);
        var bottomLeft = new Point2D(location.X - halfSize, location.Y + halfSize);
        
        // Draw outline with thicker stroke
        context.DrawLine(topLeft, topRight, r, g, b, 2, dashed: false);
        context.DrawLine(topRight, bottomRight, r, g, b, 2, dashed: false);
        context.DrawLine(bottomRight, bottomLeft, r, g, b, 2, dashed: false);
        context.DrawLine(bottomLeft, topLeft, r, g, b, 2, dashed: false);
    }
    
    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        var items = new List<IContextMenuItem>();
        
        // Selection actions
        if (_selectionService.SelectedEntities.Any() || _selectionService.SelectedVertices.Any())
        {
            items.Add(new ContextMenuItem
            {
                Text = "Clear All Selections",
                Action = () => _selectionService.ClearAllSelections()
            });
            
            items.Add(new ContextMenuItem { IsSeparator = true });
        }
        
        if (_selectionService.SelectedEntities.Any())
        {
            items.Add(new ContextMenuItem
            {
                Text = $"Delete ({_selectionService.SelectedEntities.Count} entities)",
                Action = () => {
                    var command = new DeleteMultipleEntitiesCommand(_geometryModel, _selectionService.SelectedEntities.ToList());
                    _commandManager.Execute(command);
                    _selectionService.ClearSelection();
                }
            });
            
            // Check if any selected entities are boundaries
            var selectedBoundaries = _selectionService.SelectedEntities.OfType<Boundary>().ToList();
            if (selectedBoundaries.Any())
            {
                bool allIntersectable = selectedBoundaries.All(b => b.Intersectable);
                
                items.Add(new ContextMenuItem
                {
                    Text = "Make Intersectable",
                    IsChecked = allIntersectable,
                    Action = () => {
                        foreach (var boundary in selectedBoundaries)
                        {
                            boundary.Intersectable = !allIntersectable;
                        }
                        // Apply rules to detect intersections
                        _geometryModel.ApplyAllRules();
                    }
                });
            }
            
            items.Add(new ContextMenuItem
            {
                Text = "Properties..."
            });
            
            items.Add(new ContextMenuItem { IsSeparator = true });
        }
        
        items.Add(new ContextMenuItem
        {
            Text = "Add Boundary",
            Action = () => {
                var mode = new AddBoundaryMode(_modeManager, _commandManager, _geometryModel, _snapService);
                _modeManager.EnterMode(mode);
            }
        });
        
        items.Add(new ContextMenuItem
        {
            Text = "Add Polyline",
            Action = () => {
                var mode = new AddPolylineMode(_modeManager, _commandManager, _geometryModel, _snapService);
                _modeManager.EnterMode(mode);
            }
        });
        
        items.Add(new ContextMenuItem { IsSeparator = true });
        
        items.Add(new ContextMenuItem
        {
            Text = "Trim",
            Action = () => {
                var mode = new TrimMode(_geometryModel, _commandManager, _selectionService, _modeManager);
                _modeManager.EnterMode(mode);
            }
        });
        
        items.Add(new ContextMenuItem
        {
            Text = "Extend",
            Action = () => {
                var mode = new ExtendMode(_geometryModel, _commandManager, _selectionService, _modeManager);
                _modeManager.EnterMode(mode);
            }
        });
        
        items.Add(new ContextMenuItem { IsSeparator = true });
        
        items.Add(new ContextMenuItem
        {
            Text = "Move Vertex",
            Action = () => {
                var mode = new MoveVertexMode(_modeManager, _commandManager, _selectionService, _snapService, _geometryModel);
                _modeManager.EnterMode(mode);
            }
        });
        
        items.Add(new ContextMenuItem
        {
            Text = "Move Boundary",
            Action = () => {
                var mode = new MoveBoundaryMode(_modeManager, _commandManager, _selectionService, _snapService, _geometryModel);
                _modeManager.EnterMode(mode);
            }
        });
        
        return items;
    }
}

/// <summary>
/// Simple context menu item implementation with Action support
/// </summary>
public class ContextMenuItem : IContextMenuItem
{
    public string Text { get; set; } = string.Empty;
    public Action? Action { get; set; }
    public System.Windows.Input.ICommand? Command { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsSeparator { get; set; }
    public bool IsChecked { get; set; }
    public IEnumerable<IContextMenuItem>? SubItems { get; set; }
}

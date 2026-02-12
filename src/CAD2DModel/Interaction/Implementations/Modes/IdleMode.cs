using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

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
    private Point2D? _selectionBoxStart;
    
    public IdleMode(
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
    
    public override string Name => "Idle";
    public override string StatusPrompt => "Click to select entities, drag for selection box";
    public override Cursor Cursor => Interaction.Cursor.Arrow;
    
    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button == MouseButton.Left)
        {
            // Try to select an entity
            var hitEntity = _selectionService.HitTest(worldPoint, 0.5, _geometryModel.Entities);
            
            if (hitEntity != null)
            {
                // Select the entity
                bool addToSelection = modifiers.HasFlag(ModifierKeys.Control);
                _selectionService.Select(hitEntity, addToSelection);
            }
            else
            {
                // Start selection box
                _selectionBoxStart = worldPoint;
                
                // Clear selection if not holding control
                if (!modifiers.HasFlag(ModifierKeys.Control))
                {
                    _selectionService.ClearSelection();
                }
            }
        }
    }
    
    public override void OnMouseMove(Point2D worldPoint, ModifierKeys modifiers)
    {
        // Update selection box if dragging
        if (_selectionBoxStart.HasValue)
        {
            // Selection box visualization would be handled in Render()
        }
    }
    
    public override void OnMouseUp(Point2D worldPoint, MouseButton button)
    {
        if (button == MouseButton.Left && _selectionBoxStart.HasValue)
        {
            // Complete selection box
            var boxStart = _selectionBoxStart.Value;
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
            
            _selectionBoxStart = null;
        }
    }
    
    public override void OnKeyDown(Key key, ModifierKeys modifiers)
    {
        switch (key)
        {
            case Key.Delete:
                // Delete selected entities
                foreach (var entity in _selectionService.SelectedEntities.ToList())
                {
                    _geometryModel.RemoveEntity(entity);
                }
                _selectionService.ClearSelection();
                break;
                
            case Key.Escape:
                // Clear selection
                _selectionService.ClearSelection();
                break;
        }
    }
    
    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        var items = new List<IContextMenuItem>();
        
        if (_selectionService.SelectedEntities.Any())
        {
            items.Add(new ContextMenuItem
            {
                Text = "Delete"
            });
            
            items.Add(new ContextMenuItem
            {
                Text = "Properties..."
            });
        }
        
        items.Add(new ContextMenuItem { IsSeparator = true });
        
        items.Add(new ContextMenuItem
        {
            Text = "Add Boundary"
        });
        
        items.Add(new ContextMenuItem
        {
            Text = "Add Polyline"
        });
        
        return items;
    }
}

/// <summary>
/// Simple context menu item implementation
/// </summary>
internal class ContextMenuItem : IContextMenuItem
{
    public string Text { get; set; } = string.Empty;
    public System.Windows.Input.ICommand? Command { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsSeparator { get; set; }
    public bool IsChecked { get; set; }
    public IEnumerable<IContextMenuItem>? SubItems { get; set; }
}

using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Selection;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Substates for MoveVertexMode
/// </summary>
public abstract class MoveVertexSubState : ModeSubState
{
    public static readonly SelectingVerticesSubState SelectingVertices = new();
    public static readonly SettingOriginPointSubState SettingOriginPoint = new();
    public static readonly PreviewingMoveSubState PreviewingMove = new();
    
    public sealed class SelectingVerticesSubState : MoveVertexSubState
    {
        public override string Name => "SelectingVertices";
        public override string Description => "Selecting vertices to move";
    }
    
    public sealed class SettingOriginPointSubState : MoveVertexSubState
    {
        public override string Name => "SettingOriginPoint";
        public override string Description => "Setting origin point for move";
    }
    
    public sealed class PreviewingMoveSubState : MoveVertexSubState
    {
        public override string Name => "PreviewingMove";
        public override string Description => "Previewing move with live update";
    }
}

/// <summary>
/// Mode for moving vertices of polylines and boundaries
/// CAD-style workflow with origin point and live preview:
/// 1. Select one or more vertices (click to select, Ctrl+Click to add, Enter when done)
/// 2. Click to set origin point for the move
/// 3. Move mouse to see live preview of new positions
/// 4. Click to finalize the move
/// Press U to undo last step, R to redo
/// </summary>
public class MoveVertexMode : InteractionModeBase
{
    private readonly IModeManager _modeManager;
    private readonly ICommandManager _commandManager;
    private readonly ISelectionService _selectionService;
    private readonly ISnapService _snapService;
    private readonly IGeometryModel _geometryModel;
    
    private Point2D? _originPoint;
    private Point2D _currentPreviewPoint;
    private Dictionary<VertexHandle, Point2D> _originalLocations;
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
        _originalLocations = new Dictionary<VertexHandle, Point2D>();
    }
    
    public override string Name => "Move Vertex";
    
    public override Cursor Cursor
    {
        get
        {
            if (SubState == MoveVertexSubState.SelectingVertices)
                return Interaction.Cursor.PickBox;
            else if (SubState == MoveVertexSubState.SettingOriginPoint || SubState == MoveVertexSubState.PreviewingMove)
                return Interaction.Cursor.Cross;
            else
                return Interaction.Cursor.Arrow;
        }
    }
    
    public override string StatusPrompt
    {
        get
        {
            if (SubState == MoveVertexSubState.SelectingVertices)
                return GetSelectionPrompt();
            else if (SubState == MoveVertexSubState.SettingOriginPoint)
                return "Click origin point for move (or press U to go back)";
            else if (SubState == MoveVertexSubState.PreviewingMove)
                return "Move to destination and click to finalize (or press U to go back)";
            else
                return "Move Vertex Mode";
        }
    }
    
    private string GetSelectionPrompt()
    {
        int count = _selectionService.SelectedVertices.Count;
        if (count == 0)
            return "Select vertices to move (Click to select, Ctrl+Click to add, Enter when done)";
        else if (count == 1)
            return "1 vertex selected (Click to select more, Ctrl+Click to add, Enter to continue)";
        else
            return $"{count} vertices selected (Click to select more, Ctrl+Click to add, Enter to continue)";
    }
    
    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _camera = context.Camera;
        SubState = MoveVertexSubState.SelectingVertices;
        _originPoint = null;
        _originalLocations.Clear();
        
        // Clear any previous selections to start fresh
        _selectionService.ClearAllSelections();
        
        State = ModeState.WaitingForInput;
    }
    
    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button == MouseButton.Left)
        {
            if (SubState == MoveVertexSubState.SelectingVertices)
            {
                // Selection phase - select vertices
                HandleVertexSelection(worldPoint, modifiers);
            }
            else if (SubState == MoveVertexSubState.SettingOriginPoint)
            {
                // Set origin point with snapping
                Point2D snappedOrigin = worldPoint;
                if (_camera != null)
                {
                    var snapResult = _snapService.Snap(worldPoint, _geometryModel.Entities, _camera);
                    if (snapResult != null)
                    {
                        snappedOrigin = snapResult.SnappedPoint;
                    }
                }
                
                _originPoint = snappedOrigin;
                _currentPreviewPoint = snappedOrigin;
                
                // Store original locations
                _originalLocations.Clear();
                foreach (var handle in _selectionService.SelectedVertices)
                {
                    _originalLocations[handle] = handle.Location;
                }
                
                // Move to preview phase
                var oldSubState = SubState;
                SubState = MoveVertexSubState.PreviewingMove;
                State = ModeState.Active;
                OnSubStateChanged(oldSubState, SubState);
            }
            else if (SubState == MoveVertexSubState.PreviewingMove)
            {
                // Finalize the move
                FinalizeMove(worldPoint);
            }
        }
        else if (button == MouseButton.Right)
        {
            // Right-click to show context menu
        }
    }
    
    private void HandleVertexSelection(Point2D worldPoint, ModifierKeys modifiers)
    {
        double tolerance = _camera != null ? 8.0 * _camera.Scale : 0.5;
        
        // Find ALL vertices at this location (handles coincident vertices at intersections)
        var clickedVertices = _selectionService.HitTestAllVertices(worldPoint, tolerance, _geometryModel.Entities);
        
        if (clickedVertices.Count > 0)
        {
            bool ctrlPressed = (modifiers & ModifierKeys.Control) != 0;
            
            if (ctrlPressed)
            {
                // Toggle selection for all vertices at this location
                foreach (var vertex in clickedVertices)
                {
                    _selectionService.ToggleVertexSelection(vertex);
                }
            }
            else
            {
                // Clear selection and select all vertices at this location
                _selectionService.ClearVertexSelection();
                foreach (var vertex in clickedVertices)
                {
                    _selectionService.SelectVertex(vertex, addToSelection: true);
                }
            }
            
            // Trigger UI update
            OnSubStateChanged(SubState, SubState);
        }
        else
        {
            // Clicked on empty space - clear selection
            if ((modifiers & ModifierKeys.Control) == 0)
            {
                _selectionService.ClearVertexSelection();
                OnSubStateChanged(SubState, SubState);
            }
        }
    }
    
    public override void OnMouseMove(Point2D worldPoint, ModifierKeys modifiers)
    {
        if (SubState == MoveVertexSubState.PreviewingMove && _originPoint.HasValue)
        {
            // Apply snapping
            Point2D snappedPoint = worldPoint;
            if (_camera != null)
            {
                var snapResult = _snapService.Snap(worldPoint, _geometryModel.Entities, _camera);
                if (snapResult != null)
                {
                    snappedPoint = snapResult.SnappedPoint;
                }
                
                // Apply ortho snap if enabled (after other snaps)
                if ((_snapService.ActiveSnapModes & SnapMode.Ortho) != 0)
                {
                    var orthoResult = _snapService.SnapToOrtho(snappedPoint, _originPoint.Value);
                    if (orthoResult.IsSnapped)
                    {
                        snappedPoint = orthoResult.SnappedPoint;
                    }
                }
            }
            
            _currentPreviewPoint = snappedPoint;
            
            // Calculate offset from origin
            var offset = new Vector2D(
                _currentPreviewPoint.X - _originPoint.Value.X,
                _currentPreviewPoint.Y - _originPoint.Value.Y
            );
            
            // Update all selected vertices to show live preview
            foreach (var handle in _selectionService.SelectedVertices)
            {
                var originalLoc = _originalLocations[handle];
                var vertex = GetVertex(handle);
                if (vertex != null)
                {
                    vertex.Location = new Point2D(
                        originalLoc.X + offset.X,
                        originalLoc.Y + offset.Y
                    );
                }
            }
            
            // Notify that geometry has changed for live contour update
            _geometryModel.NotifyGeometryChanged();
        }
    }
    
    private Vertex? GetVertex(VertexHandle handle)
    {
        if (handle.Entity is Polyline polyline)
            return polyline.Vertices[handle.VertexIndex];
        if (handle.Entity is Boundary boundary)
            return boundary.Vertices[handle.VertexIndex];
        return null;
    }
    
    public override void OnMouseUp(Point2D worldPoint, MouseButton button)
    {
        // Mouse up is not used in this mode (clicks are handled in OnMouseDown)
    }
    
    private void FinalizeMove(Point2D destinationPoint)
    {
        if (!_originPoint.HasValue || _selectionService.SelectedVertices.Count == 0)
            return;
        
        // Apply snapping to destination
        Point2D snappedDestination = destinationPoint;
        if (_camera != null)
        {
            var snapResult = _snapService.Snap(destinationPoint, _geometryModel.Entities, _camera);
            if (snapResult != null)
            {
                snappedDestination = snapResult.SnappedPoint;
            }
        }
        
        // Calculate final offset
        var offset = new Vector2D(
            snappedDestination.X - _originPoint.Value.X,
            snappedDestination.Y - _originPoint.Value.Y
        );
        
        // Restore original locations first
        foreach (var handle in _selectionService.SelectedVertices)
        {
            var vertex = GetVertex(handle);
            if (vertex != null)
            {
                vertex.Location = _originalLocations[handle];
            }
        }
        
        // Collect all affected entities for rule application
        var affectedEntities = new HashSet<IEntity>();
        
        // Execute move commands for all vertices
        foreach (var handle in _selectionService.SelectedVertices)
        {
            var originalLoc = _originalLocations[handle];
            var newLoc = new Point2D(
                originalLoc.X + offset.X,
                originalLoc.Y + offset.Y
            );
            var vertex = GetVertex(handle);
            if (vertex != null)
            {
                // Pass parent entity and model to command so rules can be applied
                var command = new MoveVertexCommand(vertex, newLoc, handle.Entity, _geometryModel);
                _commandManager.Execute(command);
                
                // Track affected entity
                affectedEntities.Add(handle.Entity);
            }
        }
        
        // Apply rules to all affected entities (handles intersections, duplicates, etc.)
        foreach (var entity in affectedEntities)
        {
            _geometryModel.ApplyRulesToEntity(entity);
        }
        
        // Apply rules to ALL entities to detect new intersections with other boundaries
        _geometryModel.ApplyAllRules();
        
        // Reset for next move operation
        var oldSubState = SubState;
        SubState = MoveVertexSubState.SelectingVertices;
        _originPoint = null;
        _originalLocations.Clear();
        _selectionService.ClearVertexSelection();
        State = ModeState.WaitingForInput;
        OnSubStateChanged(oldSubState, SubState);
    }
    
    public override void OnKeyDown(Key key, ModifierKeys modifiers)
    {
        switch (key)
        {
            case Key.Escape:
                // Cancel and return to idle
                CancelMove();
                break;
                
            case Key.Enter:
                // Proceed to next phase
                if (SubState == MoveVertexSubState.SelectingVertices && _selectionService.SelectedVertices.Count > 0)
                {
                    var oldSubState = SubState;
                    SubState = MoveVertexSubState.SettingOriginPoint;
                    State = ModeState.WaitingForInput;
                    OnSubStateChanged(oldSubState, SubState);
                }
                break;
                
            case Key.U:
                // Undo - go back one phase
                UndoPhase();
                break;
                
            case Key.R:
                // Redo - go forward one phase (only makes sense after undo)
                RedoPhase();
                break;
                
            case Key.Delete:
                // Delete selected vertices in selection phase
                if (SubState == MoveVertexSubState.SelectingVertices && _selectionService.SelectedVertices.Count > 0)
                {
                    DeleteSelectedVertices();
                }
                break;
        }
    }
    
    private void UndoPhase()
    {
        var oldSubState = SubState;
        
        if (SubState == MoveVertexSubState.SettingOriginPoint)
        {
            // Go back to selection
            SubState = MoveVertexSubState.SelectingVertices;
            OnSubStateChanged(oldSubState, SubState);
        }
        else if (SubState == MoveVertexSubState.PreviewingMove)
        {
            // Go back to setting origin, restore original positions
            RestoreOriginalLocations();
            _originPoint = null;
            SubState = MoveVertexSubState.SettingOriginPoint;
            OnSubStateChanged(oldSubState, SubState);
        }
    }
    
    private void RedoPhase()
    {
        // For now, redo doesn't make sense in this context
        // It would require storing phase history which we're not doing yet
    }
    
    private void RestoreOriginalLocations()
    {
        foreach (var handle in _selectionService.SelectedVertices)
        {
            var vertex = GetVertex(handle);
            if (vertex != null && _originalLocations.TryGetValue(handle, out var originalLoc))
            {
                vertex.Location = originalLoc;
            }
        }
    }
    
    private void CancelMove()
    {
        // Restore original locations if in preview phase
        if (SubState == MoveVertexSubState.PreviewingMove)
        {
            RestoreOriginalLocations();
        }
        
        State = ModeState.Cancelled;
        SubState = MoveVertexSubState.SelectingVertices;
        _originPoint = null;
        _originalLocations.Clear();
        _selectionService.ClearAllSelections();
        
        // Return to idle mode
        _modeManager.ReturnToIdle();
    }
    
    public override void Render(IRenderContext context)
    {
        base.Render(context);
        
        // Highlight selected vertices with cyan circles
        const int segments = 16;
        const double radius = 6.0; // pixels (will be scaled by camera)
        
        foreach (var handle in _selectionService.SelectedVertices)
        {
            Point2D center = handle.Location;
            double worldRadius = context.Camera.Scale * radius;
            
            // Draw circle as line segments
            for (int i = 0; i < segments; i++)
            {
                double angle1 = 2.0 * Math.PI * i / segments;
                double angle2 = 2.0 * Math.PI * (i + 1) / segments;
                
                Point2D p1 = new Point2D(
                    center.X + worldRadius * Math.Cos(angle1),
                    center.Y + worldRadius * Math.Sin(angle1)
                );
                Point2D p2 = new Point2D(
                    center.X + worldRadius * Math.Cos(angle2),
                    center.Y + worldRadius * Math.Sin(angle2)
                );
                
                context.DrawLine(p1, p2, 0, 255, 255, 3);
            }
        }
        
        // Draw origin point marker if set
        if (_originPoint.HasValue && SubState == MoveVertexSubState.PreviewingMove)
        {
            Point2D origin = _originPoint.Value;
            double markerSize = context.Camera.Scale * 8.0;
            
            // Draw crosshair at origin
            context.DrawLine(
                new Point2D(origin.X - markerSize, origin.Y),
                new Point2D(origin.X + markerSize, origin.Y),
                255, 165, 0, 2); // Orange
            context.DrawLine(
                new Point2D(origin.X, origin.Y - markerSize),
                new Point2D(origin.X, origin.Y + markerSize),
                255, 165, 0, 2); // Orange
        }
    }
    
    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        var items = new List<IContextMenuItem>();
        
        if (SubState == MoveVertexSubState.SelectingVertices)
        {
            if (_selectionService.SelectedVertices.Count > 0)
            {
                items.Add(new SelectModeContextMenuItem
                {
                    Text = $"Finish Selection ({_selectionService.SelectedVertices.Count} vertices)",
                    Action = () => {
                        var oldSubState = SubState;
                        SubState = MoveVertexSubState.SettingOriginPoint;
                        OnSubStateChanged(oldSubState, SubState);
                    },
                    IsEnabled = true
                });
                
                items.Add(new SelectModeContextMenuItem { IsSeparator = true });
                
                items.Add(new SelectModeContextMenuItem
                {
                    Text = "Clear Selection",
                    Action = () => {
                        _selectionService.ClearVertexSelection();
                        OnSubStateChanged(SubState, SubState);
                    }
                });
            }
        }
        else if (SubState == MoveVertexSubState.SettingOriginPoint)
        {
            items.Add(new SelectModeContextMenuItem
            {
                Text = "Back to Selection",
                Action = () => UndoPhase()
            });
        }
        else if (SubState == MoveVertexSubState.PreviewingMove)
        {
            items.Add(new SelectModeContextMenuItem
            {
                Text = "Finalize Move",
                Action = () => FinalizeMove(_currentPreviewPoint)
            });
            
            items.Add(new SelectModeContextMenuItem
            {
                Text = "Back to Origin Point",
                Action = () => UndoPhase()
            });
        }
        
        items.Add(new SelectModeContextMenuItem { IsSeparator = true });
        
        items.Add(new SelectModeContextMenuItem
        {
            Text = "Exit Move Vertex Mode",
            Action = () => CancelMove()
        });
        
        return items;
    }
    
    private void DeleteSelectedVertices()
    {
        // Group vertices by their parent entity
        var verticesByEntity = new Dictionary<IEntity, List<(VertexHandle handle, int index)>>();
        
        foreach (var handle in _selectionService.SelectedVertices.ToList())
        {
            if (!verticesByEntity.ContainsKey(handle.Entity))
            {
                verticesByEntity[handle.Entity] = new List<(VertexHandle, int)>();
            }
            verticesByEntity[handle.Entity].Add((handle, handle.VertexIndex));
        }
        
        // Create composite command to delete all vertices
        var compositeCommand = new CompositeCommand("Delete selected vertices");
        
        foreach (var kvp in verticesByEntity)
        {
            var entity = kvp.Key;
            var vertices = kvp.Value.OrderByDescending(v => v.index).ToList(); // Delete from end to maintain indices
            
            // Check if deletion would leave too few vertices
            int verticesAfterDeletion = 0;
            if (entity is Polyline polyline)
            {
                verticesAfterDeletion = polyline.Vertices.Count - vertices.Count;
                if (verticesAfterDeletion < 2)
                {
                    // Would leave too few vertices - delete the whole entity instead
                    compositeCommand.AddCommand(new DeleteEntityCommand(_geometryModel, entity));
                    continue;
                }
            }
            else if (entity is Boundary boundary)
            {
                verticesAfterDeletion = boundary.Vertices.Count - vertices.Count;
                if (verticesAfterDeletion < 3)
                {
                    // Would leave too few vertices - delete the whole entity instead
                    compositeCommand.AddCommand(new DeleteEntityCommand(_geometryModel, entity));
                    continue;
                }
            }
            
            // Delete individual vertices
            foreach (var (handle, index) in vertices)
            {
                compositeCommand.AddCommand(new DeleteVertexCommand(entity, index));
            }
        }
        
        // Execute the composite command
        _commandManager.Execute(compositeCommand);
        
        // Clear selection
        _selectionService.ClearVertexSelection();
        OnSubStateChanged(SubState, SubState);
    }
}

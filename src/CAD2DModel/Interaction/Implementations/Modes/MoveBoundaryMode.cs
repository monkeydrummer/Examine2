using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Substates for MoveBoundaryMode
/// </summary>
public abstract class MoveBoundarySubState : ModeSubState
{
    public static readonly SelectingEntitiesSubState SelectingEntities = new();
    public static readonly SettingOriginPointSubState SettingOriginPoint = new();
    public static readonly PreviewingMoveSubState PreviewingMove = new();
    
    public sealed class SelectingEntitiesSubState : MoveBoundarySubState
    {
        public override string Name => "SelectingEntities";
        public override string Description => "Selecting entities to move";
    }
    
    public sealed class SettingOriginPointSubState : MoveBoundarySubState
    {
        public override string Name => "SettingOriginPoint";
        public override string Description => "Setting origin point for move";
    }
    
    public sealed class PreviewingMoveSubState : MoveBoundarySubState
    {
        public override string Name => "PreviewingMove";
        public override string Description => "Previewing move with live update";
    }
}

/// <summary>
/// Mode for moving entire boundaries and polylines
/// CAD-style workflow with origin point and live preview:
/// 1. Select one or more entities (click to select, Ctrl+Click to add, Enter when done)
/// 2. Click to set origin point for the move
/// 3. Move mouse to see live preview of new positions
/// 4. Click to finalize the move
/// Press U to undo last step, Escape to cancel
/// </summary>
public class MoveBoundaryMode : InteractionModeBase
{
    private readonly IModeManager _modeManager;
    private readonly ICommandManager _commandManager;
    private readonly ISelectionService _selectionService;
    private readonly ISnapService _snapService;
    private readonly IGeometryModel _geometryModel;
    
    private Point2D? _originPoint;
    private Point2D _currentPreviewPoint;
    private Dictionary<IEntity, Dictionary<Vertex, Point2D>> _originalLocations;
    private Camera.Camera2D? _camera;
    
    public MoveBoundaryMode(
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
        _originalLocations = new Dictionary<IEntity, Dictionary<Vertex, Point2D>>();
    }
    
    public override string Name => "Move Entity";
    
    public override Cursor Cursor
    {
        get
        {
            if (SubState == MoveBoundarySubState.SelectingEntities)
                return Interaction.Cursor.PickBox;
            else if (SubState == MoveBoundarySubState.SettingOriginPoint || SubState == MoveBoundarySubState.PreviewingMove)
                return Interaction.Cursor.Cross;
            else
                return Interaction.Cursor.Arrow;
        }
    }
    
    public override string StatusPrompt
    {
        get
        {
            if (SubState == MoveBoundarySubState.SelectingEntities)
                return GetSelectionPrompt();
            else if (SubState == MoveBoundarySubState.SettingOriginPoint)
                return "Click origin point for move (or press U to go back)";
            else if (SubState == MoveBoundarySubState.PreviewingMove)
                return "Move to destination and click to finalize (or press U to go back)";
            else
                return "Move Entity Mode";
        }
    }
    
    private string GetSelectionPrompt()
    {
        int count = _selectionService.SelectedEntities.Count;
        if (count == 0)
            return "Select entities to move (Click to select, Ctrl+Click to add, Enter when done)";
        else if (count == 1)
            return "1 entity selected (Click to select more, Ctrl+Click to add, Enter to continue)";
        else
            return $"{count} entities selected (Click to select more, Ctrl+Click to add, Enter to continue)";
    }
    
    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _camera = context.Camera;
        SubState = MoveBoundarySubState.SelectingEntities;
        _originPoint = null;
        _originalLocations.Clear();
        
        // Keep existing entity selections if any, otherwise start fresh
        if (_selectionService.SelectedEntities.Count == 0)
        {
            State = ModeState.WaitingForInput;
        }
        else
        {
            State = ModeState.WaitingForInput;
        }
    }
    
    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button == MouseButton.Left)
        {
            if (SubState == MoveBoundarySubState.SelectingEntities)
            {
                // Selection phase - select entities
                HandleEntitySelection(worldPoint, modifiers);
            }
            else if (SubState == MoveBoundarySubState.SettingOriginPoint)
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
                
                // Store original locations for all vertices in selected entities
                _originalLocations.Clear();
                foreach (var entity in _selectionService.SelectedEntities)
                {
                    if (entity is Polyline polyline)
                    {
                        var vertexLocations = new Dictionary<Vertex, Point2D>();
                        foreach (var vertex in polyline.Vertices)
                        {
                            vertexLocations[vertex] = vertex.Location;
                        }
                        _originalLocations[entity] = vertexLocations;
                    }
                }
                
                // Move to preview phase
                var oldSubState = SubState;
                SubState = MoveBoundarySubState.PreviewingMove;
                State = ModeState.Active;
                OnSubStateChanged(oldSubState, SubState);
            }
            else if (SubState == MoveBoundarySubState.PreviewingMove)
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
    
    private void HandleEntitySelection(Point2D worldPoint, ModifierKeys modifiers)
    {
        double tolerance = _camera != null ? 8.0 * _camera.Scale : 0.5;
        
        // Hit test for entities
        var clickedEntity = _selectionService.HitTest(worldPoint, tolerance, _geometryModel.Entities);
        
        if (clickedEntity != null)
        {
            bool ctrlPressed = (modifiers & ModifierKeys.Control) != 0;
            
            if (ctrlPressed)
            {
                // Toggle selection
                _selectionService.ToggleSelection(clickedEntity);
            }
            else
            {
                // Regular click: if entity is already selected, keep selection; otherwise, select only this one
                if (!_selectionService.SelectedEntities.Contains(clickedEntity))
                {
                    _selectionService.ClearSelection();
                    _selectionService.Select(clickedEntity);
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
                _selectionService.ClearSelection();
                OnSubStateChanged(SubState, SubState);
            }
        }
    }
    
    public override void OnMouseMove(Point2D worldPoint, ModifierKeys modifiers)
    {
        if (SubState == MoveBoundarySubState.PreviewingMove && _originPoint.HasValue)
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
            }
            
            _currentPreviewPoint = snappedPoint;
            
            // Calculate offset from origin
            var offset = new Vector2D(
                _currentPreviewPoint.X - _originPoint.Value.X,
                _currentPreviewPoint.Y - _originPoint.Value.Y
            );
            
            // Update all vertices in selected entities to show live preview
            foreach (var kvp in _originalLocations)
            {
                var entity = kvp.Key;
                var vertexLocations = kvp.Value;
                
                if (entity is Polyline polyline)
                {
                    foreach (var vertex in polyline.Vertices)
                    {
                        if (vertexLocations.TryGetValue(vertex, out var originalLoc))
                        {
                            vertex.Location = new Point2D(
                                originalLoc.X + offset.X,
                                originalLoc.Y + offset.Y
                            );
                        }
                    }
                }
            }
            
            // Trigger redraw to show live preview
        }
    }
    
    public override void OnMouseUp(Point2D worldPoint, MouseButton button)
    {
        // Mouse up is not used in this mode (clicks are handled in OnMouseDown)
    }
    
    private void FinalizeMove(Point2D destinationPoint)
    {
        if (!_originPoint.HasValue || _selectionService.SelectedEntities.Count == 0)
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
        foreach (var kvp in _originalLocations)
        {
            var entity = kvp.Key;
            var vertexLocations = kvp.Value;
            
            if (entity is Polyline polyline)
            {
                foreach (var vertex in polyline.Vertices)
                {
                    if (vertexLocations.TryGetValue(vertex, out var originalLoc))
                    {
                        vertex.Location = originalLoc;
                    }
                }
            }
        }
        
        // Create composite command to move all entities
        var compositeCommand = new CompositeCommand($"Move {_selectionService.SelectedEntities.Count} entity(ies)");
        
        foreach (var kvp in _originalLocations)
        {
            var entity = kvp.Key;
            var vertexLocations = kvp.Value;
            
            if (entity is Polyline polyline)
            {
                // Create move commands for each vertex in the entity
                foreach (var vertex in polyline.Vertices)
                {
                    if (vertexLocations.TryGetValue(vertex, out var originalLoc))
                    {
                        var newLoc = new Point2D(
                            originalLoc.X + offset.X,
                            originalLoc.Y + offset.Y
                        );
                        compositeCommand.AddCommand(new MoveVertexCommand(vertex, newLoc, entity, _geometryModel));
                    }
                }
            }
        }
        
        // Execute the composite command
        _commandManager.Execute(compositeCommand);
        
        // Apply rules to all moved entities
        foreach (var entity in _originalLocations.Keys)
        {
            _geometryModel.ApplyRulesToEntity(entity);
        }
        
        // Apply rules to ALL entities to detect new intersections
        _geometryModel.ApplyAllRules();
        
        // Reset for next move or return to idle
        var oldSubState = SubState;
        SubState = MoveBoundarySubState.SelectingEntities;
        _originPoint = null;
        _originalLocations.Clear();
        _selectionService.ClearSelection();
        State = ModeState.WaitingForInput;
        OnSubStateChanged(oldSubState, SubState);
        
        // Return to idle mode
        _modeManager.ReturnToIdle();
    }
    
    private void CancelMove()
    {
        // Restore original positions if we were previewing
        if (SubState == MoveBoundarySubState.PreviewingMove)
        {
            RestoreOriginalLocations();
        }
        
        // Clear selection and return to idle
        _selectionService.ClearSelection();
        _modeManager.ReturnToIdle();
    }
    
    private void RestoreOriginalLocations()
    {
        foreach (var kvp in _originalLocations)
        {
            var entity = kvp.Key;
            var vertexLocations = kvp.Value;
            
            if (entity is Polyline polyline)
            {
                foreach (var vertex in polyline.Vertices)
                {
                    if (vertexLocations.TryGetValue(vertex, out var originalLoc))
                    {
                        vertex.Location = originalLoc;
                    }
                }
            }
        }
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
                if (SubState == MoveBoundarySubState.SelectingEntities && _selectionService.SelectedEntities.Count > 0)
                {
                    var oldSubState = SubState;
                    SubState = MoveBoundarySubState.SettingOriginPoint;
                    State = ModeState.WaitingForInput;
                    OnSubStateChanged(oldSubState, SubState);
                }
                break;
                
            case Key.U:
                // Undo - go back one phase
                UndoPhase();
                break;
                
            case Key.Delete:
                // Delete selected entities in selection phase
                if (SubState == MoveBoundarySubState.SelectingEntities && _selectionService.SelectedEntities.Count > 0)
                {
                    var command = new DeleteMultipleEntitiesCommand(_geometryModel, _selectionService.SelectedEntities.ToList());
                    _commandManager.Execute(command);
                    _selectionService.ClearSelection();
                    OnSubStateChanged(SubState, SubState);
                }
                break;
        }
    }
    
    private void UndoPhase()
    {
        var oldSubState = SubState;
        
        if (SubState == MoveBoundarySubState.SettingOriginPoint)
        {
            // Go back to selection
            SubState = MoveBoundarySubState.SelectingEntities;
            OnSubStateChanged(oldSubState, SubState);
        }
        else if (SubState == MoveBoundarySubState.PreviewingMove)
        {
            // Go back to setting origin, restore original positions
            RestoreOriginalLocations();
            _originPoint = null;
            SubState = MoveBoundarySubState.SettingOriginPoint;
            OnSubStateChanged(oldSubState, SubState);
        }
    }
    
    public override void Render(IRenderContext context)
    {
        // Render origin point if set
        if (_originPoint.HasValue)
        {
            var origin = _originPoint.Value;
            double markerSize = _camera != null ? _camera.Scale * 10 : 0.5;
            
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
        
        // Render preview line if in preview mode
        if (SubState == MoveBoundarySubState.PreviewingMove && _originPoint.HasValue)
        {
            context.DrawLine(_originPoint.Value, _currentPreviewPoint, 255, 255, 0, 1, dashed: true);
        }
    }
    
    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        var items = new List<IContextMenuItem>();
        
        if (SubState == MoveBoundarySubState.SelectingEntities)
        {
            if (_selectionService.SelectedEntities.Count > 0)
            {
                items.Add(new SelectModeContextMenuItem
                {
                    Text = "Continue to Origin Point (Enter)",
                    Action = () => {
                        var oldSubState = SubState;
                        SubState = MoveBoundarySubState.SettingOriginPoint;
                        State = ModeState.WaitingForInput;
                        OnSubStateChanged(oldSubState, SubState);
                    }
                });
                
                items.Add(new SelectModeContextMenuItem
                {
                    Text = $"Delete Selected ({_selectionService.SelectedEntities.Count})",
                    Action = () => {
                        var command = new DeleteMultipleEntitiesCommand(_geometryModel, _selectionService.SelectedEntities.ToList());
                        _commandManager.Execute(command);
                        _selectionService.ClearSelection();
                    }
                });
            }
        }
        else if (SubState == MoveBoundarySubState.SettingOriginPoint)
        {
            items.Add(new SelectModeContextMenuItem
            {
                Text = "Back to Selection (U)",
                Action = () => UndoPhase()
            });
        }
        else if (SubState == MoveBoundarySubState.PreviewingMove)
        {
            items.Add(new SelectModeContextMenuItem
            {
                Text = "Finalize Move",
                Action = () => FinalizeMove(_currentPreviewPoint)
            });
            
            items.Add(new SelectModeContextMenuItem
            {
                Text = "Back to Origin Point (U)",
                Action = () => UndoPhase()
            });
        }
        
        items.Add(new SelectModeContextMenuItem { IsSeparator = true });
        
        items.Add(new SelectModeContextMenuItem
        {
            Text = "Exit Move Entity Mode (Esc)",
            Action = () => CancelMove()
        });
        
        return items;
    }
}

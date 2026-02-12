using CAD2DModel.Camera;
using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Selection;
using System.Collections.Generic;
using System.Linq;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Interaction mode for extending entities to meet boundary edges
/// </summary>
public class ExtendMode : InteractionModeBase
{
    private readonly IGeometryModel _model;
    private readonly ICommandManager _commandManager;
    private readonly ISelectionService _selectionService;
    private readonly IModeManager _modeManager;
    private Camera2D _camera = null!;
    private bool _selectingBoundarySegments = true;

    public override string Name => "Extend";
    
    public override string StatusPrompt => _selectingBoundarySegments
        ? $"Select boundary edges ({_selectionService.SelectedSegments.Count} segments selected) [Enter to finish, Escape to cancel]"
        : "Click on segments to extend [Escape to return to boundary edge selection]";

    public override Cursor Cursor => Interaction.Cursor.Cross;

    public ExtendMode(
        IGeometryModel model,
        ICommandManager commandManager,
        ISelectionService selectionService,
        IModeManager modeManager)
    {
        _model = model;
        _commandManager = commandManager;
        _selectionService = selectionService;
        _modeManager = modeManager;
    }

    public override void OnEnter(ModeContext context)
    {
        base.OnEnter(context);
        _camera = context.Camera;
        _selectingBoundarySegments = true;
        _selectionService.ClearAllSelections();
    }

    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button != MouseButton.Left)
            return;

        double tolerance = 5.0 * _camera.Scale;

        if (_selectingBoundarySegments)
        {
            // Select boundary segments (edges) using SelectionService
            var segment = _selectionService.HitTestSegment(worldPoint, tolerance, _model.Entities);
            if (segment != null)
            {
                // Toggle segment selection in the SelectionService
                _selectionService.ToggleSegmentSelection(segment);
                // Force status prompt refresh by changing state
                OnStateChanged(State, State);
            }
        }
        else
        {
            // Extend entity to boundary segments
            var segmentToExtend = _selectionService.HitTestSegment(worldPoint, tolerance, _model.Entities);
            if (segmentToExtend != null && !_selectionService.SelectedSegments.Any(s => s.Equals(segmentToExtend)))
            {
                ExtendSegment(segmentToExtend, worldPoint);
            }
        }
    }

    public override void OnKeyDown(Key key, ModifierKeys modifiers)
    {
        if (key == Key.Escape)
        {
            if (_selectingBoundarySegments)
            {
                // Cancel and return to idle
                _modeManager.ReturnToIdle();
            }
            else
            {
                // Return to boundary edge selection
                _selectingBoundarySegments = true;
                OnStateChanged(State, State); // Refresh status prompt
            }
        }
        else if (key == Key.Enter && _selectingBoundarySegments)
        {
            if (_selectionService.SelectedSegments.Count > 0)
            {
                _selectingBoundarySegments = false;
                OnStateChanged(State, State); // Refresh status prompt
            }
        }
    }

    public override void Render(IRenderContext context)
    {
        // Highlight boundary segments
        foreach (var segment in _selectionService.SelectedSegments)
        {
            context.DrawLine(
                segment.StartPoint,
                segment.EndPoint,
                0, 255, 0, // Green
                4.0f);
        }
    }

    private void ExtendSegment(SegmentHandle segmentToExtend, Point2D clickPoint)
    {
        // Only polylines can be extended (boundaries are closed)
        var entity = segmentToExtend.Entity;
        if (entity is not Polyline polyline)
            return;

        var vertices = polyline.Vertices.ToList();
        if (vertices.Count < 2)
            return;

        // Determine if we're extending from start or end based on which segment was clicked
        bool extendFromStart = (segmentToExtend.SegmentIndex == 0);
        bool extendFromEnd = (segmentToExtend.SegmentIndex == vertices.Count - 2);

        if (!extendFromStart && !extendFromEnd)
            return; // Can only extend from first or last segment

        // Find closest intersection with boundary segments
        Point2D? extensionPoint = null;
        double minDistance = double.MaxValue;

        Point2D endPoint = extendFromStart ? vertices[0].Location : vertices[^1].Location;
        Point2D directionPoint = extendFromStart ? vertices[1].Location : vertices[^2].Location;

        // Direction vector for extension
        double dx = endPoint.X - directionPoint.X;
        double dy = endPoint.Y - directionPoint.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1e-10)
            return;

        dx /= length;
        dy /= length;

        // Create an extended point far in the direction
        Point2D farPoint = new Point2D(
            endPoint.X + dx * 10000,
            endPoint.Y + dy * 10000);

        // Find intersections with boundary segments
        foreach (var boundarySegment in _selectionService.SelectedSegments)
        {
            // Create a temporary polyline for the extension line
            var extensionLine = new Polyline(new[] { endPoint, farPoint });
            
            // Create a temporary polyline/boundary for the boundary segment
            IEntity boundaryEntity;
            if (boundarySegment.Entity is Polyline)
            {
                boundaryEntity = new Polyline(new[] { boundarySegment.StartPoint, boundarySegment.EndPoint });
            }
            else
            {
                boundaryEntity = new Boundary(new[] { boundarySegment.StartPoint, boundarySegment.EndPoint, boundarySegment.StartPoint });
            }
            
            var intersections = IntersectionCalculator.EntityIntersections(
                extensionLine,
                boundaryEntity,
                extendFirst: false,
                extendSecond: false);

            foreach (var intersection in intersections)
            {
                double distance = endPoint.DistanceTo(intersection.Location);
                if (distance > 0.01 && distance < minDistance) // Must be beyond current end
                {
                    minDistance = distance;
                    extensionPoint = intersection.Location;
                }
            }
        }

        if (extensionPoint == null)
            return;

        // Create extended polyline
        var newVertices = new List<Point2D>();

        if (extendFromStart)
        {
            newVertices.Add(extensionPoint.Value);
            newVertices.AddRange(vertices.Select(v => v.Location));
        }
        else
        {
            newVertices.AddRange(vertices.Select(v => v.Location));
            newVertices.Add(extensionPoint.Value);
        }

        var extendedPolyline = new Polyline(newVertices);

        // Execute extend command
        var command = new ExtendCommand(_model, entity, extensionPoint.Value, extendedPolyline);
        _commandManager.Execute(command);
    }

    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        yield return new ExtendContextMenuItem 
        { 
            Text = "Finish Boundary Edge Selection", 
            IsEnabled = _selectingBoundarySegments && _selectionService.SelectedSegments.Count > 0,
            Action = () => _selectingBoundarySegments = false
        };
        
        yield return new ExtendContextMenuItem 
        { 
            Text = "Cancel Extend", 
            IsEnabled = true,
            Action = () => _modeManager.ReturnToIdle()
        };
    }

    internal class ExtendContextMenuItem : IContextMenuItem
    {
        public string Text { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public bool IsChecked { get; } = false;
        public bool IsSeparator { get; } = false;
        public System.Windows.Input.ICommand? Command { get; set; }
        public IEnumerable<IContextMenuItem>? SubItems { get; set; }
        public Action? Action { get; set; }
    }
}

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
/// Interaction mode for trimming entities at intersection points
/// </summary>
public class TrimMode : InteractionModeBase
{
    private readonly IGeometryModel _model;
    private readonly ICommandManager _commandManager;
    private readonly ISelectionService _selectionService;
    private readonly IModeManager _modeManager;
    private Camera2D _camera = null!;
    private bool _selectingCuttingSegments = true;

    public override string Name => "Trim";
    
    public override string StatusPrompt => _selectingCuttingSegments
        ? $"Select cutting edges ({_selectionService.SelectedSegments.Count} segments selected) [Enter to finish, Escape to cancel]"
        : "Click on segments to trim [Escape to return to cutting edge selection]";

    public override Cursor Cursor => Interaction.Cursor.Cross;

    public TrimMode(
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
        _selectingCuttingSegments = true;
        _selectionService.ClearAllSelections();
    }

    public override void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
        if (button != MouseButton.Left)
            return;

        double tolerance = 5.0 * _camera.Scale;

        if (_selectingCuttingSegments)
        {
            // Select cutting segments (edges) using SelectionService
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
            // Trim entity at intersections with cutting segments
            var segmentToTrim = _selectionService.HitTestSegment(worldPoint, tolerance, _model.Entities);
            if (segmentToTrim != null && !_selectionService.SelectedSegments.Any(s => s.Equals(segmentToTrim)))
            {
                TrimSegment(segmentToTrim, worldPoint);
            }
        }
    }

    public override void OnKeyDown(Key key, ModifierKeys modifiers)
    {
        if (key == Key.Escape)
        {
            if (_selectingCuttingSegments)
            {
                // Cancel and return to idle
                _modeManager.ReturnToIdle();
            }
            else
            {
                // Return to cutting edge selection
                _selectingCuttingSegments = true;
                OnStateChanged(State, State); // Refresh status prompt
            }
        }
        else if (key == Key.Enter && _selectingCuttingSegments)
        {
            if (_selectionService.SelectedSegments.Count > 0)
            {
                _selectingCuttingSegments = false;
                OnStateChanged(State, State); // Refresh status prompt
            }
        }
    }

    public override void Render(IRenderContext context)
    {
        // Highlight cutting segments
        foreach (var segment in _selectionService.SelectedSegments)
        {
            context.DrawLine(
                segment.StartPoint,
                segment.EndPoint,
                255, 165, 0, // Orange
                4.0f);
        }
    }

    private void TrimSegment(SegmentHandle segmentToTrim, Point2D clickPoint)
    {
        // Find all intersections between the segment and cutting segments
        var entity = segmentToTrim.Entity;
        var allIntersections = new List<(Point2D point, int segmentIndex, double parameter)>();

        foreach (var cuttingSegment in _selectionService.SelectedSegments)
        {
            var intersections = IntersectionCalculator.EntityIntersections(entity, cuttingSegment.Entity);
            foreach (var intersection in intersections)
            {
                // Only consider intersections on the segment we're trimming
                if (intersection.SegmentIndex1 == segmentToTrim.SegmentIndex)
                {
                    allIntersections.Add((intersection.Location, intersection.SegmentIndex1, intersection.Parameter1));
                }
            }
        }

        if (allIntersections.Count == 0)
            return;

        // Sort intersections by parameter along the segment
        allIntersections = allIntersections
            .OrderBy(i => i.parameter)
            .ToList();

        // Find intersections that bound the clicked segment
        IEntity? resultEntity = null;

        if (entity is Polyline polyline)
        {
            resultEntity = TrimPolyline(polyline, segmentToTrim.SegmentIndex, allIntersections);
        }
        else if (entity is Boundary boundary)
        {
            resultEntity = TrimBoundary(boundary, segmentToTrim.SegmentIndex, allIntersections);
        }

        // Execute trim command
        var command = new TrimCommand(_model, entity, 
            allIntersections.Select(i => i.point).ToList(), 
            resultEntity);
        _commandManager.Execute(command);
    }

    private Polyline? TrimPolyline(Polyline polyline, int clickedSegment, List<(Point2D point, int segmentIndex, double parameter)> intersections)
    {
        // Find the two closest intersections that bound the clicked segment
        var beforeIntersection = intersections
            .Where(i => i.segmentIndex < clickedSegment || 
                       (i.segmentIndex == clickedSegment && i.parameter < 0.5))
            .LastOrDefault();

        var afterIntersection = intersections
            .Where(i => i.segmentIndex > clickedSegment || 
                       (i.segmentIndex == clickedSegment && i.parameter >= 0.5))
            .FirstOrDefault();

        if (beforeIntersection == default && afterIntersection == default)
            return null; // No valid trim

        var vertices = polyline.Vertices.ToList();
        var newVertices = new List<Point2D>();

        // Keep vertices before the trim section
        if (beforeIntersection != default)
        {
            for (int i = 0; i <= beforeIntersection.segmentIndex; i++)
            {
                newVertices.Add(vertices[i].Location);
            }
            newVertices.Add(beforeIntersection.point);
        }
        else
        {
            newVertices.Clear(); // Trim from start
        }

        // Skip vertices in the trim section

        // Keep vertices after the trim section
        if (afterIntersection != default)
        {
            newVertices.Add(afterIntersection.point);
            for (int i = afterIntersection.segmentIndex + 1; i < vertices.Count; i++)
            {
                newVertices.Add(vertices[i].Location);
            }
        }

        if (newVertices.Count < 2)
            return null; // Polyline would be invalid

        return new Polyline(newVertices);
    }

    private Boundary? TrimBoundary(Boundary boundary, int clickedSegment, List<(Point2D point, int segmentIndex, double parameter)> intersections)
    {
        // For boundaries (closed shapes), trimming requires at least 2 intersections
        if (intersections.Count < 2)
            return null;

        // Find the two intersections closest to the clicked segment
        var sortedByDistance = intersections
            .OrderBy(i => Math.Abs(i.segmentIndex - clickedSegment))
            .ToList();

        if (sortedByDistance.Count < 2)
            return null;

        var first = sortedByDistance[0];
        var second = sortedByDistance[1];

        // Make sure first comes before second
        if (first.segmentIndex > second.segmentIndex ||
            (first.segmentIndex == second.segmentIndex && first.parameter > second.parameter))
        {
            (first, second) = (second, first);
        }

        var vertices = boundary.Vertices.ToList();
        var newVertices = new List<Point2D>();

        // Keep vertices before first intersection
        for (int i = 0; i <= first.segmentIndex; i++)
        {
            newVertices.Add(vertices[i].Location);
        }
        newVertices.Add(first.point);

        // Skip vertices between intersections

        // Add second intersection and vertices after
        newVertices.Add(second.point);
        for (int i = second.segmentIndex + 1; i < vertices.Count; i++)
        {
            newVertices.Add(vertices[i].Location);
        }

        if (newVertices.Count < 3)
            return null; // Boundary would be invalid

        return new Boundary(newVertices);
    }

    public override IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        yield return new TrimContextMenuItem 
        { 
            Text = "Finish Cutting Edge Selection", 
            IsEnabled = _selectingCuttingSegments && _selectionService.SelectedSegments.Count > 0,
            Action = () => _selectingCuttingSegments = false
        };
        
        yield return new TrimContextMenuItem 
        { 
            Text = "Cancel Trim", 
            IsEnabled = true,
            Action = () => _modeManager.ReturnToIdle()
        };
    }

    internal class TrimContextMenuItem : IContextMenuItem
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

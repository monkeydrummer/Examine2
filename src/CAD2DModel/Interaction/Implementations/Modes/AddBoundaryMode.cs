using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for creating new boundary entities by clicking points
/// </summary>
public class AddBoundaryMode : PolylineDrawingModeBase
{
    private Boundary? _currentBoundary;
    private bool _makeIntersectable = true; // User preference for new boundaries (default: intersectable)
    
    public AddBoundaryMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Boundary";
    
    protected override int MinimumPointCount => 3;
    protected override bool IsClosedShape => true;
    protected override string EntityTypeName => "Boundary";
    
    protected override void CreateTemporaryEntity(List<Point2D> points)
    {
        if (_currentBoundary == null && points.Count >= 2)
        {
            // Create boundary entity and add to model as we build it
            _currentBoundary = new Boundary
            {
                Name = $"Boundary {DateTime.Now:HHmmss}",
                IsClosed = true,
                Intersectable = _makeIntersectable
            };
            
            foreach (var pt in points)
            {
                _currentBoundary.AddVertex(pt);
            }
            
            _geometryModel.AddEntity(_currentBoundary);
        }
        else if (_currentBoundary != null && points.Count >= 2)
        {
            // Add the new point to existing boundary
            _currentBoundary.AddVertex(points[points.Count - 1]);
        }
    }
    
    protected override void UpdateTemporaryEntity(List<Point2D> points)
    {
        if (_currentBoundary == null)
        {
            CreateTemporaryEntity(points);
            return;
        }
        
        // Remove all vertices and re-add from points list
        while (_currentBoundary.Vertices.Count > 0)
        {
            _currentBoundary.RemoveVertex(_currentBoundary.Vertices[0]);
        }
        
        foreach (var point in points)
        {
            _currentBoundary.AddVertex(point);
        }
        
        // Remove boundary if less than 2 points remain
        if (points.Count < 2)
        {
            _geometryModel.RemoveEntity(_currentBoundary);
            _currentBoundary = null;
        }
    }
    
    protected override void RemoveTemporaryEntity()
    {
        if (_currentBoundary != null)
        {
            _geometryModel.RemoveEntity(_currentBoundary);
            _currentBoundary = null;
        }
    }
    
    protected override void CreateAndCommitEntity(List<Point2D> points)
    {
        if (points.Count < MinimumPointCount)
            return;
        
        // The temporary boundary already has the geometry rules applied
        // and is already in the model, so we just need to wrap it in a command
        if (_currentBoundary != null)
        {
            // Apply geometry rules now that the boundary is complete
            _geometryModel.ApplyRulesToEntity(_currentBoundary);
            
            // Remove the temporary boundary
            _geometryModel.RemoveEntity(_currentBoundary);
            
            // Add it back through the command manager for proper undo/redo support
            var command = new AddBoundaryCommand(_geometryModel, _currentBoundary);
            _commandManager.Execute(command);
            
            _currentBoundary = null;
        }
    }
    
    protected override void AddDerivedContextMenuItems(List<IContextMenuItem> items, Point2D worldPoint)
    {
        // Intersectable toggle (always available)
        items.Add(new DrawingModeContextMenuItem
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
        items.Add(new DrawingModeContextMenuItem { IsSeparator = true });
    }
}

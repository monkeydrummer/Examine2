using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for creating new polyline entities (open lines) by clicking points
/// </summary>
public class AddPolylineMode : PolylineDrawingModeBase
{
    private Polyline? _currentPolyline;
    
    public AddPolylineMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Polyline";
    
    protected override int MinimumPointCount => 2;
    protected override bool IsClosedShape => false;
    protected override string EntityTypeName => "Polyline";
    
    protected override void CreateTemporaryEntity(List<Point2D> points)
    {
        if (_currentPolyline == null && points.Count >= 2)
        {
            // Create polyline entity and add to model as we build it
            _currentPolyline = new Polyline
            {
                Name = $"Polyline {DateTime.Now:HHmmss}"
            };
            
            foreach (var pt in points)
            {
                _currentPolyline.AddVertex(pt);
            }
            
            _geometryModel.AddEntity(_currentPolyline);
        }
        else if (_currentPolyline != null && points.Count >= 2)
        {
            // Add the new point to existing polyline
            _currentPolyline.AddVertex(points[points.Count - 1]);
        }
    }
    
    protected override void UpdateTemporaryEntity(List<Point2D> points)
    {
        if (_currentPolyline == null)
        {
            CreateTemporaryEntity(points);
            return;
        }
        
        // Remove all vertices and re-add from points list
        while (_currentPolyline.Vertices.Count > 0)
        {
            _currentPolyline.RemoveVertex(_currentPolyline.Vertices[0]);
        }
        
        foreach (var point in points)
        {
            _currentPolyline.AddVertex(point);
        }
        
        // Remove polyline if less than 2 points remain
        if (points.Count < 2)
        {
            _geometryModel.RemoveEntity(_currentPolyline);
            _currentPolyline = null;
        }
    }
    
    protected override void RemoveTemporaryEntity()
    {
        if (_currentPolyline != null)
        {
            _geometryModel.RemoveEntity(_currentPolyline);
            _currentPolyline = null;
        }
    }
    
    protected override void CreateAndCommitEntity(List<Point2D> points)
    {
        if (points.Count < MinimumPointCount)
            return;
        
        // The temporary polyline already exists and is in the model
        // Apply geometry rules and then wrap it in a command
        if (_currentPolyline != null)
        {
            // Apply geometry rules now that the polyline is complete
            _geometryModel.ApplyRulesToEntity(_currentPolyline);
            
            // Remove the temporary polyline
            _geometryModel.RemoveEntity(_currentPolyline);
            
            // Add it back through the command manager for proper undo/redo support
            var command = new AddPolylineCommand(_geometryModel, _currentPolyline);
            _commandManager.Execute(command);
            
            _currentPolyline = null;
        }
    }
}

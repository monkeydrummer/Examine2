using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations.Modes;

/// <summary>
/// Mode for creating polyline annotations (for markup/notes) by clicking points
/// </summary>
public class AddPolylineAnnotationMode : PolylineDrawingModeBase
{
    private PolylineAnnotation? _currentAnnotation;
    private bool _arrowAtStart = false;
    private bool _arrowAtEnd = false;
    private ArrowStyle _arrowStyle = ArrowStyle.FilledTriangle;
    private double _arrowSize = 10.0;
    
    public AddPolylineAnnotationMode(
        IModeManager modeManager,
        ICommandManager commandManager,
        IGeometryModel geometryModel,
        ISnapService snapService)
        : base(modeManager, commandManager, geometryModel, snapService)
    {
    }
    
    public override string Name => "Add Polyline Annotation";
    
    protected override int MinimumPointCount => 2;
    protected override bool IsClosedShape => false;
    protected override string EntityTypeName => "Polyline Annotation";
    
    protected override void CreateTemporaryEntity(List<Point2D> points)
    {
        if (_currentAnnotation == null && points.Count >= 2)
        {
            // Create annotation entity
            _currentAnnotation = new PolylineAnnotation
            {
                ArrowAtStart = _arrowAtStart,
                ArrowAtEnd = _arrowAtEnd,
                ArrowStyle = _arrowStyle,
                ArrowSize = _arrowSize
            };
            
            foreach (var pt in points)
            {
                _currentAnnotation.Vertices.Add(pt);
            }
            
            _geometryModel.Annotations.Add(_currentAnnotation);
        }
        else if (_currentAnnotation != null && points.Count >= 2)
        {
            // Add the new point to existing annotation
            _currentAnnotation.Vertices.Add(points[points.Count - 1]);
        }
    }
    
    protected override void UpdateTemporaryEntity(List<Point2D> points)
    {
        if (_currentAnnotation == null)
        {
            CreateTemporaryEntity(points);
            return;
        }
        
        // Remove all vertices and re-add from points list
        _currentAnnotation.Vertices.Clear();
        
        foreach (var point in points)
        {
            _currentAnnotation.Vertices.Add(point);
        }
        
        // Remove annotation if less than 2 points remain
        if (points.Count < 2)
        {
            _geometryModel.Annotations.Remove(_currentAnnotation);
            _currentAnnotation = null;
        }
    }
    
    protected override void RemoveTemporaryEntity()
    {
        if (_currentAnnotation != null)
        {
            _geometryModel.Annotations.Remove(_currentAnnotation);
            _currentAnnotation = null;
        }
    }
    
    protected override void CreateAndCommitEntity(List<Point2D> points)
    {
        if (points.Count < MinimumPointCount)
            return;
        
        // The temporary annotation already exists
        // Just keep it in the annotations collection (annotations don't use command manager)
        if (_currentAnnotation != null)
        {
            // Annotation is already in the collection, just clear the reference
            _currentAnnotation = null;
        }
    }
    
    protected override void AddDerivedContextMenuItems(List<IContextMenuItem> items, Point2D worldPoint)
    {
        // Arrow options
        items.Add(new DrawingModeContextMenuItem
        {
            Text = "Arrow at Start",
            IsChecked = _arrowAtStart,
            Action = () => {
                _arrowAtStart = !_arrowAtStart;
                if (_currentAnnotation != null)
                {
                    _currentAnnotation.ArrowAtStart = _arrowAtStart;
                }
            }
        });
        
        items.Add(new DrawingModeContextMenuItem
        {
            Text = "Arrow at End",
            IsChecked = _arrowAtEnd,
            Action = () => {
                _arrowAtEnd = !_arrowAtEnd;
                if (_currentAnnotation != null)
                {
                    _currentAnnotation.ArrowAtEnd = _arrowAtEnd;
                }
            }
        });
        
        items.Add(new DrawingModeContextMenuItem { IsSeparator = true });
    }
}

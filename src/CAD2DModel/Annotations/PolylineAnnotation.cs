using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace CAD2DModel.Annotations;

/// <summary>
/// Polyline annotation (multi-segment line)
/// </summary>
public partial class PolylineAnnotation : AnnotationBase
{
    public ObservableCollection<Point2D> Vertices { get; } = new();
    
    [ObservableProperty]
    private bool _arrowAtStart;
    
    [ObservableProperty]
    private bool _arrowAtEnd;
    
    [ObservableProperty]
    private ArrowStyle _arrowStyle = ArrowStyle.FilledTriangle;
    
    [ObservableProperty]
    private double _arrowSize = 10.0;
    
    public PolylineAnnotation()
    {
        Vertices.CollectionChanged += (s, e) => OnPropertyChanged(nameof(Vertices));
    }
    
    public PolylineAnnotation(IEnumerable<Point2D> points) : this()
    {
        foreach (var point in points)
        {
            Vertices.Add(point);
        }
    }
    
    public override Rect2D GetBounds()
    {
        if (Vertices.Count == 0)
            return Rect2D.Empty;
        
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        
        foreach (var vertex in Vertices)
        {
            minX = Math.Min(minX, vertex.X);
            minY = Math.Min(minY, vertex.Y);
            maxX = Math.Max(maxX, vertex.X);
            maxY = Math.Max(maxY, vertex.Y);
        }
        
        return new Rect2D(minX - 5, minY - 5, (maxX - minX) + 10, (maxY - minY) + 10);
    }
    
    public override IReadOnlyList<IControlPoint> GetControlPoints()
    {
        var controlPoints = new List<IControlPoint>();
        
        for (int i = 0; i < Vertices.Count; i++)
        {
            controlPoints.Add(new ControlPoint(this, ControlPointType.Vertex, Vertices[i], i));
        }
        
        return controlPoints;
    }
    
    public override bool HitTest(Point2D worldPoint, double tolerance)
    {
        if (Vertices.Count < 2)
            return false;
        
        // Check each segment
        for (int i = 0; i < Vertices.Count - 1; i++)
        {
            double distance = DistanceToLineSegment(worldPoint, Vertices[i], Vertices[i + 1]);
            if (distance <= tolerance)
                return true;
        }
        
        return false;
    }
    
    public override void UpdateControlPoint(IControlPoint controlPoint, Point2D newLocation)
    {
        if (controlPoint.Type == ControlPointType.Vertex && controlPoint.Index >= 0 && controlPoint.Index < Vertices.Count)
        {
            Vertices[controlPoint.Index] = newLocation;
        }
    }
}

using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Annotations;

/// <summary>
/// Polygon annotation (closed polyline with fill/hatch)
/// </summary>
public partial class PolygonAnnotation : PolylineAnnotation
{
    [ObservableProperty]
    private bool _isFilled;
    
    [ObservableProperty]
    private Color _fillColor = new Color(128, 128, 128, 100);
    
    [ObservableProperty]
    private bool _isHatched;
    
    [ObservableProperty]
    private HatchStyle _hatchStyle = HatchStyle.None;
    
    [ObservableProperty]
    private Color _hatchColor = Color.Black;
    
    public PolygonAnnotation() : base()
    {
    }
    
    public PolygonAnnotation(IEnumerable<Point2D> points) : base(points)
    {
    }
    
    public override bool HitTest(Point2D worldPoint, double tolerance)
    {
        if (IsFilled && Vertices.Count >= 3)
        {
            // Check if point is inside polygon using ray casting algorithm
            bool inside = false;
            int j = Vertices.Count - 1;
            
            for (int i = 0; i < Vertices.Count; i++)
            {
                if ((Vertices[i].Y > worldPoint.Y) != (Vertices[j].Y > worldPoint.Y) &&
                    worldPoint.X < (Vertices[j].X - Vertices[i].X) * (worldPoint.Y - Vertices[i].Y) / 
                    (Vertices[j].Y - Vertices[i].Y) + Vertices[i].X)
                {
                    inside = !inside;
                }
                j = i;
            }
            
            if (inside)
                return true;
        }
        
        // Check edges (including closing edge)
        if (Vertices.Count < 2)
            return false;
        
        for (int i = 0; i < Vertices.Count - 1; i++)
        {
            double distance = DistanceToLineSegment(worldPoint, Vertices[i], Vertices[i + 1]);
            if (distance <= tolerance)
                return true;
        }
        
        // Check closing edge
        if (Vertices.Count >= 3)
        {
            double distance = DistanceToLineSegment(worldPoint, Vertices[^1], Vertices[0]);
            if (distance <= tolerance)
                return true;
        }
        
        return false;
    }
}

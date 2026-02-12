using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace CAD2DModel.Geometry;

/// <summary>
/// Polyline consisting of an ordered list of vertices
/// </summary>
public partial class Polyline : ObservableObject, IEntity
{
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private bool _isVisible = true;
    
    [ObservableProperty]
    private bool _isClosed;
    
    public ObservableCollection<Vertex> Vertices { get; init; } = new();
    
    public Polyline()
    {
        Vertices.CollectionChanged += (s, e) => OnPropertyChanged(nameof(Vertices));
    }
    
    public Polyline(IEnumerable<Point2D> points) : this()
    {
        foreach (var point in points)
        {
            Vertices.Add(new Vertex(point));
        }
    }
    
    public int VertexCount => Vertices.Count;
    
    public double Length
    {
        get
        {
            double length = 0;
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                length += Vertices[i].Location.DistanceTo(Vertices[i + 1].Location);
            }
            
            if (IsClosed && Vertices.Count > 0)
            {
                length += Vertices[^1].Location.DistanceTo(Vertices[0].Location);
            }
            
            return length;
        }
    }
    
    public Rect2D GetBounds()
    {
        if (Vertices.Count == 0)
            return Rect2D.Empty;
        
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        
        foreach (var vertex in Vertices)
        {
            minX = Math.Min(minX, vertex.Location.X);
            minY = Math.Min(minY, vertex.Location.Y);
            maxX = Math.Max(maxX, vertex.Location.X);
            maxY = Math.Max(maxY, vertex.Location.Y);
        }
        
        return new Rect2D(minX, minY, maxX - minX, maxY - minY);
    }
    
    public LineSegment GetSegment(int index)
    {
        if (index < 0 || index >= GetSegmentCount())
            throw new ArgumentOutOfRangeException(nameof(index));
        
        if (index == Vertices.Count - 1 && IsClosed)
        {
            return new LineSegment(Vertices[index].Location, Vertices[0].Location);
        }
        
        return new LineSegment(Vertices[index].Location, Vertices[index + 1].Location);
    }
    
    public int GetSegmentCount()
    {
        if (Vertices.Count < 2)
            return 0;
        
        return IsClosed ? Vertices.Count : Vertices.Count - 1;
    }
    
    public void AddVertex(Point2D location)
    {
        Vertices.Add(new Vertex(location));
    }
    
    public void InsertVertex(int index, Point2D location)
    {
        Vertices.Insert(index, new Vertex(location));
    }
    
    public void RemoveVertex(Vertex vertex)
    {
        Vertices.Remove(vertex);
    }
    
    public void RemoveVertexAt(int index)
    {
        if (index >= 0 && index < Vertices.Count)
        {
            Vertices.RemoveAt(index);
        }
    }
    
    public override string ToString()
    {
        return $"Polyline({Name}, {VertexCount} vertices, {(IsClosed ? "closed" : "open")})";
    }
}

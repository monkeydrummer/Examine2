using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Geometry;

/// <summary>
/// Vertex in a polyline
/// </summary>
public partial class Vertex : ObservableObject
{
    [ObservableProperty]
    private Point2D _location;
    
    [ObservableProperty]
    private bool _isSelected;
    
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public Vertex(Point2D location)
    {
        Location = location;
    }
    
    public override string ToString()
    {
        return $"Vertex({Location})";
    }
}

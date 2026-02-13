namespace CAD2DModel.Geometry;

/// <summary>
/// External boundary that defines the region where results will be computed
/// In boundary element analysis, this represents the outer boundary of the analysis domain
/// Results (stress, displacement) will be computed at points within this boundary
/// </summary>
public class ExternalBoundary : Boundary
{
    public ExternalBoundary() : base()
    {
        // External boundaries should not intersect with regular boundaries
        Intersectable = false;
        IsExternal = true;
    }
    
    public ExternalBoundary(IEnumerable<Point2D> points) : base(points)
    {
        Intersectable = false;
        IsExternal = true;
    }
    
    /// <summary>
    /// Marks this as an external boundary
    /// </summary>
    public bool IsExternal { get; private set; }
    
    /// <summary>
    /// Mesh resolution for result computation within this boundary
    /// Higher values = more points = smoother contours but slower computation
    /// </summary>
    public double MeshResolution { get; set; } = 0.5; // Mesh resolution for contours
    
    /// <summary>
    /// Whether results should be computed for this boundary
    /// </summary>
    public bool ComputeResults { get; set; } = true;
    
    public override string ToString()
    {
        return $"ExternalBoundary({Name}, {VertexCount} vertices, Area: {Area:F2})";
    }
}

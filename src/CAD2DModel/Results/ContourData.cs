using CAD2DModel.Geometry;

namespace CAD2DModel.Results;

/// <summary>
/// Represents contour data for visualization
/// Contains a mesh of points with associated result values
/// </summary>
public class ContourData
{
    /// <summary>
    /// The field this contour data represents
    /// </summary>
    public ResultField Field { get; set; }
    
    /// <summary>
    /// Mesh points where results are computed
    /// </summary>
    public List<Point2D> MeshPoints { get; set; } = new();
    
    /// <summary>
    /// Result values at each mesh point (same count as MeshPoints)
    /// </summary>
    public List<double> Values { get; set; } = new();
    
    /// <summary>
    /// Triangular mesh connectivity (triplets of indices into MeshPoints)
    /// Each triangle is defined by 3 consecutive indices
    /// </summary>
    public List<int> Triangles { get; set; } = new();
    
    /// <summary>
    /// Minimum value in the dataset
    /// </summary>
    public double MinValue { get; set; }
    
    /// <summary>
    /// Maximum value in the dataset
    /// </summary>
    public double MaxValue { get; set; }
    
    /// <summary>
    /// Excavation boundaries to be masked (drawn as white polygons on top)
    /// </summary>
    public List<Boundary> ExcavationsToMask { get; set; } = new();
    
    /// <summary>
    /// Timestamp when this data was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Check if the contour data is valid
    /// </summary>
    public bool IsValid => MeshPoints.Count > 0 && 
                          Values.Count == MeshPoints.Count && 
                          Triangles.Count > 0 && 
                          Triangles.Count % 3 == 0;
}

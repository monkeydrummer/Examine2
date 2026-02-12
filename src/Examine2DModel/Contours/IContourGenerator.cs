using CAD2DModel.Geometry;
using Examine2DModel.Stress;

namespace Examine2DModel.Contours;

/// <summary>
/// Contour line
/// </summary>
public class ContourLine
{
    public double Level { get; init; }
    public List<Point2D[]> Segments { get; init; } = new();
}

/// <summary>
/// Filled contour polygon
/// </summary>
public class ContourPolygon
{
    public double MinLevel { get; init; }
    public double MaxLevel { get; init; }
    public List<Point2D> Points { get; init; } = new();
    public uint Color { get; init; }
}

/// <summary>
/// Contour data
/// </summary>
public class ContourData
{
    public List<ContourLine> Lines { get; init; } = new();
    public List<ContourPolygon> FilledPolygons { get; init; } = new();
}

/// <summary>
/// Contour generation options
/// </summary>
public class ContourOptions
{
    public double[] Levels { get; set; } = Array.Empty<double>();
    public int NumberOfLevels { get; set; } = 10;
    public string ColorMap { get; set; } = "viridis";
    public bool ShowLines { get; set; } = true;
    public bool ShowFilled { get; set; } = true;
}

/// <summary>
/// Interface for contour generation
/// </summary>
public interface IContourGenerator
{
    /// <summary>
    /// Generate contours from a stress field
    /// </summary>
    ContourData Generate(StressField field, ContourOptions options);
    
    /// <summary>
    /// Generate contours asynchronously
    /// </summary>
    Task<ContourData> GenerateAsync(StressField field, ContourOptions options, CancellationToken cancellationToken = default);
}

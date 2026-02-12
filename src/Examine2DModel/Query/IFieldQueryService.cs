using CAD2DModel.Geometry;
using Examine2DModel.Stress;

namespace Examine2DModel.Query;

/// <summary>
/// Query point result
/// </summary>
public class QueryPointResult
{
    public double Sigma1 { get; set; }
    public double Sigma3 { get; set; }
    public Vector2D Displacement { get; set; }
    public double StrengthFactor { get; set; }
    public double Strain { get; set; }
}

/// <summary>
/// Query sample result along a polyline
/// </summary>
public class QuerySampleResult
{
    public double DistanceAlongPath { get; set; }
    public Point2D Location { get; set; }
    public double Sigma1 { get; set; }
    public double Sigma3 { get; set; }
    public Vector2D Displacement { get; set; }
    public double StrengthFactor { get; set; }
}

/// <summary>
/// Interface for querying field results at specific locations
/// </summary>
public interface IFieldQueryService
{
    /// <summary>
    /// Evaluate results at a specific point
    /// </summary>
    QueryPointResult EvaluateAtPoint(Point2D location, StressField field);
    
    /// <summary>
    /// Evaluate results along a polyline
    /// </summary>
    List<QuerySampleResult> EvaluateAlongPolyline(Polyline polyline, StressField field, int sampleCount);
}

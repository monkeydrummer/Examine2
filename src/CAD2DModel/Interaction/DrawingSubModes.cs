namespace CAD2DModel.Interaction;

/// <summary>
/// Drawing sub-mode for polyline/boundary creation
/// </summary>
public enum DrawingSubMode
{
    /// <summary>
    /// Drawing straight line segments
    /// </summary>
    Line,
    
    /// <summary>
    /// Drawing an arc segment
    /// </summary>
    Arc,
    
    /// <summary>
    /// Drawing a complete circle (replaces all previous points)
    /// </summary>
    Circle
}

/// <summary>
/// Method for drawing arcs
/// </summary>
public enum ArcDrawMode
{
    /// <summary>
    /// Arc defined by 3 points (start, mid, end)
    /// </summary>
    ThreePoint,
    
    /// <summary>
    /// Arc defined by start point, end point, and radius
    /// </summary>
    StartEndRadius,
    
    /// <summary>
    /// Arc defined by start point, end point, and bulge factor
    /// </summary>
    StartEndBulge
}

/// <summary>
/// Method for drawing circles
/// </summary>
public enum CircleDrawMode
{
    /// <summary>
    /// Circle defined by center point and radius
    /// </summary>
    CenterRadius,
    
    /// <summary>
    /// Circle defined by 2 points (diameter)
    /// </summary>
    TwoPointDiameter,
    
    /// <summary>
    /// Circle defined by 3 points on circumference
    /// </summary>
    ThreePoint
}

/// <summary>
/// Parameters for arc drawing
/// </summary>
public class ArcDrawingParameters
{
    /// <summary>
    /// Number of line segments to use for arc discretization
    /// </summary>
    public int Segments { get; set; } = 16;
    
    /// <summary>
    /// Arc draw mode
    /// </summary>
    public ArcDrawMode DrawMode { get; set; } = ArcDrawMode.ThreePoint;
    
    /// <summary>
    /// Radius for StartEndRadius mode
    /// </summary>
    public double Radius { get; set; } = 10.0;
    
    /// <summary>
    /// Bulge factor for StartEndBulge mode (-1 to 1, 0 = straight line)
    /// </summary>
    public double Bulge { get; set; } = 0.5;
}

/// <summary>
/// Parameters for circle drawing
/// </summary>
public class CircleDrawingParameters
{
    /// <summary>
    /// Number of line segments to use for circle discretization
    /// </summary>
    public int Segments { get; set; } = 32;
    
    /// <summary>
    /// Circle draw mode
    /// </summary>
    public CircleDrawMode DrawMode { get; set; } = CircleDrawMode.CenterRadius;
    
    /// <summary>
    /// Radius for CenterRadius mode
    /// </summary>
    public double Radius { get; set; } = 10.0;
}

using CAD2DModel.Geometry;

namespace Examine2DModel.BEM;

/// <summary>
/// Represents a single boundary element in the BEM discretization
/// Maps to BCOMPUTE2D_ELEMENT from C++ code
/// </summary>
public class BoundaryElement
{
    /// <summary>
    /// Start point of the element
    /// </summary>
    public Point2D StartPoint { get; set; }
    
    /// <summary>
    /// End point of the element
    /// </summary>
    public Point2D EndPoint { get; set; }
    
    /// <summary>
    /// Midpoint of the element (collocation point)
    /// </summary>
    public Point2D MidPoint { get; set; }
    
    /// <summary>
    /// Length of the element
    /// </summary>
    public double Length { get; set; }
    
    /// <summary>
    /// Direction cosine (cos of angle from x-axis)
    /// </summary>
    public double CosineDirection { get; set; }
    
    /// <summary>
    /// Direction sine (sin of angle from x-axis)
    /// </summary>
    public double SineDirection { get; set; }
    
    /// <summary>
    /// Element type (constant, linear, quadratic)
    /// </summary>
    public int ElementType { get; set; }
    
    /// <summary>
    /// Boundary condition type
    /// 1 = traction specified, 2 = displacement specified
    /// </summary>
    public int BoundaryConditionType { get; set; }
    
    /// <summary>
    /// Normal boundary condition value (stress or displacement)
    /// </summary>
    public double NormalBoundaryCondition { get; set; }
    
    /// <summary>
    /// Shear boundary condition value (stress or displacement)
    /// </summary>
    public double ShearBoundaryCondition { get; set; }
    
    /// <summary>
    /// Boundary ID this element belongs to
    /// </summary>
    public int BoundaryId { get; set; }
    
    /// <summary>
    /// True if this is a surface (ground) element
    /// </summary>
    public bool IsGroundSurface { get; set; }
    
    /// <summary>
    /// Create a boundary element from start and end points
    /// </summary>
    public static BoundaryElement Create(Point2D start, Point2D end, int elementType, int boundaryId)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        
        return new BoundaryElement
        {
            StartPoint = start,
            EndPoint = end,
            MidPoint = new Point2D((start.X + end.X) / 2.0, (start.Y + end.Y) / 2.0),
            Length = length,
            CosineDirection = dx / length,
            SineDirection = dy / length,
            ElementType = elementType,
            BoundaryId = boundaryId,
            BoundaryConditionType = 1, // Default: traction specified
            NormalBoundaryCondition = 0.0,
            ShearBoundaryCondition = 0.0,
            IsGroundSurface = false
        };
    }
}

namespace CAD2DModel.Annotations;

/// <summary>
/// Type of control point on an annotation
/// </summary>
public enum ControlPointType
{
    /// <summary>
    /// Start point of a linear annotation
    /// </summary>
    Start,
    
    /// <summary>
    /// End point of a linear annotation
    /// </summary>
    End,
    
    /// <summary>
    /// Middle point for positioning or offsetting
    /// </summary>
    Middle,
    
    /// <summary>
    /// Third point (e.g., for dimension offset)
    /// </summary>
    Third,
    
    /// <summary>
    /// Top-left corner
    /// </summary>
    TopLeft,
    
    /// <summary>
    /// Top-right corner
    /// </summary>
    TopRight,
    
    /// <summary>
    /// Bottom-left corner
    /// </summary>
    BottomLeft,
    
    /// <summary>
    /// Bottom-right corner
    /// </summary>
    BottomRight,
    
    /// <summary>
    /// Top edge midpoint
    /// </summary>
    TopEdge,
    
    /// <summary>
    /// Bottom edge midpoint
    /// </summary>
    BottomEdge,
    
    /// <summary>
    /// Left edge midpoint
    /// </summary>
    LeftEdge,
    
    /// <summary>
    /// Right edge midpoint
    /// </summary>
    RightEdge,
    
    /// <summary>
    /// Rotation handle
    /// </summary>
    Rotation,
    
    /// <summary>
    /// Arc begin point
    /// </summary>
    ArcBegin,
    
    /// <summary>
    /// Arc end point
    /// </summary>
    ArcEnd,
    
    /// <summary>
    /// Vertex in a polyline or polygon
    /// </summary>
    Vertex
}

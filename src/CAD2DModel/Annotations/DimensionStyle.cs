namespace CAD2DModel.Annotations;

/// <summary>
/// Style for dimension annotations
/// </summary>
public enum DimensionStyle
{
    /// <summary>
    /// Linear horizontal or vertical dimension
    /// </summary>
    Linear,
    
    /// <summary>
    /// Aligned dimension (parallel to measured line)
    /// </summary>
    Aligned,
    
    /// <summary>
    /// Angular dimension
    /// </summary>
    Angular,
    
    /// <summary>
    /// Radius dimension
    /// </summary>
    Radial,
    
    /// <summary>
    /// Diameter dimension
    /// </summary>
    Diametric
}

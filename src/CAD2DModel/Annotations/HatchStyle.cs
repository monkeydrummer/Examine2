namespace CAD2DModel.Annotations;

/// <summary>
/// Hatch pattern style for filled shapes
/// </summary>
public enum HatchStyle
{
    /// <summary>
    /// No hatching
    /// </summary>
    None,
    
    /// <summary>
    /// Horizontal lines
    /// </summary>
    Horizontal,
    
    /// <summary>
    /// Vertical lines
    /// </summary>
    Vertical,
    
    /// <summary>
    /// Forward diagonal lines (\\\)
    /// </summary>
    ForwardDiagonal,
    
    /// <summary>
    /// Backward diagonal lines (///)
    /// </summary>
    BackwardDiagonal,
    
    /// <summary>
    /// Cross hatch (horizontal + vertical)
    /// </summary>
    Cross,
    
    /// <summary>
    /// Diagonal cross hatch
    /// </summary>
    DiagonalCross
}

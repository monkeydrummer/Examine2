namespace CAD2DModel.Results;

/// <summary>
/// Types of result fields that can be displayed as contours
/// </summary>
public enum ResultField
{
    /// <summary>
    /// Horizontal stress (sigma_x)
    /// </summary>
    StressX,
    
    /// <summary>
    /// Vertical stress (sigma_y)
    /// </summary>
    StressY,
    
    /// <summary>
    /// Shear stress (tau_xy)
    /// </summary>
    StressXY,
    
    /// <summary>
    /// Maximum principal stress (sigma_1)
    /// </summary>
    PrincipalStress1,
    
    /// <summary>
    /// Minimum principal stress (sigma_3)
    /// </summary>
    PrincipalStress3,
    
    /// <summary>
    /// Von Mises equivalent stress
    /// </summary>
    VonMisesStress,
    
    /// <summary>
    /// Horizontal displacement (u_x)
    /// </summary>
    DisplacementX,
    
    /// <summary>
    /// Vertical displacement (u_y)
    /// </summary>
    DisplacementY,
    
    /// <summary>
    /// Total displacement magnitude
    /// </summary>
    DisplacementMagnitude
}

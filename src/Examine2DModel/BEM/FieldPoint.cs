using CAD2DModel.Geometry;

namespace Examine2DModel.BEM;

/// <summary>
/// Represents a field point where stresses and displacements are evaluated
/// Maps to BCOMPUTE2D_FPS from C++ code
/// </summary>
public class FieldPoint
{
    /// <summary>
    /// Index of the field point
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Location of the field point
    /// </summary>
    public Point2D Location { get; set; }
    
    /// <summary>
    /// Grid level this point belongs to (for adaptive grid generation)
    /// </summary>
    public GridLevel GridLevel { get; set; }
    
    /// <summary>
    /// True if this point is inside an excavation (invalid for stress calculation)
    /// </summary>
    public bool InsideExcavation { get; set; }
    
    /// <summary>
    /// True if this point is too close to a boundary element (invalid)
    /// </summary>
    public bool TooCloseToElement { get; set; }
    
    /// <summary>
    /// X displacement
    /// </summary>
    public double Ux { get; set; }
    
    /// <summary>
    /// Y displacement
    /// </summary>
    public double Uy { get; set; }
    
    /// <summary>
    /// Z (out-of-plane) displacement
    /// </summary>
    public double Uz { get; set; }
    
    /// <summary>
    /// X-direction stress
    /// </summary>
    public double SigmaX { get; set; }
    
    /// <summary>
    /// Y-direction stress
    /// </summary>
    public double SigmaY { get; set; }
    
    /// <summary>
    /// Z-direction (out-of-plane) stress
    /// </summary>
    public double SigmaZ { get; set; }
    
    /// <summary>
    /// XY shear stress
    /// </summary>
    public double TauXY { get; set; }
    
    /// <summary>
    /// XZ shear stress
    /// </summary>
    public double TauXZ { get; set; }
    
    /// <summary>
    /// YZ shear stress
    /// </summary>
    public double TauYZ { get; set; }
    
    /// <summary>
    /// Major principal stress (3D)
    /// </summary>
    public double Sigma1 { get; set; }
    
    /// <summary>
    /// Intermediate principal stress (3D)
    /// </summary>
    public double Sigma2 { get; set; }
    
    /// <summary>
    /// Minor principal stress (3D)
    /// </summary>
    public double Sigma3 { get; set; }
    
    /// <summary>
    /// In-plane major principal stress
    /// </summary>
    public double InPlaneSigma1 { get; set; }
    
    /// <summary>
    /// In-plane minor principal stress
    /// </summary>
    public double InPlaneSigma3 { get; set; }
    
    /// <summary>
    /// Principal stress angle (radians from x-axis)
    /// </summary>
    public double PrincipalAngle { get; set; }
    
    /// <summary>
    /// Strength factor (Factor of safety)
    /// </summary>
    public double StrengthFactor { get; set; }
    
    /// <summary>
    /// First stress invariant (I1 = σx + σy + σz)
    /// </summary>
    public double I1 { get; set; }
    
    /// <summary>
    /// Second deviatoric stress invariant (J2)
    /// </summary>
    public double J2 { get; set; }
    
    /// <summary>
    /// Lode angle (radians)
    /// </summary>
    public double LodeAngle { get; set; }
    
    /// <summary>
    /// True if this field point has valid results
    /// </summary>
    public bool IsValid => !InsideExcavation && !TooCloseToElement;
}

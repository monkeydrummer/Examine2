using Examine2DModel.Analysis;

namespace Examine2DModel.BEM;

/// <summary>
/// Configuration for Boundary Element Method solver
/// </summary>
public class BEMConfiguration
{
    /// <summary>
    /// Target number of boundary elements for discretization.
    /// Lower = faster but less accurate. Higher = slower but more accurate.
    /// Default: 100 (matches old C++ code behavior)
    /// Typical range: 50-500
    /// </summary>
    public int TargetElementCount { get; set; } = 100;
    
    /// <summary>
    /// Enable adaptive element sizing (denser at corners, sparser on straights)
    /// </summary>
    public bool UseAdaptiveElementSizing { get; set; } = true;
    
    /// <summary>
    /// Maximum refinement factor for adaptive sizing (1.0 = no refinement, 4.0 = up to 4x denser at corners)
    /// Higher values create more elements at corners and high curvature regions
    /// </summary>
    public double MaxRefinementFactor { get; set; } = 4.0;
    
    /// <summary>
    /// Type of boundary element to use
    /// NOTE: Currently only Constant elements are fully implemented and tested!
    /// Linear and Quadratic elements require additional integration point handling.
    /// </summary>
    public ElementType ElementType { get; set; } = ElementType.Constant;
    
    /// <summary>
    /// Type of plane strain analysis
    /// </summary>
    public PlaneStrainType PlaneStrainType { get; set; } = PlaneStrainType.PlaneStrain;
    
    /// <summary>
    /// Convergence tolerance for iterative solvers
    /// </summary>
    public double Tolerance { get; set; } = 1e-6;
    
    /// <summary>
    /// Maximum iterations for iterative solvers
    /// </summary>
    public int MaxIterations { get; set; } = 1000;
    
    /// <summary>
    /// Use direct solver (LU) for small problems, iterative (BiCGStab) for large
    /// Threshold in degrees of freedom
    /// Default: 2000 (use direct solver for most typical BEM problems)
    /// BEM influence matrices are dense and well-suited for direct solvers
    /// </summary>
    public int DirectSolverThreshold { get; set; } = 2000;
    
    /// <summary>
    /// Enable caching of influence matrix and solutions
    /// </summary>
    public bool EnableCaching { get; set; } = true;
}

namespace Examine2DModel.Materials;

/// <summary>
/// Base interface for material properties
/// </summary>
public interface IMaterialProperties
{
    string Name { get; set; }
    double Density { get; set; }
}

/// <summary>
/// Isotropic material properties
/// </summary>
public interface IIsotropicMaterial : IMaterialProperties
{
    double YoungModulus { get; set; }
    double PoissonRatio { get; set; }
    
    /// <summary>
    /// Shear modulus (calculated)
    /// </summary>
    double ShearModulus { get; }
}

/// <summary>
/// Transversely isotropic material properties
/// </summary>
public interface ITransverselyIsotropicMaterial : IMaterialProperties
{
    double E1 { get; set; }  // Young's modulus in plane
    double E2 { get; set; }  // Young's modulus perpendicular to plane
    double Nu12 { get; set; } // Poisson's ratio in plane
    double Nu23 { get; set; } // Poisson's ratio out of plane
    double G12 { get; set; }  // Shear modulus
}

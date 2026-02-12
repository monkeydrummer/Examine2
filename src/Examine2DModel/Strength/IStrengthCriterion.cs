namespace Examine2DModel.Strength;

/// <summary>
/// Base interface for strength criteria
/// </summary>
public interface IStrengthCriterion
{
    string Name { get; }
    
    /// <summary>
    /// Calculate strength factor at given stress state
    /// </summary>
    double CalculateStrengthFactor(double sigma1, double sigma3);
    
    /// <summary>
    /// Check if stress state is in failure
    /// </summary>
    bool IsFailure(double sigma1, double sigma3);
}

/// <summary>
/// Mohr-Coulomb strength criterion
/// </summary>
public interface IMohrCoulombCriterion : IStrengthCriterion
{
    double Cohesion { get; set; }
    double FrictionAngle { get; set; } // in degrees
}

/// <summary>
/// Generalized Hoek-Brown strength criterion
/// </summary>
public interface IHoekBrownCriterion : IStrengthCriterion
{
    double Mb { get; set; }
    double S { get; set; }
    double A { get; set; }
    double Sci { get; set; } // Uniaxial compressive strength
    
    /// <summary>
    /// Alternative: Calculate from GSI, mi, D
    /// </summary>
    void SetFromGSI(double gsi, double mi, double disturbanceFactor);
}

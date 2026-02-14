namespace Examine2DModel.Calculations;

/// <summary>
/// Calculator for strain computations from stress and elastic properties
/// Ports logic from get_analysis_data() lines 273-285
/// </summary>
public static class StrainCalculator
{
    /// <summary>
    /// Calculate principal strains from principal stresses (plane strain analysis)
    /// Ported from get_analysis_data() lines 67-78
    /// </summary>
    /// <param name="sigma1">Major in-plane principal stress</param>
    /// <param name="sigma3">Minor in-plane principal stress</param>
    /// <param name="youngModulus">Young's modulus (E)</param>
    /// <param name="poissonRatio">Poisson's ratio (ν)</param>
    /// <returns>Tuple of (epsilon1, epsilon3)</returns>
    public static (double Epsilon1, double Epsilon3) CalculatePrincipalStrains(
        double sigma1, double sigma3,
        double youngModulus, double poissonRatio)
    {
        // For plane strain: εᵢ = (1/E)[(1-ν²)σᵢ - ν(1+ν)σⱼ]
        double e = youngModulus;
        double pr = poissonRatio;
        
        double epsilon1 = (1.0 / e) * (((1.0 - pr * pr) * sigma1) - (pr * (1.0 + pr) * sigma3));
        double epsilon3 = (1.0 / e) * (((1.0 - pr * pr) * sigma3) - (pr * (1.0 + pr) * sigma1));
        
        return (epsilon1, epsilon3);
    }
    
    /// <summary>
    /// Calculate strains in x-y coordinates from stresses
    /// Ported from get_analysis_data() lines 69-76
    /// </summary>
    public static (double EpsilonX, double EpsilonY, double GammaXY) CalculateCartesianStrains(
        double sigmaX, double sigmaY, double tauXY,
        double youngModulus, double poissonRatio)
    {
        double e = youngModulus;
        double pr = poissonRatio;
        double g = e / (2.0 * (1.0 + pr)); // Shear modulus
        
        // Normal strains (plane strain)
        double epsilonX = (1.0 / e) * (((1.0 - pr * pr) * sigmaX) - (pr * (1.0 + pr) * sigmaY));
        double epsilonY = (1.0 / e) * (((1.0 - pr * pr) * sigmaY) - (pr * (1.0 + pr) * sigmaX));
        
        // Shear strain
        double gammaXY = (1.0 / g) * tauXY;
        
        return (epsilonX, epsilonY, gammaXY);
    }
    
    /// <summary>
    /// Calculate volumetric strain (change in volume per unit volume)
    /// Ported from get_analysis_data() line 277
    /// </summary>
    public static double CalculateVolumetricStrain(double epsilon1, double epsilon3)
    {
        return epsilon1 + epsilon3;
    }
    
    /// <summary>
    /// Calculate maximum shear strain (engineering shear strain)
    /// Ported from get_analysis_data() line 284
    /// </summary>
    public static double CalculateShearStrain(double epsilon1, double epsilon3)
    {
        return epsilon1 - epsilon3;
    }
    
    /// <summary>
    /// Calculate shear modulus from Young's modulus and Poisson's ratio
    /// G = E / (2(1+ν))
    /// </summary>
    public static double CalculateShearModulus(double youngModulus, double poissonRatio)
    {
        return youngModulus / (2.0 * (1.0 + poissonRatio));
    }
}

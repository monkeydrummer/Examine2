namespace Examine2DModel.Calculations;

/// <summary>
/// Calculator for stress-related computations
/// Ports logic from BCompute2D::invariants(), sort_principal_stresses(), and get_analysis_data()
/// </summary>
public static class StressCalculator
{
    private const double Pi = Math.PI;
    private const double Epsilon = 1e-10;
    
    /// <summary>
    /// Calculate stress invariants (I1, J2, Lode angle) from stress tensor components
    /// Ported from BCompute2D::invariants() line 6343
    /// </summary>
    /// <param name="sigmaX">X-direction stress</param>
    /// <param name="sigmaY">Y-direction stress</param>
    /// <param name="sigmaZ">Z-direction stress</param>
    /// <param name="tauXY">XY shear stress</param>
    /// <param name="tauYZ">YZ shear stress</param>
    /// <param name="tauXZ">XZ shear stress</param>
    /// <returns>Tuple of (I1, J2, LodeAngle)</returns>
    public static (double I1, double J2, double LodeAngle) CalculateInvariants(
        double sigmaX, double sigmaY, double sigmaZ,
        double tauXY, double tauYZ, double tauXZ)
    {
        // First invariant: sum of normal stresses
        double i1 = sigmaX + sigmaY + sigmaZ;
        double p = i1 / 3.0; // Mean stress
        
        // Second deviatoric stress invariant
        double j2 = ((sigmaX - sigmaY) * (sigmaX - sigmaY) + 
                     (sigmaY - sigmaZ) * (sigmaY - sigmaZ) + 
                     (sigmaZ - sigmaX) * (sigmaZ - sigmaX)) / 6.0
                    + tauXY * tauXY + tauYZ * tauYZ + tauXZ * tauXZ;
        
        // Third deviatoric stress invariant
        double j3 = (sigmaX - p) * (sigmaY - p) * (sigmaZ - p) 
                    + 2.0 * tauXY * tauYZ * tauXZ
                    - (sigmaX - p) * tauYZ * tauYZ
                    - (sigmaY - p) * tauXZ * tauXZ
                    - (sigmaZ - p) * tauXY * tauXY;
        
        // Lode angle (in radians, range: -π/6 to π/6)
        double lodeAngle = 0.0;
        if (j2 >= Epsilon)
        {
            double sqrtJ2 = Math.Sqrt(j2);
            double arg = -1.5 * Math.Sqrt(3.0) * j3 / Math.Pow(sqrtJ2, 3.0);
            
            // Clamp argument to valid range for asin
            if (Math.Abs(arg) > 1.0)
            {
                arg = Math.Sign(arg) * 1.0;
            }
            
            lodeAngle = Math.Asin(arg) / 3.0;
        }
        
        return (i1, j2, lodeAngle);
    }
    
    /// <summary>
    /// Calculate 3D principal stresses from stress tensor
    /// Returns stresses sorted as (sigma3, sigma2, sigma1) where sigma1 >= sigma2 >= sigma3
    /// Ported from BCompute2D::sort_principal_stresses() line 6328
    /// </summary>
    public static (double Sigma3, double Sigma2, double Sigma1) CalculatePrincipalStresses3D(
        double sigmaX, double sigmaY, double sigmaZ,
        double tauXY, double tauYZ, double tauXZ)
    {
        double sigma1, sigma2, sigma3;
        
        // For the simple case where shears are zero or negligible, principal stresses are the normal stresses
        if (Math.Abs(tauXY) < Epsilon && Math.Abs(tauYZ) < Epsilon && Math.Abs(tauXZ) < Epsilon)
        {
            sigma1 = sigmaX;
            sigma2 = sigmaY;
            sigma3 = sigmaZ;
        }
        else
        {
            // For general case, calculate invariants and use them to find principal stresses
            var (i1, j2, lodeAngle) = CalculateInvariants(sigmaX, sigmaY, sigmaZ, tauXY, tauYZ, tauXZ);
            
            double p = i1 / 3.0;
            double sqrtJ2 = Math.Sqrt(j2);
            
            // Calculate principal stresses using invariants
            // These formulas give principal stresses directly from invariants
            sigma1 = p + 2.0 * sqrtJ2 * Math.Sin(lodeAngle + 2.0 * Pi / 3.0);
            sigma2 = p + 2.0 * sqrtJ2 * Math.Sin(lodeAngle);
            sigma3 = p + 2.0 * sqrtJ2 * Math.Sin(lodeAngle - 2.0 * Pi / 3.0);
        }
        
        // Sort to ensure sigma1 >= sigma2 >= sigma3
        SortPrincipalStresses(ref sigma3, ref sigma2, ref sigma1);
        
        return (sigma3, sigma2, sigma1);
    }
    
    /// <summary>
    /// Calculate 2D in-plane principal stresses from plane stress components
    /// Ported from BCCOMPUTE2D_FIELD_POINT_RESULTS::get_principal_stresses() line 354
    /// </summary>
    /// <returns>Tuple of (Sigma3, Sigma1, Angle) where angle is in degrees from x-axis</returns>
    public static (double Sigma3, double Sigma1, double AngleDegrees) CalculatePrincipalStresses2D(
        double sigmaX, double sigmaY, double tauXY)
    {
        double avgStress = (sigmaX + sigmaY) / 2.0;
        double tauMax = Math.Sqrt(((sigmaX - sigmaY) * (sigmaX - sigmaY) / 4.0) + (tauXY * tauXY));
        
        double sigma1 = avgStress + tauMax;
        double sigma3 = avgStress - tauMax;
        
        // Calculate angle
        double angle = 0.0;
        if ((sigma1 - sigma3) >= 0.01 * (Math.Abs(sigma1) + Math.Abs(sigma3)))
        {
            angle = Math.Atan2(sigma1 - sigmaX, tauXY) * 180.0 / Pi;
            if (angle < 0.0) angle += 180.0;
        }
        
        return (sigma3, sigma1, angle);
    }
    
    /// <summary>
    /// Sort three values so that min <= intermediate <= max
    /// Ported from BCompute2D::sort_principal_stresses() line 6328
    /// </summary>
    private static void SortPrincipalStresses(ref double min, ref double intermediate, ref double max)
    {
        double s1 = max, s2 = intermediate, s3 = min;
        
        // All 6 possible orderings
        if (min < max && max < intermediate) { s3 = min; s2 = max; s1 = intermediate; }
        else if (intermediate < min && min < max) { s3 = intermediate; s2 = min; s1 = max; }
        else if (intermediate < max && max < min) { s3 = intermediate; s2 = max; s1 = min; }
        else if (max < min && min < intermediate) { s3 = max; s2 = min; s1 = intermediate; }
        else if (max < intermediate && intermediate < min) { s3 = max; s2 = intermediate; s1 = min; }
        // else already sorted (min <= intermediate <= max)
        
        max = s1;
        intermediate = s2;
        min = s3;
    }
    
    /// <summary>
    /// Calculate von Mises stress from J2 invariant
    /// Ported from get_analysis_data() line 231
    /// </summary>
    public static double CalculateVonMisesStress(double j2)
    {
        return Math.Sqrt(3.0 * j2);
    }
    
    /// <summary>
    /// Calculate mean stress from I1 invariant
    /// Ported from get_analysis_data() line 226
    /// </summary>
    public static double CalculateMeanStress(double i1)
    {
        return i1 / 3.0;
    }
    
    /// <summary>
    /// Calculate deviatoric stress (maximum shear stress intensity)
    /// Ported from get_analysis_data() line 228
    /// </summary>
    public static double CalculateDeviatoricStress(double sigma1, double sigma3)
    {
        return sigma1 - sigma3;
    }
    
    /// <summary>
    /// Calculate Angelier stress ratio
    /// Ported from get_analysis_data() line 234
    /// </summary>
    public static double CalculateAngelierStressRatio(double sigma1, double sigma2, double sigma3)
    {
        if (Math.Abs(sigma1 - sigma3) < Epsilon) return 0.0;
        
        double ratio = (sigma2 - sigma3) / (sigma1 - sigma3);
        
        // Clamp to [0, 1]
        if (ratio > 1.0) ratio = 1.0;
        if (ratio < 0.0) ratio = 0.0;
        
        return ratio;
    }
    
    /// <summary>
    /// Calculate stress ratio K (horizontal to vertical stress ratio)
    /// Ported from get_analysis_data() line 302
    /// </summary>
    public static double CalculateStressRatio(double sigma1, double sigma3)
    {
        if (Math.Abs(sigma1) > Epsilon)
        {
            return sigma3 / sigma1;
        }
        return 100.0; // Default for near-zero stress
    }
    
    /// <summary>
    /// Calculate total displacement magnitude
    /// Ported from get_analysis_data() line 262
    /// </summary>
    public static double CalculateTotalDisplacement(double ux, double uy, double uz)
    {
        return Math.Sqrt(ux * ux + uy * uy + uz * uz);
    }
}

namespace Examine2DModel.Strength;

/// <summary>
/// Mohr-Coulomb failure criterion
/// Ported from BCompute2D::strength_factor() case 1 (lines 7477-7495)
/// </summary>
public class MohrCoulombCriterion : IMohrCoulombCriterion
{
    private const double RadiansPerDegree = Math.PI / 180.0;
    private const double Epsilon = 1e-12;
    
    /// <summary>
    /// Cohesion (c) in stress units
    /// </summary>
    public double Cohesion { get; set; }
    
    /// <summary>
    /// Friction angle (φ) in degrees
    /// </summary>
    public double FrictionAngle { get; set; }
    
    /// <summary>
    /// Tensile strength (T0) in stress units
    /// Negative value indicates tensile stress limit
    /// </summary>
    public double TensileStrength { get; set; }
    
    public string Name => "Mohr-Coulomb";
    
    public MohrCoulombCriterion()
    {
        Cohesion = 0.0;
        FrictionAngle = 30.0; // Default 30 degrees
        TensileStrength = 0.0;
    }
    
    public MohrCoulombCriterion(double cohesion, double frictionAngleDegrees, double tensileStrength = 0.0)
    {
        Cohesion = cohesion;
        FrictionAngle = frictionAngleDegrees;
        TensileStrength = tensileStrength;
    }
    
    /// <summary>
    /// Calculate strength factor using stress invariants and minimum principal stress
    /// Ported from BCompute2D::strength_factor() case 1 (lines 7477-7495)
    /// </summary>
    /// <param name="i1">First stress invariant (I1 = σ1 + σ2 + σ3)</param>
    /// <param name="rtJ2">Square root of second deviatoric invariant (√J2)</param>
    /// <param name="lode">Lode angle in radians</param>
    /// <param name="sigmaMin">Minimum principal stress (σ3)</param>
    /// <returns>Strength factor (>1.0 = safe, <1.0 = failure, -1.0 = tensile failure)</returns>
    public double CalculateStrengthFactorFromInvariants(double i1, double rtJ2, double lode, double sigmaMin)
    {
        // Check for tensile failure (C++ line 7479-7482)
        // sigmin < -|T0| means tension exceeds tensile strength
        if (sigmaMin < -Math.Abs(TensileStrength))
        {
            return -1.0; // Tensile failure
        }
        
        // If stress state is negligible, return high safety factor
        if (rtJ2 <= Epsilon)
        {
            return 100.0;
        }
        
        // Clamp Lode angle to valid range [-π/6, π/6]
        if (lode > Math.PI / 6.0)
            lode = Math.PI / 6.0;
        else if (lode < -Math.PI / 6.0)
            lode = -Math.PI / 6.0;
        
        // Calculate strength factor (C++ lines 7485-7490)
        double angle = FrictionAngle * RadiansPerDegree;
        double sinAngle = Math.Sin(angle);
        double cosAngle = Math.Cos(angle);
        
        // Mohr-Coulomb criterion in terms of invariants:
        // SF = (c·cos(φ) + I1·sin(φ)/3) / [(cos(θ) + sin(φ)·sin(θ)/√3) · √J2]
        double numerator = Cohesion * cosAngle + i1 * sinAngle / 3.0;
        double denominator = (Math.Cos(lode) + sinAngle * Math.Sin(lode) / Math.Sqrt(3.0)) * rtJ2;
        
        double strengthFactor = numerator / denominator;
        
        // Cap at 100.0 (C++ line 7492)
        if (strengthFactor > 100.0)
            strengthFactor = 100.0;
        
        return strengthFactor;
    }
    
    /// <summary>
    /// Calculate strength factor from principal stresses (simplified interface)
    /// </summary>
    public double CalculateStrengthFactor(double sigma1, double sigma3)
    {
        // For 2D plane strain, sigma2 = (sigma1 + sigma3)/2 (common approximation)
        // or use out-of-plane stress calculation
        double sigma2 = (sigma1 + sigma3) / 2.0;
        
        // Calculate invariants
        var (i1, j2, lodeAngle) = Calculations.StressCalculator.CalculateInvariants(
            sigma1, sigma2, sigma3, 0, 0, 0);
        
        double rtJ2 = Math.Sqrt(j2);
        
        return CalculateStrengthFactorFromInvariants(i1, rtJ2, lodeAngle, sigma3);
    }
    
    /// <summary>
    /// Check if stress state represents failure
    /// </summary>
    public bool IsFailure(double sigma1, double sigma3)
    {
        double sf = CalculateStrengthFactor(sigma1, sigma3);
        return sf < 1.0;
    }
    
    /// <summary>
    /// Calculate failure stress for given confining pressure using Mohr-Coulomb equation
    /// σ1f = σ3 · tan²(45° + φ/2) + 2c · tan(45° + φ/2)
    /// </summary>
    public double CalculateFailureStress(double sigma3)
    {
        double angle = FrictionAngle * RadiansPerDegree;
        double term = Math.Tan(Math.PI / 4.0 + angle / 2.0);
        
        return sigma3 * term * term + 2.0 * Cohesion * term;
    }
    
    public override string ToString()
    {
        return $"Mohr-Coulomb: c={Cohesion:F2}, φ={FrictionAngle:F1}°, T0={TensileStrength:F2}";
    }
}

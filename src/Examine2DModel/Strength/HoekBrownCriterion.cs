namespace Examine2DModel.Strength;

/// <summary>
/// Original Hoek-Brown failure criterion (1980)
/// Ported from BCompute2D::strength_factor() case 2 (lines 7497-7517)
/// </summary>
public class HoekBrownCriterion : IStrengthCriterion
{
    private const double Epsilon = 1e-12;
    
    /// <summary>
    /// Material constant m
    /// </summary>
    public double M { get; set; }
    
    /// <summary>
    /// Material constant s (typically 1.0 for intact rock, 0 for highly fractured)
    /// </summary>
    public double S { get; set; }
    
    /// <summary>
    /// Uniaxial compressive strength (σci) in stress units
    /// </summary>
    public double Sci { get; set; }
    
    public string Name => "Hoek-Brown (Original)";
    
    public HoekBrownCriterion()
    {
        M = 10.0; // Typical value for intact rock
        S = 1.0;  // Intact rock
        Sci = 50.0; // Default UCS
    }
    
    public HoekBrownCriterion(double m, double s, double sci)
    {
        M = m;
        S = s;
        Sci = sci;
    }
    
    /// <summary>
    /// Calculate strength factor using stress invariants
    /// Ported from BCompute2D::strength_factor() case 2 (lines 7497-7517)
    /// </summary>
    public double CalculateStrengthFactorFromInvariants(double i1, double rtJ2, double lode, double sigmaMin)
    {
        // Check for tensile failure (C++ line 7499-7502)
        // Original H-B: tensile strength = s·σci/m
        double tensileLimit = -S * Sci / M;
        if (sigmaMin < tensileLimit)
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
        
        // Calculate strength factor using original Hoek-Brown formulation (C++ lines 7504-7511)
        double mScOn8 = M * Sci / 8.0;
        double temp = 1.0 + Math.Tan(lode) / Math.Sqrt(3.0);
        double cosLode = Math.Cos(lode);
        
        // Original H-B equation in invariant form:
        // SF = [√(m·σci/8 · (m·σci/8 · temp² + 2·I1/3) + s·σci²/4) - m·σci/8 · temp] / (cos(θ)·√J2)
        double underSqrt = mScOn8 * (mScOn8 * temp * temp + 2.0 * i1 / 3.0) + S * Sci * Sci / 4.0;
        double numerator = Math.Sqrt(underSqrt) - mScOn8 * temp;
        double denominator = cosLode * rtJ2;
        
        double strengthFactor = numerator / denominator;
        
        // Cap at 100.0 (C++ line 7514)
        if (strengthFactor > 100.0)
            strengthFactor = 100.0;
        
        return strengthFactor;
    }
    
    /// <summary>
    /// Calculate strength factor from principal stresses (simplified interface)
    /// </summary>
    public double CalculateStrengthFactor(double sigma1, double sigma3)
    {
        // For 2D plane strain, approximate sigma2
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
    /// Calculate failure stress for given confining pressure using original H-B equation
    /// σ1 = σ3 + σci·√(m·σ3/σci + s)
    /// </summary>
    public double CalculateFailureStress(double sigma3)
    {
        double term = M * sigma3 / Sci + S;
        if (term < 0)
            return sigma3; // Cannot take sqrt of negative
        
        return sigma3 + Sci * Math.Sqrt(term);
    }
    
    /// <summary>
    /// Calculate tensile strength
    /// σt = -s·σci/m
    /// </summary>
    public double GetTensileStrength()
    {
        return -S * Sci / M;
    }
    
    public override string ToString()
    {
        return $"Hoek-Brown (Original): m={M:F1}, s={S:F2}, σci={Sci:F2}";
    }
}

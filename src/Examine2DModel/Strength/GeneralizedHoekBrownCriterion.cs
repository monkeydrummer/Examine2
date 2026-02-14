namespace Examine2DModel.Strength;

/// <summary>
/// Generalized Hoek-Brown failure criterion (2002)
/// Ported from BCompute2D::strength_factor() case 3 (lines 7518-7532) and helper functions
/// </summary>
public class GeneralizedHoekBrownCriterion : IHoekBrownCriterion
{
    private const double Epsilon = 1e-12;
    private const double Pi = Math.PI;
    
    // Hoek-Brown factors (from C++ code)
    // hoek_factor1 and hoek_factor2 are precomputed constants
    private static readonly double HoekFactor1 = Math.Pow(3.0, -0.5); // 1/√3
    private static readonly double HoekFactor2 = Math.Pow(3.0, 0.5);  // √3
    
    /// <summary>
    /// Material constant mb
    /// </summary>
    public double Mb { get; set; }
    
    /// <summary>
    /// Material constant s
    /// </summary>
    public double S { get; set; }
    
    /// <summary>
    /// Material constant a
    /// </summary>
    public double A { get; set; }
    
    /// <summary>
    /// Uniaxial compressive strength (σci) in stress units
    /// </summary>
    public double Sci { get; set; }
    
    public string Name => "Hoek-Brown (Generalized)";
    
    public GeneralizedHoekBrownCriterion()
    {
        Mb = 1.0;
        S = 0.001;
        A = 0.5;
        Sci = 50.0;
    }
    
    public GeneralizedHoekBrownCriterion(double mb, double s, double a, double sci)
    {
        Mb = mb;
        S = s;
        A = a;
        Sci = sci;
    }
    
    /// <summary>
    /// Set parameters from GSI (Geological Strength Index), mi, and disturbance factor
    /// Standard Hoek-Brown equations for rock mass properties
    /// </summary>
    public void SetFromGSI(double gsi, double mi, double disturbanceFactor)
    {
        // GSI: 0-100 (Geological Strength Index)
        // mi: intact rock material constant (typically 4-35)
        // D: disturbance factor (0 = undisturbed, 1 = very disturbed)
        
        // Hoek-Brown 2002 equations
        Mb = mi * Math.Exp((gsi - 100.0) / (28.0 - 14.0 * disturbanceFactor));
        S = Math.Exp((gsi - 100.0) / (9.0 - 3.0 * disturbanceFactor));
        
        // Parameter a
        if (gsi > 25.0)
        {
            A = 0.5;
        }
        else
        {
            A = 0.65 - gsi / 200.0;
        }
    }
    
    /// <summary>
    /// Calculate strength factor using stress invariants with root-finding
    /// Ported from BCompute2D::strength_factor() case 3 (lines 7518-7532)
    /// </summary>
    public double CalculateStrengthFactorFromInvariants(double i1, double rtJ2, double lode, double sigmaMin)
    {
        // If stress state is negligible, return high safety factor
        if (rtJ2 < Epsilon)
        {
            rtJ2 = Epsilon; // C++ line 7527-7528
        }
        
        // Clamp Lode angle to valid range [-π/6, π/6]
        if (lode > Pi / 6.0)
            lode = Pi / 6.0;
        else if (lode < -Pi / 6.0)
            lode = -Pi / 6.0;
        
        // Find root using bracketing and bisection (C++ lines 7529-7531)
        double xl = 0.0001;
        double xr = 1.0;
        
        // Bracket the root
        bool bracketed = BracketRoot(i1, lode, xl, xr, out double x1, out double x2);
        
        if (!bracketed)
        {
            // If root finding fails, return a safe value
            return 100.0;
        }
        
        // Find root using bisection
        double root = BisectionRoot(i1, lode, x1, x2, 1.0e-5);
        
        // Calculate strength factor
        double strengthFactor = root / rtJ2;
        
        // Cap at reasonable maximum
        if (strengthFactor > 100.0)
            strengthFactor = 100.0;
        
        return strengthFactor;
    }
    
    /// <summary>
    /// Root function for generalized Hoek-Brown
    /// Ported from BCompute2D::rootF() line 7541
    /// </summary>
    private double RootFunction(double i1, double lode, double str)
    {
        // F(str) = I1·mb/3·√(1/3) + s·√3 - (2·str·cos(θ))^(1/a) + mb·str·√(1/3)·(-cos(θ) - sin(θ)/√3)
        
        double term1 = 0.33333333333333333333333333333333 * i1 * Mb * HoekFactor1;
        double term2 = S * HoekFactor2;
        double term3 = -Math.Pow(2.0 * str * Math.Cos(lode), 1.0 / A);
        double term4 = Mb * str * HoekFactor1 * (-Math.Cos(lode) - Math.Sin(lode) / 1.7320508075688772935274463415059);
        
        return term1 + term2 + term3 + term4;
    }
    
    /// <summary>
    /// Bracket the root by expanding the search range
    /// Ported from BCompute2D::zbracrootF() lines 7549-7571
    /// </summary>
    private bool BracketRoot(double i1, double lode, double x1In, double x2In, out double x1, out double x2)
    {
        const double Factor = 1.6;
        const int MaxTries = 50;
        
        x1 = x1In;
        x2 = x2In;
        
        double f1 = RootFunction(i1, lode, x1);
        double f2 = RootFunction(i1, lode, x2);
        
        for (int j = 0; j < MaxTries; j++)
        {
            // Check if root is bracketed (function changes sign)
            if (f1 * f2 < 0.0)
            {
                return true; // Successfully bracketed
            }
            
            // Expand the range
            if (Math.Abs(f1) < Math.Abs(f2))
            {
                x1 += Factor * (x1 - x2);
                f1 = RootFunction(i1, lode, x1);
            }
            else
            {
                x2 += Factor * (x2 - x1);
                f2 = RootFunction(i1, lode, x2);
            }
        }
        
        return false; // Failed to bracket
    }
    
    /// <summary>
    /// Find root using bisection method
    /// Ported from BCompute2D::rtbisrootF() lines 7573-7592
    /// </summary>
    private double BisectionRoot(double i1, double lode, double x1, double x2, double accuracy)
    {
        const int MaxIterations = 100;
        
        double f = RootFunction(i1, lode, x1);
        double fmid = RootFunction(i1, lode, x2);
        
        // Check if root is bracketed
        if (f * fmid >= 0.0)
        {
            return 0.0; // Should not happen if BracketRoot succeeded
        }
        
        // Initialize bisection
        double dx;
        double rtb;
        
        if (f < 0.0)
        {
            dx = x2 - x1;
            rtb = x1;
        }
        else
        {
            dx = x1 - x2;
            rtb = x2;
        }
        
        // Bisection loop
        for (int j = 0; j < MaxIterations; j++)
        {
            dx *= 0.5;
            double xmid = rtb + dx;
            fmid = RootFunction(i1, lode, xmid);
            
            if (fmid <= 0.0)
            {
                rtb = xmid;
            }
            
            // Check convergence
            if (Math.Abs(dx) < accuracy || fmid == 0.0)
            {
                return rtb;
            }
        }
        
        return rtb; // Return best estimate
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
    /// Calculate failure stress for given confining pressure using generalized H-B equation
    /// σ1 = σ3 + σci·(mb·σ3/σci + s)^a
    /// </summary>
    public double CalculateFailureStress(double sigma3)
    {
        double term = Mb * sigma3 / Sci + S;
        if (term < 0)
            return sigma3; // Cannot take power of negative
        
        return sigma3 + Sci * Math.Pow(term, A);
    }
    
    /// <summary>
    /// Calculate tensile strength from generalized H-B
    /// Found by setting σ1 = 0 in failure criterion
    /// </summary>
    public double GetTensileStrength()
    {
        // Approximate solution: σt ≈ -s·σci/(2·mb) for small s
        // More accurate requires numerical solution
        return -S * Sci / (2.0 * Mb);
    }
    
    public override string ToString()
    {
        return $"Hoek-Brown (Generalized): mb={Mb:F2}, s={S:F4}, a={A:F2}, σci={Sci:F2}";
    }
}

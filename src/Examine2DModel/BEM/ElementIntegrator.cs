using CAD2DModel.Geometry;
using Examine2DModel.Materials;

namespace Examine2DModel.BEM;

/// <summary>
/// Handles numerical integration of boundary element influence functions
/// Ports integration logic from bcompute2d.cpp (coffsimg, fskel, fshlf, hlfspc)
/// Performance optimized with pre-computed Gaussian quadrature
/// </summary>
public class ElementIntegrator
{
    private const double PI = Math.PI;
    private const double EPSILON = 1e-8; // Tolerance for geometric comparisons
    private const double GEOMETRY_EPS = 1e-4;
    
    private readonly IIsotropicMaterial _material;
    private readonly double _kappa; // Material constant (3 - 4*nu) for plane strain
    private readonly double _kappaPlus1; // kappa + 1
    private readonly double _kappaMinus1; // kappa - 1
    private readonly double _shearModulus;
    private readonly double _displacementCoefficient; // 1/(8*pi*G*(1-nu))
    private readonly double _stressCoefficient; // G/(2*pi*(1-nu))

    /// <summary>
    /// Represents influence coefficients for displacements and stresses
    /// </summary>
    public class InfluenceCoefficients
    {
        // Displacement influence coefficients
        public double UxFromNormal { get; set; }    // st11[0] - displacement in x from normal traction
        public double UxFromShear { get; set; }     // st21[0] - displacement in x from shear traction
        public double UyFromNormal { get; set; }    // st11[1] - displacement in y from normal traction
        public double UyFromShear { get; set; }     // st21[1] - displacement in y from shear traction
        
        // Stress influence coefficients
        public double SxxFromNormal { get; set; }   // st11[2] - sigma_xx from normal traction
        public double SxxFromShear { get; set; }    // st21[2] - sigma_xx from shear traction
        public double SyyFromNormal { get; set; }   // st11[3] - sigma_yy from normal traction
        public double SyyFromShear { get; set; }    // st21[3] - sigma_yy from shear traction
        public double SxyFromNormal { get; set; }   // st11[4] - tau_xy from normal traction
        public double SxyFromShear { get; set; }    // st21[4] - tau_xy from shear traction
    }

    /// <summary>
    /// Element shape function coefficients for linear/quadratic elements
    /// </summary>
    public class ShapeFunctionCoefficients
    {
        public double[] LeftNode { get; set; } = new double[3];  // Coefficients for left node
        public double[] MiddleNode { get; set; } = new double[3]; // Coefficients for middle node (quadratic only)
        public double[] RightNode { get; set; } = new double[3];  // Coefficients for right node
    }

    public ElementIntegrator(IIsotropicMaterial material)
    {
        _material = material ?? throw new ArgumentNullException(nameof(material));
        _shearModulus = material.ShearModulus;
        
        // Calculate material constants for plane strain
        // kappa = 3 - 4*nu
        _kappa = 3.0 - 4.0 * material.PoissonRatio;
        _kappaPlus1 = _kappa + 1.0;
        _kappaMinus1 = _kappa - 1.0;
        
        // Coefficients matching C++ code exactly (lines 1045-1046)
        // strcof = 1/(8*pi*(1-nu))
        _stressCoefficient = 1.0 / (8.0 * PI * (1.0 - material.PoissonRatio));
        
        // dspcof = strcof * 2*(1+nu)/E = (1+nu) / (4*pi*E*(1-nu))
        _displacementCoefficient = _stressCoefficient * 2.0 * (1.0 + material.PoissonRatio) / material.YoungModulus;
    }

    /// <summary>
    /// Compute influence of a boundary element on a field point
    /// This is the main entry point for integration (ports coffsimg from C++)
    /// </summary>
    /// <param name="fieldPoint">Field point location</param>
    /// <param name="element">Boundary element</param>
    /// <param name="groundSurfaceY">Y-coordinate of ground surface (for half-space solution)</param>
    /// <param name="isHalfSpace">True for half-space problem, false for full-space</param>
    /// <returns>Influence coefficients in GLOBAL coordinates</returns>
    public InfluenceCoefficients ComputeInfluence(Point2D fieldPoint, BoundaryElement element, 
        double groundSurfaceY = 0.0, bool isHalfSpace = true)
    {
        var coeffs = new InfluenceCoefficients();
        
        // Transform field point to local element coordinate system
        // Element x-axis along element direction, y-axis normal to element
        double dx = fieldPoint.X - element.MidPoint.X;
        double dy = fieldPoint.Y - element.MidPoint.Y;
        
        double cost = element.CosineDirection; // cos(theta) - element angle
        double sint = element.SineDirection;   // sin(theta)
        
        // Local coordinates: x' along element, y' perpendicular
        double xLocal = dx * cost + dy * sint;
        double yLocal = dy * cost - dx * sint;
        
        // Half-length of element
        double halfLength = element.Length / 2.0;
        
        // Get adaptive quadrature based on distance
        var quadrature = GaussianQuadrature.GetQuadrature(
            fieldPoint.X, fieldPoint.Y,
            element.MidPoint.X, element.MidPoint.Y,
            element.Length);
        
        // Arrays for local coordinate influence [order][component]
        double[,] st11 = new double[3, 5]; // Normal traction influence
        double[,] st21 = new double[3, 5]; // Shear traction influence
        
        if (isHalfSpace)
        {
            // Half-space solution (with ground surface)
            ComputeHalfSpaceInfluence(fieldPoint, element, groundSurfaceY, 
                cost, sint, xLocal, yLocal, halfLength, quadrature, st11, st21);
        }
        else
        {
            // Full-space Kelvin solution (infinite domain)
            ComputeFullSpaceInfluence(xLocal, yLocal, halfLength, quadrature, st11, st21);
        }
        
        // Transform from local element coordinates back to global coordinates
        // This matches the transformation in coffsimg (lines 2146-2163)
        int k = 0; // Constant elements for now
        
        // Displacements (transform from local to global)
        double uxFromNormal = st11[k, 0] * cost - st11[k, 1] * sint;
        double uxFromShear = st21[k, 0] * cost - st21[k, 1] * sint;
        double uyFromNormal = st11[k, 0] * sint + st11[k, 1] * cost;
        double uyFromShear = st21[k, 0] * sint + st21[k, 1] * cost;
        
        // Stresses (transform from local to global)
        double sxxFromNormal = st11[k, 2] * cost * cost + st11[k, 3] * sint * sint 
            - 2.0 * st11[k, 4] * sint * cost;
        double sxxFromShear = st21[k, 2] * cost * cost + st21[k, 3] * sint * sint 
            - 2.0 * st21[k, 4] * sint * cost;
            
        double syyFromNormal = st11[k, 2] * sint * sint + st11[k, 3] * cost * cost 
            + 2.0 * st11[k, 4] * sint * cost;
        double syyFromShear = st21[k, 2] * sint * sint + st21[k, 3] * cost * cost 
            + 2.0 * st21[k, 4] * sint * cost;
            
        double sxyFromNormal = sint * cost * (st11[k, 2] - st11[k, 3]) 
            + st11[k, 4] * (cost * cost - sint * sint);
        double sxyFromShear = sint * cost * (st21[k, 2] - st21[k, 3]) 
            + st21[k, 4] * (cost * cost - sint * sint);
        
        // Store in output structure (now in global coordinates)
        coeffs.UxFromNormal = uxFromNormal;
        coeffs.UyFromNormal = uyFromNormal;
        coeffs.SxxFromNormal = sxxFromNormal;
        coeffs.SyyFromNormal = syyFromNormal;
        coeffs.SxyFromNormal = sxyFromNormal;
        
        coeffs.UxFromShear = uxFromShear;
        coeffs.UyFromShear = uyFromShear;
        coeffs.SxxFromShear = sxxFromShear;
        coeffs.SyyFromShear = syyFromShear;
        coeffs.SxyFromShear = sxyFromShear;
        
        return coeffs;
    }

    /// <summary>
    /// Compute full-space (Kelvin) influence coefficients using CLOSED-FORM analytical integration
    /// Ports coffsobj() from C++ (lines 1872-1982) - exact analytical formulas
    /// This is MORE ACCURATE than Gaussian quadrature
    /// </summary>
    private void ComputeFullSpaceInfluence(double xLocal, double yLocal, double halfLength,
        GaussianQuadrature.QuadratureData quadrature, double[,] st11, double[,] st21)
    {
        // CLOSED-FORM ANALYTICAL INTEGRATION (matching C++ coffsobj exactly)
        // This integrates the Kelvin fundamental solution from -halfLength to +halfLength
        
        double ymp = yLocal;  // Distance perpendicular to element
        double xmp = xLocal;  // Distance along element direction
        double dl = halfLength;
        
        // Element endpoints in local coordinates
        double xmat = xmp - dl;  // Left endpoint
        double xmab = xmp + dl;  // Right endpoint
        
        double ymp2 = ymp * ymp;
        double r2t = xmat * xmat + ymp2;  // Distance squared to left endpoint
        double r2b = xmab * xmab + ymp2;  // Distance squared to right endpoint
        
        // Check for singularity (field point on or very close to element)
        if (r2t < 1e-8 || r2b < 1e-8)
        {
            // Singular case - return zeros (C++ returns 0 from function)
            // The arrays are already initialized to zero, so just return
            return;
        }
        
        // Compute angular integral (difference of arctangents)
        double ttd;
        if (Math.Abs(ymp) <= 1e-4)
        {
            // Field point is on the element line - use limiting formula
            ttd = (Sign(PI, xmab) - Sign(PI, xmat)) / 2.0;
        }
        else
        {
            // General case
            ttd = Math.Atan(xmat / ymp) - Math.Atan(xmab / ymp);
        }
        
        // Precompute common terms
        double dbl = 2.0 * dl;
        double lgrt = 0.5 * Math.Log(r2t);
        double lgrb = 0.5 * Math.Log(r2b);
        double lgrd = lgrt - lgrb;
        
        double xyr2d = 2.0 * ymp * (xmat / r2t - xmab / r2b);
        double y2r2d = 2.0 * (ymp2 / r2t - ymp2 / r2b);
        
        // Check for NaN in intermediate values
        if (double.IsNaN(ttd) || double.IsNaN(lgrd) || double.IsNaN(xyr2d) || double.IsNaN(y2r2d))
        {
            System.Diagnostics.Debug.WriteLine($"NaN detected in closed-form integration:");
            System.Diagnostics.Debug.WriteLine($"  xmp={xmp:F6}, ymp={ymp:F6}, dl={dl:F6}");
            System.Diagnostics.Debug.WriteLine($"  r2t={r2t:E6}, r2b={r2b:E6}");
            System.Diagnostics.Debug.WriteLine($"  ttd={ttd}, lgrd={lgrd}");
            return;
        }
        
        // For constant elements (k=0)
        // NORMAL TRACTION influence (st11)
        st11[0, 0] = ymp * (-lgrd) * _displacementCoefficient;
        st11[0, 1] = (_kappa * (xmat * (lgrt - 1.0) - xmab * (lgrb - 1.0)) + ymp * _kappaMinus1 * ttd) * _displacementCoefficient;
        st11[0, 2] = ((3.0 - _kappa) * ttd - xyr2d) * _stressCoefficient;
        st11[0, 3] = (_kappaPlus1 * ttd + xyr2d) * _stressCoefficient;
        st11[0, 4] = (_kappaMinus1 * lgrd - y2r2d) * _stressCoefficient;
        
        // SHEAR TRACTION influence (st21)
        // Note: No kx*ky factor for non-symmetry case (kx=ky=1)
        st21[0, 0] = (_kappa * (xmat * lgrt - xmab * lgrb) + _kappaPlus1 * (ymp * ttd - (xmat - xmab))) * _displacementCoefficient;
        st21[0, 1] = ymp * (-lgrd) * _displacementCoefficient;
        st21[0, 2] = ((_kappa + 3.0) * lgrd + y2r2d) * _stressCoefficient;
        st21[0, 3] = (_kappaMinus1 * (-lgrd) - y2r2d) * _stressCoefficient;
        st21[0, 4] = (_kappaPlus1 * ttd - xyr2d) * _stressCoefficient;
        
        // For linear elements (order > 1), would add st11[1][...] and st21[1][...]
        // For quadratic elements (order > 2), would add st11[2][...] and st21[2][...]
        // TODO: Implement higher-order elements if needed
        
        // Note: The C++ code applies shape function combinations (linear_comb, quadratic_comb)
        // after this, but for constant elements we skip that step
    }

    /// <summary>
    /// Compute half-space influence coefficients (with ground surface)
    /// Ports coffsimg() from C++ - combines object (full-space) + image solutions
    /// </summary>
    private void ComputeHalfSpaceInfluence(Point2D fieldPoint, BoundaryElement element, 
        double groundSurfaceY, double cost, double sint, double xLocal, double yLocal, 
        double halfLength, GaussianQuadrature.QuadratureData quadrature, double[,] st11, double[,] st21)
    {
        // First: Compute full-space (object) part - this uses closed-form integration!
        ComputeFullSpaceInfluence(xLocal, yLocal, halfLength, quadrature, st11, st21);
        
        // Second: Add image part for ground surface boundary condition
        // Transform to global coordinates
        double x = fieldPoint.X;
        double y = fieldPoint.Y;
        double cx = element.MidPoint.X;
        double cy = element.MidPoint.Y;
        
        // Element orientation (source element)
        double cosbj = element.CosineDirection;
        double sinbj = element.SineDirection;
        
        // Compute coordinates for image solution
        double xmp = (x - cx) * cost + (y - cy) * sint;
        double ymp = (y - cy) * cost - (x - cx) * sint;
        double yp = (groundSurfaceY - cx) * sint - (groundSurfaceY - cy) * cost;
        double yy = ymp + yp;
        
        // Check if element is on ground surface and aligned with it
        bool isElementOnSurface = 
            Math.Abs(Math.Abs(cosbj) - Math.Abs(cost)) <= 1e-4 &&
            Math.Abs(yp) < 1e-4 &&
            Math.Abs(yy) < 1e-4;
        
        if (isElementOnSurface)
        {
            // Special case: element on ground surface - use closed-form image solution
            ComputeImageInfluenceClosedForm(xmp, yy, yp, halfLength, st11, st21);
        }
        else
        {
            // General case: use numerical integration with improved hlfspc() kernel
            ComputeImageInfluenceNumercial(x, y, cx, cy, cosbj, sinbj, cost, sint, 
                xmp, ymp, yp, yy, halfLength, quadrature, st11, st21);
        }
    }

    /// <summary>
    /// Compute image influence using closed-form integration (element on ground surface)
    /// Ports fshlf() from C++ (lines 2211-2253) - analytical formulas for surface elements
    /// </summary>
    private void ComputeImageInfluenceClosedForm(double xmp, double yy, double yp, 
        double halfLength, double[,] st11, double[,] st21)
    {
        double dl = halfLength;
        double sgn = 1.0;  // Sign factor
        
        // For constant elements: compute at left and right endpoints
        double ns = -1.0;
        for (int i = 1; i <= 2; i++)
        {
            ns = -ns;
            double xma = xmp - ns * dl;
            double yyp = yy + yp;
            double r1 = Math.Sqrt(xma * xma + yyp * yyp);
            
            double t1;
            if (Math.Abs(yyp) <= 1e-4)
            {
                t1 = -sgn * Sign(PI, xma) / 2.0;
            }
            else
            {
                t1 = Math.Atan(xma / yyp);
            }
            
            if (r1 < EPSILON)
                continue;
            
            double lgr1 = Math.Log(r1);
            double kappa2plus1 = _kappa * _kappa + 1.0;
            
            // Image part formulas from fshlf() - constant element (order=1, k=0)
            // st11 - Normal traction influence
            st11[0, 0] += 0.0;
            st11[0, 1] += kappa2plus1 * xma * (lgr1 - 1.0) * 0.5 * _displacementCoefficient;
            st11[0, 2] += (3.0 * _kappa - 1.0) * t1 * _stressCoefficient;
            st11[0, 3] += _kappaPlus1 * t1 * _stressCoefficient;
            st11[0, 4] += -_kappaMinus1 * lgr1 * _stressCoefficient;
            
            // st21 - Shear traction influence
            st21[0, 0] += (kappa2plus1 * xma * lgr1 / 2.0 - _kappaPlus1 * xma) * _displacementCoefficient;
            st21[0, 1] += 0.0;
            st21[0, 2] += (3.0 * _kappa + 1.0) * lgr1 * _stressCoefficient;
            st21[0, 3] += _kappaMinus1 * lgr1 * _stressCoefficient;
            st21[0, 4] += _kappaPlus1 * t1 * _stressCoefficient;
        }
    }

    /// <summary>
    /// Compute image influence using numerical integration with hlfspc() kernel
    /// Ports hlfspc() from C++ (lines 2262-2342) - general half-space solution
    /// </summary>
    private void ComputeImageInfluenceNumercial(double x, double y, double cx, double cy,
        double cosbj, double sinbj, double cost, double sint,
        double xmp, double ymp, double yp, double yy, double halfLength,
        GaussianQuadrature.QuadratureData quadrature, double[,] st11, double[,] st21)
    {
        double dl = halfLength;
        
        // Angle between field element and source element
        double cosa = cosbj * cost + sinbj * sint;
        double sina = sinbj * cost - cosbj * sint;
        
        // Numerical integration over element
        for (int i = 0; i < quadrature.Order; i++)
        {
            double zeta = quadrature.Points[i];
            double weight = quadrature.Weights[i];
            double loc_x = zeta * dl;
            
            // Position along element in global coordinates
            double xi = cx + zeta * dl * cosbj;
            double yi = cy + zeta * dl * sinbj;
            
            // Transform to local coordinates
            double xce = (x - xi) * cost + (y - yi) * sint;
            double ymp_local = -(x - xi) * sint + (y - yi) * cost;
            double yp_local = (0.0 - xi) * sint - (0.0 - yi) * cost;  // Ground surface at y=0
            double yy_local = ymp_local + yp_local;
            
            // Distance to image point
            double ypc = yy_local + yp_local;
            double r1 = Math.Sqrt(xce * xce + ypc * ypc);
            
            if (r1 < EPSILON)
                continue;
            
            double r12 = r1 * r1;
            double r14 = r12 * r12;
            double r16 = r14 * r12;
            
            double ymc = yy_local - yp_local;
            
            // Angular term
            double t;
            if (Math.Abs(xce) <= 1e-8 && Math.Abs(ypc) <= 1e-4)
            {
                t = Sign(PI, xce) / 4.0;
            }
            else
            {
                t = Math.Atan(xce / (r1 + ypc));
            }
            
            // hlfspc() kernel formulas from C++ (lines 2292-2318)
            double[] ut21 = new double[5];
            double[] ut11 = new double[5];
            
            ut21[0] = (2.0 * yp_local * yy_local + _kappa * xce * xce) / r12
                    - 4.0 * yp_local * xce * xce * yy_local / r14
                    - (_kappa * _kappa + 1.0) / 2.0 * Math.Log(r1)
                    + (1.0 - _kappa * _kappa) / 2.0;
                    
            ut21[1] = _kappa * xce * ymc / r12
                    - 4.0 * yp_local * xce * yy_local * ypc / r14
                    - (1.0 - _kappa * _kappa) * t;
                    
            ut21[2] = xce * _kappaMinus1 / r12
                    - 4.0 * xce * (_kappa * yp_local * ypc + 3.0 * yp_local * ymc + _kappa * xce * xce) / r14
                    + 32.0 * yp_local * xce * xce * xce * yy_local / r16;
                    
            ut21[3] = -xce * _kappaMinus1 / r12
                    - 4.0 * xce * (_kappa * yy_local * ypc + yp_local * ymc) / r14
                    + 32.0 * yp_local * xce * yy_local * ypc * ypc / r16;
                    
            ut21[4] = _kappaMinus1 * (ypc - 2.0 * yp_local) / r12
                    - 4.0 * (2.0 * yp_local * yy_local * ypc + xce * xce * (_kappa * yy_local + yp_local)) / r14
                    + 32.0 * yp_local * xce * xce * yy_local * ypc / r16;
            
            ut11[0] = 4.0 * yp_local * xce * yy_local * ypc / r14
                    + _kappa * xce * ymc / r12
                    + (1.0 - _kappa * _kappa) * t;
                    
            ut11[1] = (_kappa * ypc * ypc - 2.0 * yp_local * yy_local) / r12
                    + 4.0 * yp_local * yy_local * ypc * ypc / r14
                    - (_kappa * _kappa + 1.0) / 2.0 * Math.Log(r1);
                    
            ut11[2] = -4.0 * _kappa * xce * xce * ymc / r14
                    + 4.0 * yp_local * ypc * (ypc * _kappaMinus1 - 2.0 * yp_local) / r14
                    - _kappaMinus1 * (ypc + 6.0 * yp_local) / r12;
                    
            ut11[3] = _kappaMinus1 * (ypc - 2.0 * yp_local) / r12
                    - 4.0 * ypc * ((_kappa * yy_local + yp_local) * ypc - 6.0 * yp_local * yy_local) / r14
                    - 32.0 * yp_local * yy_local * ypc * ypc * ypc / r16;
                    
            ut11[4] = _kappaMinus1 * xce / r12
                    - 4.0 * xce * ((_kappa * yy_local - yp_local) * ypc - 2.0 * yp_local * yy_local) / r14
                    - 32.0 * yp_local * xce * yy_local * ypc * ypc / r16;
            
            // Accumulate with proper transformation (constant elements only)
            for (int j = 0; j < 5; j++)
            {
                if (j < 2) // Displacements
                {
                    st21[0, j] += (ut21[j] * cosa + ut11[j] * sina) * dl * weight * _displacementCoefficient;
                    st11[0, j] += (-ut21[j] * sina + ut11[j] * cosa) * dl * weight * _displacementCoefficient;
                }
                else // Stresses
                {
                    st21[0, j] += (ut21[j] * cosa + ut11[j] * sina) * dl * weight * _stressCoefficient;
                    st11[0, j] += (-ut21[j] * sina + ut11[j] * cosa) * dl * weight * _stressCoefficient;
                }
            }
        }
    }

    /// <summary>
    /// Compute shape function coefficients for linear elements
    /// Ports linear_comb() from C++
    /// </summary>
    public static void ApplyLinearShapeFunctions(double[,] st11, double[,] st21, 
        double xLocal, double halfLength)
    {
        // Temporary storage for constant element values
        double[,] temp11 = new double[3, 5];
        double[,] temp21 = new double[3, 5];
        
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                temp11[i, j] = st11[i, j];
                temp21[i, j] = st21[i, j];
            }
        }
        
        double dbl = halfLength / Math.Sqrt(2.0);
        double dbl2 = 2.0 * dbl;
        
        // Apply linear shape functions
        for (int n = 0; n < 5; n++)
        {
            st11[1, n] = (temp11[0, n] * (xLocal + dbl) + temp11[1, n]) / dbl2;
            st21[1, n] = (temp21[0, n] * (xLocal + dbl) + temp21[1, n]) / dbl2;
            st11[0, n] = (temp11[0, n] * (-xLocal + dbl) - temp11[1, n]) / dbl2;
            st21[0, n] = (temp21[0, n] * (-xLocal + dbl) - temp21[1, n]) / dbl2;
        }
    }

    /// <summary>
    /// Compute shape function coefficients for quadratic elements
    /// Ports quadratic_comb() from C++
    /// </summary>
    public static void ApplyQuadraticShapeFunctions(double[,] st11, double[,] st21, 
        double xLocal, double halfLength, double quadRatio = 0.816496580927726) // sqrt(2/3)
    {
        // Temporary storage
        double[,] temp11 = new double[3, 5];
        double[,] temp21 = new double[3, 5];
        
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                temp11[i, j] = st11[i, j];
                temp21[i, j] = st21[i, j];
            }
        }
        
        double db = halfLength * quadRatio;
        double dbs2 = db * db;
        double tdbs2 = 2.0 * dbs2;
        double txmp = 2.0 * xLocal;
        
        // Apply quadratic shape functions
        for (int n = 0; n < 5; n++)
        {
            st11[0, n] = (temp11[0, n] * xLocal * (xLocal - db) + 
                         temp11[1, n] * (txmp - db) + temp11[2, n]) / tdbs2;
            st11[1, n] = (temp11[0, n] * (db + xLocal) * (db - xLocal) - 
                         temp11[1, n] * txmp - temp11[2, n]) / dbs2;
            st11[2, n] = (temp11[0, n] * xLocal * (xLocal + db) + 
                         temp11[1, n] * (txmp + db) + temp11[2, n]) / tdbs2;
            
            st21[0, n] = (temp21[0, n] * xLocal * (xLocal - db) + 
                         temp21[1, n] * (txmp - db) + temp21[2, n]) / tdbs2;
            st21[1, n] = (temp21[0, n] * (db + xLocal) * (db - xLocal) - 
                         temp21[1, n] * txmp - temp21[2, n]) / dbs2;
            st21[2, n] = (temp21[0, n] * xLocal * (xLocal + db) + 
                         temp21[1, n] * (txmp + db) + temp21[2, n]) / tdbs2;
        }
    }

    /// <summary>
    /// Sign function: returns -a if b < 0, else returns a
    /// Matches C++ sign() function behavior
    /// </summary>
    private static double Sign(double a, double b)
    {
        return b < 0.0 ? -a : a;
    }
}

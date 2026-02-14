using System.Security.Cryptography;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using Examine2DModel.Materials;
using Examine2DModel.Analysis;

namespace Examine2DModel.BEM;

/// <summary>
/// Builds influence coefficient matrices for BEM analysis with caching and parallel assembly.
/// Ports matrix assembly logic from make_inf_matrix() and coffsobj() in bcompute2d.cpp (lines 998-2022)
/// Performance optimized with aggressive caching and parallelization
/// </summary>
public class InfluenceMatrixBuilder
{
    private readonly ElementIntegrator _integrator;
    private readonly BEMConfiguration _config;
    
    // Cache for influence matrix - only rebuild if geometry/materials change
    private Matrix<double>? _cachedMatrix;
    private int _cachedElementCount;
    private string? _cachedGeometryHash;
    private double _cachedGroundSurfaceY;
    private bool _cachedIsHalfSpace;
    private bool _cacheValid;
    
    /// <summary>
    /// Statistics for performance monitoring
    /// </summary>
    public class BuildStatistics
    {
        public TimeSpan MatrixAssemblyTime { get; set; }
        public TimeSpan HashComputationTime { get; set; }
        public bool CacheHit { get; set; }
        public int ElementCount { get; set; }
        public int DegreesOfFreedom { get; set; }
    }
    
    public BuildStatistics LastBuildStats { get; private set; } = new();
    
    public InfluenceMatrixBuilder(IIsotropicMaterial material, BEMConfiguration config)
    {
        if (material == null)
            throw new ArgumentNullException(nameof(material));
        if (config == null)
            throw new ArgumentNullException(nameof(config));
            
        _integrator = new ElementIntegrator(material);
        _config = config;
        _cacheValid = false;
    }
    
    /// <summary>
    /// Build the influence coefficient matrix for the boundary element system.
    /// Returns cached matrix if geometry and materials haven't changed.
    /// </summary>
    /// <param name="elements">List of boundary elements</param>
    /// <param name="groundSurfaceY">Y-coordinate of ground surface (for half-space problems)</param>
    /// <param name="isHalfSpace">True for half-space problem, false for full-space</param>
    /// <returns>Influence coefficient matrix [2N x 2N] where N is number of elements</returns>
    public Matrix<double> BuildMatrix(List<BoundaryElement> elements, 
        double groundSurfaceY = 0.0, bool isHalfSpace = true)
    {
        var stats = new BuildStatistics
        {
            ElementCount = elements.Count
        };
        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Check cache validity - includes groundSurfaceY and isHalfSpace
        var hashStopwatch = System.Diagnostics.Stopwatch.StartNew();
        string geometryHash = ComputeGeometryHash(elements);
        stats.HashComputationTime = hashStopwatch.Elapsed;
        
        if (_config.EnableCaching && _cacheValid && 
            _cachedMatrix != null && 
            _cachedElementCount == elements.Count && 
            _cachedGeometryHash == geometryHash &&
            Math.Abs(_cachedGroundSurfaceY - groundSurfaceY) < 1e-6 &&
            _cachedIsHalfSpace == isHalfSpace)
        {
            // CACHE HIT - Return cached matrix immediately
            stats.CacheHit = true;
            stats.DegreesOfFreedom = _cachedMatrix.RowCount;
            stats.MatrixAssemblyTime = TimeSpan.Zero;
            LastBuildStats = stats;
            return _cachedMatrix;
        }
        
        // CACHE MISS - Build new matrix
        stats.CacheHit = false;
        var assemblyStopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Get element order (1=constant, 2=linear, 3=quadratic)
        int order = GetElementOrder(_config.ElementType);
        int dof = elements.Count * 2 * order; // Degrees of freedom: 2 DOF per node
        stats.DegreesOfFreedom = dof;
        
        // Initialize matrix (dense matrix for BEM - typically not sparse)
        var matrix = Matrix<double>.Build.Dense(dof, dof);
        
        // PARALLEL MATRIX ASSEMBLY - Each row is independent
        // Use thread-safe parallel loop for assembling influence coefficients
        Parallel.For(0, elements.Count, i =>
        {
            var iElement = elements[i];
            
            // Element direction cosines for coordinate transformation
            double cosbi = iElement.CosineDirection;
            double sinbi = iElement.SineDirection;
            double cosbis = cosbi * cosbi;
            double sinbis = sinbi * sinbi;
            double cossinbi = cosbi * sinbi;
            
            // For each collocation point on element i (depends on element order)
            for (int mi = 0; mi < order; mi++)
            {
                // Get collocation point coordinates based on element order
                var collocationPoint = GetCollocationPoint(iElement, mi, order);
                
                // Map to global DOF row indices
                int rowX = 2 * order * i + 2 * mi;       // x-displacement/traction at node mi of element i
                int rowY = 2 * order * i + 2 * mi + 1;   // y-displacement/traction at node mi of element i
                
                // Loop through all source elements j
                for (int j = 0; j < elements.Count; j++)
                {
                    var jElement = elements[j];
                    
                    // Compute influence of element j on collocation point of element i
                    var influence = _integrator.ComputeInfluence(
                        collocationPoint, jElement, groundSurfaceY, isHalfSpace);
                    
                    // For constant elements, the influence applies directly to the element DOF
                    // For higher-order elements, we need to apply shape function weighting
                    if (order == 1)
                    {
                        // Constant elements - direct mapping
                        int colX = 2 * j;       // x-traction on element j
                        int colY = 2 * j + 1;   // y-traction on element j
                        
                        // CRITICAL: Transform influence coefficients based on boundary condition type
                        // This matches C++ code lines 1205-1241 in bcompute2d.cpp
                        // Influence coefficients from ComputeInfluence() are in GLOBAL coordinates
                        // We need to transform them based on the field element i's local coordinate system
                        double a_rowX_colX, a_rowX_colY, a_rowY_colX, a_rowY_colY;
                        
                        switch (iElement.BoundaryConditionType)
                        {
                            case 1: // Traction specified (displacement unknown)
                                // Matrix contains stress influence coefficients transformed to element i's local coordinates
                                // Extracts shear stress and normal stress in local system
                                a_rowX_colX = (influence.SyyFromShear - influence.SxxFromShear) * cossinbi 
                                            + influence.SxyFromShear * (cosbis - sinbis);
                                a_rowX_colY = (influence.SyyFromNormal - influence.SxxFromNormal) * cossinbi 
                                            + influence.SxyFromNormal * (cosbis - sinbis);
                                a_rowY_colX = influence.SxxFromShear * sinbis - 2.0 * influence.SxyFromShear * cossinbi 
                                            + influence.SyyFromShear * cosbis;
                                a_rowY_colY = influence.SxxFromNormal * sinbis - 2.0 * influence.SxyFromNormal * cossinbi 
                                            + influence.SyyFromNormal * cosbis;
                                break;
                                
                            case 2: // Displacement specified (traction unknown)
                                // Matrix contains displacement influence coefficients transformed to element i's local coordinates
                                a_rowX_colX = influence.UxFromShear * cosbi + influence.UyFromShear * sinbi;
                                a_rowX_colY = influence.UxFromNormal * cosbi + influence.UyFromNormal * sinbi;
                                a_rowY_colX = -influence.UxFromShear * sinbi + influence.UyFromShear * cosbi;
                                a_rowY_colY = -influence.UxFromNormal * sinbi + influence.UyFromNormal * cosbi;
                                break;
                                
                            case 3: // Mixed BC: displacement for normal, stress for shear
                                a_rowX_colX = influence.UxFromShear * cosbi + influence.UyFromShear * sinbi;
                                a_rowX_colY = influence.UxFromNormal * cosbi + influence.UyFromNormal * sinbi;
                                a_rowY_colX = influence.SxxFromShear * sinbis - 2.0 * influence.SxyFromShear * cossinbi 
                                            + influence.SyyFromShear * cosbis;
                                a_rowY_colY = influence.SxxFromNormal * sinbis - 2.0 * influence.SxyFromNormal * cossinbi 
                                            + influence.SyyFromNormal * cosbis;
                                break;
                                
                            case 4: // Mixed BC: stress for normal, displacement for shear
                                a_rowX_colX = (influence.SyyFromShear - influence.SxxFromShear) * cossinbi 
                                            + influence.SxyFromShear * (cosbis - sinbis);
                                a_rowX_colY = (influence.SyyFromNormal - influence.SxxFromNormal) * cossinbi 
                                            + influence.SxyFromNormal * (cosbis - sinbis);
                                a_rowY_colX = -influence.UxFromShear * sinbi + influence.UyFromShear * cosbi;
                                a_rowY_colY = -influence.UxFromNormal * sinbi + influence.UyFromNormal * cosbi;
                                break;
                                
                            default:
                                throw new InvalidOperationException($"Invalid boundary condition type: {iElement.BoundaryConditionType}");
                        }
                        
                        // Thread-safe assignment
                        lock (matrix)
                        {
                            matrix[rowX, colX] = a_rowX_colX;
                            matrix[rowX, colY] = a_rowX_colY;
                            matrix[rowY, colX] = a_rowY_colX;
                            matrix[rowY, colY] = a_rowY_colY;
                        }
                    }
                    else
                    {
                        // Linear/quadratic elements - apply shape function weighting
                        for (int mj = 0; mj < order; mj++)
                        {
                            int colX = 2 * order * j + 2 * mj;
                            int colY = 2 * order * j + 2 * mj + 1;
                            
                            double shapeFunctionWeight = GetShapeFunctionWeight(mj, order);
                            
                            // Apply same BC-dependent transformations as constant elements
                            double a_rowX_colX, a_rowX_colY, a_rowY_colX, a_rowY_colY;
                            
                            switch (iElement.BoundaryConditionType)
                            {
                                case 1: // Traction specified
                                    a_rowX_colX = (influence.SyyFromShear - influence.SxxFromShear) * cossinbi 
                                                + influence.SxyFromShear * (cosbis - sinbis);
                                    a_rowX_colY = (influence.SyyFromNormal - influence.SxxFromNormal) * cossinbi 
                                                + influence.SxyFromNormal * (cosbis - sinbis);
                                    a_rowY_colX = influence.SxxFromShear * sinbis - 2.0 * influence.SxyFromShear * cossinbi 
                                                + influence.SyyFromShear * cosbis;
                                    a_rowY_colY = influence.SxxFromNormal * sinbis - 2.0 * influence.SxyFromNormal * cossinbi 
                                                + influence.SyyFromNormal * cosbis;
                                    break;
                                    
                                case 2: // Displacement specified
                                    a_rowX_colX = influence.UxFromShear * cosbi + influence.UyFromShear * sinbi;
                                    a_rowX_colY = influence.UxFromNormal * cosbi + influence.UyFromNormal * sinbi;
                                    a_rowY_colX = -influence.UxFromShear * sinbi + influence.UyFromShear * cosbi;
                                    a_rowY_colY = -influence.UxFromNormal * sinbi + influence.UyFromNormal * cosbi;
                                    break;
                                    
                                case 3: // Mixed: displacement normal, stress shear
                                    a_rowX_colX = influence.UxFromShear * cosbi + influence.UyFromShear * sinbi;
                                    a_rowX_colY = influence.UxFromNormal * cosbi + influence.UyFromNormal * sinbi;
                                    a_rowY_colX = influence.SxxFromShear * sinbis - 2.0 * influence.SxyFromShear * cossinbi 
                                                + influence.SyyFromShear * cosbis;
                                    a_rowY_colY = influence.SxxFromNormal * sinbis - 2.0 * influence.SxyFromNormal * cossinbi 
                                                + influence.SyyFromNormal * cosbis;
                                    break;
                                    
                                case 4: // Mixed: stress normal, displacement shear
                                    a_rowX_colX = (influence.SyyFromShear - influence.SxxFromShear) * cossinbi 
                                                + influence.SxyFromShear * (cosbis - sinbis);
                                    a_rowX_colY = (influence.SyyFromNormal - influence.SxxFromNormal) * cossinbi 
                                                + influence.SxyFromNormal * (cosbis - sinbis);
                                    a_rowY_colX = -influence.UxFromShear * sinbi + influence.UyFromShear * cosbi;
                                    a_rowY_colY = -influence.UxFromNormal * sinbi + influence.UyFromNormal * cosbi;
                                    break;
                                    
                                default:
                                    throw new InvalidOperationException($"Invalid boundary condition type: {iElement.BoundaryConditionType}");
                            }
                            
                            lock (matrix)
                            {
                                matrix[rowX, colX] += a_rowX_colX * shapeFunctionWeight;
                                matrix[rowX, colY] += a_rowX_colY * shapeFunctionWeight;
                                matrix[rowY, colX] += a_rowY_colX * shapeFunctionWeight;
                                matrix[rowY, colY] += a_rowY_colY * shapeFunctionWeight;
                            }
                        }
                    }
                }
            }
        });
        
        stats.MatrixAssemblyTime = assemblyStopwatch.Elapsed;
        LastBuildStats = stats;
        
        // Debug: Check matrix for conditioning issues
        try
        {
            double conditionNumber = matrix.ConditionNumber();
            System.Diagnostics.Debug.WriteLine($"  Influence matrix statistics:");
            System.Diagnostics.Debug.WriteLine($"    Size: {matrix.RowCount}x{matrix.ColumnCount}");
            System.Diagnostics.Debug.WriteLine($"    Condition number: {conditionNumber:E3}");
            
            // Sample a few matrix entries for sanity check
            if (matrix.RowCount > 0 && matrix.ColumnCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"    Sample entries:");
                System.Diagnostics.Debug.WriteLine($"      [0,0]={matrix[0,0]:E6}, [0,1]={matrix[0,1]:E6}");
                if (matrix.RowCount > 1)
                    System.Diagnostics.Debug.WriteLine($"      [1,0]={matrix[1,0]:E6}, [1,1]={matrix[1,1]:E6}");
            }
            
            // Check for NaN or Inf
            bool hasNaN = false;
            bool hasInf = false;
            for (int i = 0; i < Math.Min(10, matrix.RowCount); i++)
            {
                for (int j = 0; j < Math.Min(10, matrix.ColumnCount); j++)
                {
                    if (double.IsNaN(matrix[i, j])) hasNaN = true;
                    if (double.IsInfinity(matrix[i, j])) hasInf = true;
                }
            }
            System.Diagnostics.Debug.WriteLine($"    Contains NaN: {hasNaN}, Contains Inf: {hasInf}");
            
            if (conditionNumber > 1e10)
            {
                System.Diagnostics.Debug.WriteLine($"    WARNING: Matrix is ill-conditioned!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"    Could not compute condition number: {ex.Message}");
        }
        
        // Cache the matrix for reuse
        if (_config.EnableCaching)
        {
            _cachedMatrix = matrix;
            _cachedElementCount = elements.Count;
            _cachedGeometryHash = geometryHash;
            _cachedGroundSurfaceY = groundSurfaceY;
            _cachedIsHalfSpace = isHalfSpace;
            _cacheValid = true;
        }
        
        return matrix;
    }
    
    /// <summary>
    /// Build influence matrix for stress/displacement at field points.
    /// This is used for computing stresses at internal points after solving the boundary system.
    /// </summary>
    /// <param name="fieldPoints">List of field points where stresses are needed</param>
    /// <param name="elements">List of boundary elements</param>
    /// <param name="groundSurfaceY">Y-coordinate of ground surface</param>
    /// <param name="isHalfSpace">True for half-space problem</param>
    /// <returns>Influence matrix [6M x 2N] where M=field points, N=elements</returns>
    public Matrix<double> BuildFieldPointMatrix(List<FieldPoint> fieldPoints, 
        List<BoundaryElement> elements, double groundSurfaceY = 0.0, bool isHalfSpace = true)
    {
        int order = GetElementOrder(_config.ElementType);
        int elementDOF = elements.Count * 2 * order;
        // 6 stress/displacement components per field point: ux, uy, sxx, syy, sxy, szz
        int fieldPointComponents = fieldPoints.Count * 6;
        
        var matrix = Matrix<double>.Build.Dense(fieldPointComponents, elementDOF);
        
        // PARALLEL FIELD POINT EVALUATION
        Parallel.For(0, fieldPoints.Count, fp =>
        {
            var fieldPoint = fieldPoints[fp];
            
            // Row indices for this field point's components
            int rowUx = 6 * fp + 0;
            int rowUy = 6 * fp + 1;
            int rowSxx = 6 * fp + 2;
            int rowSyy = 6 * fp + 3;
            int rowSxy = 6 * fp + 4;
            // int rowSzz = 6 * fp + 5; // Out-of-plane stress (calculated separately)
            
            // Compute influence from each boundary element
            for (int j = 0; j < elements.Count; j++)
            {
                var element = elements[j];
                
                // Compute influence of element on field point
                var influence = _integrator.ComputeInfluence(
                    fieldPoint.Location, element, groundSurfaceY, isHalfSpace);
                
                if (order == 1)
                {
                    // Constant elements - direct mapping
                    int colX = 2 * j;
                    int colY = 2 * j + 1;
                    
                    // Thread-safe assignment
                    lock (matrix)
                    {
                        // Displacements
                        matrix[rowUx, colX] = influence.UxFromNormal;
                        matrix[rowUx, colY] = influence.UxFromShear;
                        matrix[rowUy, colX] = influence.UyFromNormal;
                        matrix[rowUy, colY] = influence.UyFromShear;
                        
                        // Stresses
                        matrix[rowSxx, colX] = influence.SxxFromNormal;
                        matrix[rowSxx, colY] = influence.SxxFromShear;
                        matrix[rowSyy, colX] = influence.SyyFromNormal;
                        matrix[rowSyy, colY] = influence.SyyFromShear;
                        matrix[rowSxy, colX] = influence.SxyFromNormal;
                        matrix[rowSxy, colY] = influence.SxyFromShear;
                    }
                }
                else
                {
                    // Linear/quadratic elements - apply shape function weighting
                    for (int mj = 0; mj < order; mj++)
                    {
                        int colX = 2 * order * j + 2 * mj;
                        int colY = 2 * order * j + 2 * mj + 1;
                        
                        double shapeWeight = GetShapeFunctionWeight(mj, order);
                        
                        lock (matrix)
                        {
                            // Displacements
                            matrix[rowUx, colX] += influence.UxFromNormal * shapeWeight;
                            matrix[rowUx, colY] += influence.UxFromShear * shapeWeight;
                            matrix[rowUy, colX] += influence.UyFromNormal * shapeWeight;
                            matrix[rowUy, colY] += influence.UyFromShear * shapeWeight;
                            
                            // Stresses
                            matrix[rowSxx, colX] += influence.SxxFromNormal * shapeWeight;
                            matrix[rowSxx, colY] += influence.SxxFromShear * shapeWeight;
                            matrix[rowSyy, colX] += influence.SyyFromNormal * shapeWeight;
                            matrix[rowSyy, colY] += influence.SyyFromShear * shapeWeight;
                            matrix[rowSxy, colX] += influence.SxyFromNormal * shapeWeight;
                            matrix[rowSxy, colY] += influence.SxyFromShear * shapeWeight;
                        }
                    }
                }
            }
        });
        
        return matrix;
    }
    
    /// <summary>
    /// Invalidate the matrix cache, forcing rebuild on next BuildMatrix() call.
    /// Call this when geometry, materials, or solver settings change.
    /// </summary>
    public void InvalidateCache()
    {
        _cacheValid = false;
        _cachedMatrix = null;
        _cachedGeometryHash = null;
    }
    
    /// <summary>
    /// Check if the cache is valid for the given elements
    /// </summary>
    public bool IsCacheValid(List<BoundaryElement> elements)
    {
        if (!_config.EnableCaching || !_cacheValid || _cachedMatrix == null)
            return false;
            
        if (_cachedElementCount != elements.Count)
            return false;
            
        string currentHash = ComputeGeometryHash(elements);
        return _cachedGeometryHash == currentHash;
    }
    
    /// <summary>
    /// Compute hash of element geometry for cache validation.
    /// Uses SHA256 for fast, collision-resistant hashing.
    /// </summary>
    private static string ComputeGeometryHash(List<BoundaryElement> elements)
    {
        // Build string representation of geometry
        var sb = new StringBuilder(elements.Count * 100);
        
        foreach (var element in elements)
        {
            // Include critical geometric properties
            sb.Append(element.StartPoint.X.ToString("G17"));
            sb.Append(',');
            sb.Append(element.StartPoint.Y.ToString("G17"));
            sb.Append(',');
            sb.Append(element.EndPoint.X.ToString("G17"));
            sb.Append(',');
            sb.Append(element.EndPoint.Y.ToString("G17"));
            sb.Append(',');
            sb.Append(element.ElementType);
            sb.Append(',');
            sb.Append(element.BoundaryId);
            sb.Append(';');
        }
        
        // Compute SHA256 hash
        byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] hash = SHA256.HashData(bytes);
        
        // Convert to hex string
        return Convert.ToHexString(hash);
    }
    
    /// <summary>
    /// Get collocation point on element based on element order and local node index
    /// Ports logic from make_inf_matrix() lines 1113-1147
    /// </summary>
    private static CAD2DModel.Geometry.Point2D GetCollocationPoint(
        BoundaryElement element, int nodeIndex, int order)
    {
        double x, y;
        
        switch (order)
        {
            case 1: // Constant element - collocation at midpoint
                x = element.MidPoint.X;
                y = element.MidPoint.Y;
                break;
                
            case 2: // Linear element - collocation at sqrt(2) points
                {
                    double st2_v = 1.0 / Math.Sqrt(2.0);
                    double factor = (Math.Sqrt(2.0) - 1.0) * st2_v;
                    
                    switch (nodeIndex)
                    {
                        case 0: // Left node
                            x = element.StartPoint.X * st2_v + element.MidPoint.X * factor;
                            y = element.StartPoint.Y * st2_v + element.MidPoint.Y * factor;
                            break;
                        case 1: // Right node
                            x = element.EndPoint.X * st2_v + element.MidPoint.X * factor;
                            y = element.EndPoint.Y * st2_v + element.MidPoint.Y * factor;
                            break;
                        default:
                            throw new ArgumentException($"Invalid node index {nodeIndex} for linear element");
                    }
                }
                break;
                
            case 3: // Quadratic element - collocation at Gauss points
                {
                    double quad_rat = Math.Sqrt(3.0) * 0.5;
                    double mquad_rat = 1.0 - quad_rat;
                    
                    switch (nodeIndex)
                    {
                        case 0: // Left node
                            x = element.StartPoint.X * quad_rat + element.MidPoint.X * mquad_rat;
                            y = element.StartPoint.Y * quad_rat + element.MidPoint.Y * mquad_rat;
                            break;
                        case 1: // Middle node
                            x = element.MidPoint.X;
                            y = element.MidPoint.Y;
                            break;
                        case 2: // Right node
                            x = element.EndPoint.X * quad_rat + element.MidPoint.X * mquad_rat;
                            y = element.EndPoint.Y * quad_rat + element.MidPoint.Y * mquad_rat;
                            break;
                        default:
                            throw new ArgumentException($"Invalid node index {nodeIndex} for quadratic element");
                    }
                }
                break;
                
            default:
                throw new ArgumentException($"Unsupported element order: {order}");
        }
        
        return new CAD2DModel.Geometry.Point2D(x, y);
    }
    
    /// <summary>
    /// Get shape function weight for a node on an element.
    /// For constant elements, weight is 1. For linear/quadratic, weights are applied
    /// during shape function combination (handled separately in integration).
    /// </summary>
    private static double GetShapeFunctionWeight(int nodeIndex, int order)
    {
        // For now, return uniform weight
        // Shape function weighting is handled by linear_comb() and quadratic_comb()
        // in the element integrator
        return 1.0;
    }
    
    /// <summary>
    /// Get element order from ElementType enum
    /// </summary>
    private static int GetElementOrder(ElementType elementType)
    {
        return elementType switch
        {
            ElementType.Constant => 1,
            ElementType.Linear => 2,
            ElementType.Quadratic => 3,
            _ => throw new ArgumentException($"Unknown element type: {elementType}")
        };
    }
    
    /// <summary>
    /// Apply boundary conditions to the influence matrix.
    /// Modifies matrix based on known displacements or tractions on boundaries.
    /// Ports set_bc() logic from C++ (line 3113)
    /// </summary>
    /// <param name="matrix">Influence matrix to modify</param>
    /// <param name="elements">Boundary elements with BC information</param>
    /// <param name="rightHandSide">Right-hand side vector to modify</param>
    public static void ApplyBoundaryConditions(Matrix<double> matrix, 
        List<BoundaryElement> elements, Vector<double> rightHandSide, int order = 1)
    {
        // Loop through each element and apply boundary conditions
        for (int i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            
            for (int mi = 0; mi < order; mi++)
            {
                int rowX = 2 * order * i + 2 * mi;
                int rowY = 2 * order * i + 2 * mi + 1;
                
                // Apply boundary conditions based on type
                // Type 1: Traction specified (displacement unknown)
                // Type 2: Displacement specified (traction unknown)
                
                if (element.BoundaryConditionType == 2)
                {
                    // Displacement specified - swap columns to move unknowns to LHS
                    // This is a simplified approach; full implementation requires
                    // rearranging the system to separate known/unknown DOFs
                    
                    // Move known displacement to RHS
                    rightHandSide[rowX] -= element.NormalBoundaryCondition * 
                        matrix[rowX, rowX];
                    rightHandSide[rowY] -= element.ShearBoundaryCondition * 
                        matrix[rowY, rowY];
                }
                else if (element.BoundaryConditionType == 1)
                {
                    // Traction specified - add known traction to RHS
                    rightHandSide[rowX] += element.NormalBoundaryCondition;
                    rightHandSide[rowY] += element.ShearBoundaryCondition;
                }
            }
        }
    }
}

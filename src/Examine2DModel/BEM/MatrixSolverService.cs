using System.Security.Cryptography;
using System.Text;
using Examine2DModel.Analysis;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Double.Solvers;
using MathNet.Numerics.LinearAlgebra.Solvers;

namespace Examine2DModel.BEM;

/// <summary>
/// High-performance matrix solver with caching and warm start support.
/// Uses direct LU decomposition for small problems and iterative BiCGStab for large problems.
/// </summary>
public class MatrixSolverService : IMatrixSolver
{
    private readonly BEMConfiguration _configuration;
    
    // Solution cache for warm start
    private Vector<double>? _cachedSolution;
    private string? _cachedProblemHash;
    
    // Matrix cache for incremental solves
    private Matrix<double>? _cachedMatrix;
    private string? _cachedMatrixHash;
    
    /// <summary>
    /// Creates a new matrix solver service with default configuration
    /// </summary>
    public MatrixSolverService() : this(new BEMConfiguration())
    {
    }
    
    /// <summary>
    /// Creates a new matrix solver service with specified configuration
    /// </summary>
    public MatrixSolverService(BEMConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        // Enable Intel MKL for 3-10x faster matrix operations if available
        try
        {
            MathNet.Numerics.Control.UseNativeMKL();
        }
        catch
        {
            // MKL not available, will use managed implementation
            // This is fine for development/testing but slower
        }
    }
    
    /// <summary>
    /// Solve linear system Ax = b using cached results when possible
    /// </summary>
    public double[] Solve(double[,] A, double[] b)
    {
        if (A == null) throw new ArgumentNullException(nameof(A));
        if (b == null) throw new ArgumentNullException(nameof(b));
        if (A.GetLength(0) != A.GetLength(1))
            throw new ArgumentException("Matrix A must be square", nameof(A));
        if (A.GetLength(0) != b.Length)
            throw new ArgumentException("Matrix dimensions must match vector length", nameof(b));
        
        // Convert to MathNet matrix for efficient operations
        var matrix = Matrix<double>.Build.DenseOfArray(A);
        var vector = Vector<double>.Build.Dense(b);
        
        return SolveInternal(matrix, vector);
    }
    
    /// <summary>
    /// Solve using sparse matrix format (more efficient for large sparse systems)
    /// </summary>
    public double[] SolveSparse(Analysis.SparseMatrix sparseMatrix, double[] b)
    {
        // For now, this is a placeholder - will be implemented when we have real sparse matrix support
        throw new NotImplementedException("Sparse matrix solver not yet implemented");
    }
    
    /// <summary>
    /// Solve using MathNet matrix types with caching and warm start
    /// </summary>
    public double[] Solve(Matrix<double> A, Vector<double> b)
    {
        if (A == null) throw new ArgumentNullException(nameof(A));
        if (b == null) throw new ArgumentNullException(nameof(b));
        if (A.RowCount != A.ColumnCount)
            throw new ArgumentException("Matrix A must be square", nameof(A));
        if (A.RowCount != b.Count)
            throw new ArgumentException("Matrix dimensions must match vector length", nameof(b));
        
        return SolveInternal(A, b);
    }
    
    /// <summary>
    /// Internal solver implementation with caching logic
    /// </summary>
    private double[] SolveInternal(Matrix<double> A, Vector<double> b)
    {
        // Check cache if caching is enabled
        if (_configuration.EnableCaching)
        {
            string problemHash = ComputeProblemHash(A, b);
            
            if (_cachedSolution != null && _cachedProblemHash == problemHash)
            {
                // Cache hit - return immediately
                return _cachedSolution.ToArray();
            }
            
            // Check if only RHS changed (matrix unchanged)
            string matrixHash = ComputeMatrixHash(A);
            bool matrixUnchanged = _cachedMatrix != null && _cachedMatrixHash == matrixHash;
            
            if (!matrixUnchanged)
            {
                // Matrix changed, cache it
                _cachedMatrix = A;
                _cachedMatrixHash = matrixHash;
            }
        }
        
        // Select solver based on problem size
        Vector<double> solution;
        int dof = A.RowCount;
        
        if (dof < _configuration.DirectSolverThreshold)
        {
            // Small problems: Direct solver (LU decomposition)
            // Fast and reliable for problems < 1000 DOF
            // Time: ~50-200ms for 1000 DOF with MKL
            solution = SolveDirect(A, b);
        }
        else
        {
            // Large problems: Iterative solver (BiCGStab)
            // More efficient for large problems
            // Time: ~200-800ms for 2000-4000 DOF
            solution = SolveIterative(A, b);
        }
        
        // Cache the solution if caching enabled
        if (_configuration.EnableCaching)
        {
            _cachedSolution = solution;
            _cachedProblemHash = ComputeProblemHash(A, b);
        }
        
        return solution.ToArray();
    }
    
    /// <summary>
    /// Direct solver using LU decomposition (fast for small systems)
    /// </summary>
    private Vector<double> SolveDirect(Matrix<double> A, Vector<double> b)
    {
        // Check matrix condition number first
        double conditionNumber = double.NaN;
        try
        {
            conditionNumber = A.ConditionNumber();
            System.Diagnostics.Debug.WriteLine($"  Matrix condition number: {conditionNumber:E3}");
            
            // If condition number is too large, matrix is ill-conditioned
            if (conditionNumber > 1e10)
            {
                System.Diagnostics.Debug.WriteLine($"  WARNING: Matrix is ill-conditioned (condition number > 1e10)");
                System.Diagnostics.Debug.WriteLine($"  Falling back to SVD solver...");
                return SolveWithSVD(A, b);
            }
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine($"  Could not compute condition number, proceeding with caution...");
        }
        
        try
        {
            // MathNet's Solve() uses LU decomposition internally with partial pivoting
            // This is equivalent to the C++ solve(), lu_decomp(), lu_fwdsub(), lu_bcksub()
            var solution = A.Solve(b);
            
            // Check for unrealistic values (overflow/underflow)
            bool hasHugeValues = solution.Any(x => Math.Abs(x) > 1e15);
            
            if (hasHugeValues)
            {
                System.Diagnostics.Debug.WriteLine($"  WARNING: Solution contains unrealistically large values (>1e15)");
                System.Diagnostics.Debug.WriteLine($"  This indicates numerical instability. Falling back to SVD...");
                return SolveWithSVD(A, b);
            }
            
            return solution;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"  Direct solver failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"  Attempting solve with SVD...");
            return SolveWithSVD(A, b);
        }
    }
    
    /// <summary>
    /// Solve using SVD (Singular Value Decomposition) for ill-conditioned or singular matrices
    /// </summary>
    private Vector<double> SolveWithSVD(Matrix<double> A, Vector<double> b)
    {
        var svd = A.Svd(computeVectors: true);
        var solution = svd.Solve(b);
        
        System.Diagnostics.Debug.WriteLine($"  SVD solve completed");
        System.Diagnostics.Debug.WriteLine($"    Rank: {svd.Rank}");
        System.Diagnostics.Debug.WriteLine($"    Condition number: {svd.ConditionNumber:E3}");
        
        return solution;
    }
    
    /// <summary>
    /// Iterative solver using BiCGStab with warm start and fallback to direct solver
    /// </summary>
    private Vector<double> SolveIterative(Matrix<double> A, Vector<double> b)
    {
        try
        {
            // Create BiCGStab solver (Bi-Conjugate Gradient Stabilized)
            // This is equivalent to bi_conjugate_gradient_solver() from the C++ code
            var solver = new BiCgStab();
            
            // Create iterator with convergence criteria
            var iterator = new Iterator<double>(
                new ResidualStopCriterion<double>(_configuration.Tolerance),
                new IterationCountStopCriterion<double>(_configuration.MaxIterations)
            );
            
            // Create a preconditioner for better convergence
            var preconditioner = new DiagonalPreconditioner();
            
            // Use previous solution as initial guess for warm start if available
            // This significantly speeds up convergence for similar problems
            Vector<double> initialGuess;
            
            if (_configuration.EnableCaching && _cachedSolution != null && _cachedSolution.Count == b.Count)
            {
                // Use cached solution as warm start
                initialGuess = _cachedSolution.Clone();
            }
            else
            {
                // Start with zero vector
                initialGuess = Vector<double>.Build.Dense(b.Count);
            }
            
            // The Solve method modifies the result vector in-place
            // We start with the initial guess
            var result = initialGuess.Clone();
            solver.Solve(A, b, result, iterator, preconditioner);
            
            return result;
        }
        catch (NumericalBreakdownException)
        {
            // Iterative solver failed to converge - fall back to direct solver
            // BEM influence matrices are typically dense and better suited for direct solvers
            return SolveDirect(A, b);
        }
    }
    
    /// <summary>
    /// Compute hash of problem (matrix + RHS) for cache lookup
    /// </summary>
    private string ComputeProblemHash(Matrix<double> A, Vector<double> b)
    {
        using var sha256 = SHA256.Create();
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        // Hash matrix dimensions
        writer.Write(A.RowCount);
        writer.Write(A.ColumnCount);
        
        // Hash matrix elements (sample for performance - hash every 10th element)
        // This is a trade-off between hash computation time and collision probability
        int stride = Math.Max(1, A.RowCount / 100);
        for (int i = 0; i < A.RowCount; i += stride)
        {
            for (int j = 0; j < A.ColumnCount; j += stride)
            {
                writer.Write(A[i, j]);
            }
        }
        
        // Hash RHS vector (sample every 10th element for large vectors)
        int vectorStride = Math.Max(1, b.Count / 100);
        for (int i = 0; i < b.Count; i += vectorStride)
        {
            writer.Write(b[i]);
        }
        
        ms.Position = 0;
        byte[] hashBytes = sha256.ComputeHash(ms);
        return Convert.ToBase64String(hashBytes);
    }
    
    /// <summary>
    /// Compute hash of matrix only (for detecting matrix changes)
    /// </summary>
    private string ComputeMatrixHash(Matrix<double> A)
    {
        using var sha256 = SHA256.Create();
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        // Hash matrix dimensions
        writer.Write(A.RowCount);
        writer.Write(A.ColumnCount);
        
        // Hash matrix elements (sample for performance)
        int stride = Math.Max(1, A.RowCount / 100);
        for (int i = 0; i < A.RowCount; i += stride)
        {
            for (int j = 0; j < A.ColumnCount; j += stride)
            {
                writer.Write(A[i, j]);
            }
        }
        
        ms.Position = 0;
        byte[] hashBytes = sha256.ComputeHash(ms);
        return Convert.ToBase64String(hashBytes);
    }
    
    /// <summary>
    /// Clear all cached data (useful when switching to a completely different problem)
    /// </summary>
    public void ClearCache()
    {
        _cachedSolution = null;
        _cachedProblemHash = null;
        _cachedMatrix = null;
        _cachedMatrixHash = null;
    }
    
    /// <summary>
    /// Get cache statistics for debugging/monitoring
    /// </summary>
    public (bool HasCachedSolution, bool HasCachedMatrix, int? CachedDimension) GetCacheStats()
    {
        return (
            _cachedSolution != null,
            _cachedMatrix != null,
            _cachedSolution?.Count
        );
    }
}

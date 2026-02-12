using CAD2DModel.Geometry;
using Examine2DModel.Stress;

namespace Examine2DModel.Analysis;

/// <summary>
/// Type of plane strain analysis
/// </summary>
public enum PlaneStrainType
{
    PlaneStrain,
    CompletePlaneStrain
}

/// <summary>
/// Type of boundary element
/// </summary>
public enum ElementType
{
    Constant,
    Linear,
    Quadratic
}

/// <summary>
/// Solver options for BEM analysis
/// </summary>
public class SolverOptions
{
    public PlaneStrainType PlaneStrainType { get; set; } = PlaneStrainType.PlaneStrain;
    public ElementType ElementType { get; set; } = ElementType.Linear;
    public int NumberOfElements { get; set; } = 100;
    public double Tolerance { get; set; } = 1e-6;
    public int MaxIterations { get; set; } = 1000;
}

/// <summary>
/// Boundary configuration for BEM analysis
/// </summary>
public class BoundaryConfiguration
{
    public List<Boundary> Boundaries { get; init; } = new();
    public StressGrid StressGrid { get; init; } = new();
    // Additional configuration properties
}

/// <summary>
/// Interface for boundary element method solver
/// </summary>
public interface IBoundaryElementSolver
{
    /// <summary>
    /// Solve the BEM system for given boundaries and options
    /// </summary>
    StressField Solve(BoundaryConfiguration config, SolverOptions options);
    
    /// <summary>
    /// Solve asynchronously
    /// </summary>
    Task<StressField> SolveAsync(BoundaryConfiguration config, SolverOptions options, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if the solver can handle the given configuration
    /// </summary>
    bool CanSolve(BoundaryConfiguration config);
}

/// <summary>
/// Interface for matrix solvers (direct, iterative, etc.)
/// </summary>
public interface IMatrixSolver
{
    /// <summary>
    /// Solve a linear system Ax = b
    /// </summary>
    double[] Solve(double[,] A, double[] b);
    
    /// <summary>
    /// Solve using sparse matrix format
    /// </summary>
    double[] SolveSparse(SparseMatrix A, double[] b);
}

/// <summary>
/// Placeholder for sparse matrix - will use MathNet.Numerics
/// </summary>
public class SparseMatrix
{
    // Will be implemented using MathNet.Numerics.LinearAlgebra.Double.SparseMatrix
}

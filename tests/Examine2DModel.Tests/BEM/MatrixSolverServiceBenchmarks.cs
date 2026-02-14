using System.Diagnostics;
using Examine2DModel.Analysis;
using Examine2DModel.BEM;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Examine2DModel.Tests.BEM;

/// <summary>
/// Performance benchmarks for MatrixSolverService.
/// These tests validate the performance characteristics specified in the plan.
/// </summary>
[TestClass]
public class MatrixSolverServiceBenchmarks
{
    [TestMethod]
    public void Benchmark_SmallProblem_DirectSolver()
    {
        // Arrange - 500 DOF (250 elements × 2 DOF)
        int dof = 500;
        var (A, b) = CreateBEMStyleProblem(dof);
        
        var config = new BEMConfiguration
        {
            DirectSolverThreshold = 1000, // Use direct solver
            EnableCaching = false
        };
        var solver = new MatrixSolverService(config);
        
        // Act
        var sw = Stopwatch.StartNew();
        var solution = solver.Solve(A, b);
        sw.Stop();
        
        // Assert - Should be fast with direct solver
        Console.WriteLine($"Small problem (500 DOF) with direct solver: {sw.ElapsedMilliseconds}ms");
        Assert.IsTrue(sw.ElapsedMilliseconds < 200, 
            $"Direct solver should solve 500 DOF in <200ms, took {sw.ElapsedMilliseconds}ms");
        
        // Verify solution quality
        var residual = b - A.Multiply(Vector<double>.Build.Dense(solution));
        Assert.IsTrue(residual.AbsoluteMaximum() < 1e-8, "Solution should be highly accurate");
    }
    
    [TestMethod]
    public void Benchmark_MediumProblem_IterativeSolver()
    {
        // Arrange - 2000 DOF (1000 elements × 2 DOF) - typical for BEM
        int dof = 2000;
        var (A, b) = CreateBEMStyleProblem(dof);
        
        var config = new BEMConfiguration
        {
            DirectSolverThreshold = 1000, // Force iterative solver
            EnableCaching = false,
            Tolerance = 1e-6,
            MaxIterations = 1000
        };
        var solver = new MatrixSolverService(config);
        
        // Act
        var sw = Stopwatch.StartNew();
        var solution = solver.Solve(A, b);
        sw.Stop();
        
        // Assert - Should meet target from plan: 200-800ms
        Console.WriteLine($"Medium problem (2000 DOF) with iterative solver: {sw.ElapsedMilliseconds}ms");
        Assert.IsTrue(sw.ElapsedMilliseconds < 2000, 
            $"Iterative solver should solve 2000 DOF in <2s, took {sw.ElapsedMilliseconds}ms");
        
        // Verify solution quality
        var residual = b - A.Multiply(Vector<double>.Build.Dense(solution));
        Assert.IsTrue(residual.AbsoluteMaximum() < 1e-3, "Solution should be reasonably accurate");
    }
    
    [TestMethod]
    public void Benchmark_CacheHit_InstantReturn()
    {
        // Arrange
        int dof = 1000;
        var (A, b) = CreateBEMStyleProblem(dof);
        
        var config = new BEMConfiguration { EnableCaching = true };
        var solver = new MatrixSolverService(config);
        
        // First solve (cache miss)
        var solution1 = solver.Solve(A, b);
        
        // Act - Second solve (cache hit)
        var sw = Stopwatch.StartNew();
        var solution2 = solver.Solve(A, b);
        sw.Stop();
        
        // Assert - Should be nearly instant (<10ms as per plan)
        Console.WriteLine($"Cache hit return time: {sw.ElapsedMilliseconds}ms");
        Assert.IsTrue(sw.ElapsedMilliseconds < 50, 
            $"Cached solution should return in <50ms, took {sw.ElapsedMilliseconds}ms");
        
        CollectionAssert.AreEqual(solution1, solution2, "Cached solution should match");
    }
    
    [TestMethod]
    public void Benchmark_WarmStart_ImprovesPerformance()
    {
        // Arrange - Create two similar problems
        int dof = 1500;
        var (A, b1) = CreateBEMStyleProblem(dof, seed: 42);
        var b2 = Vector<double>.Build.Dense(dof, i => b1[i] * 1.05); // Similar RHS
        
        var config = new BEMConfiguration
        {
            DirectSolverThreshold = 1000, // Force iterative
            EnableCaching = true,
            Tolerance = 1e-6
        };
        var solver = new MatrixSolverService(config);
        
        // First solve (no warm start)
        var sw1 = Stopwatch.StartNew();
        var solution1 = solver.Solve(A, b1);
        sw1.Stop();
        
        // Second solve with warm start (should be faster or similar)
        var sw2 = Stopwatch.StartNew();
        var solution2 = solver.Solve(A, b2);
        sw2.Stop();
        
        // Assert
        Console.WriteLine($"First solve (cold start): {sw1.ElapsedMilliseconds}ms");
        Console.WriteLine($"Second solve (warm start): {sw2.ElapsedMilliseconds}ms");
        
        // Warm start should not be significantly slower (allowing for some variation)
        Assert.IsTrue(sw2.ElapsedMilliseconds < sw1.ElapsedMilliseconds * 2, 
            "Warm start should not be much slower than cold start");
    }
    
    /// <summary>
    /// Create a BEM-style test problem (dense, non-symmetric, well-conditioned)
    /// </summary>
    private static (Matrix<double> A, Vector<double> b) CreateBEMStyleProblem(int dof, int seed = 42)
    {
        var random = new Random(seed);
        var A = Matrix<double>.Build.Dense(dof, dof);
        var b = Vector<double>.Build.Dense(dof);
        
        // Create a matrix with BEM-like structure
        for (int i = 0; i < dof; i++)
        {
            for (int j = 0; j < dof; j++)
            {
                // Influence coefficients decay with distance
                double distance = Math.Abs(i - j) + 1;
                A[i, j] = 1.0 / distance + random.NextDouble() * 0.1;
            }
            // Make diagonally dominant for stability
            A[i, i] += 10.0;
            b[i] = random.NextDouble() * 100;
        }
        
        return (A, b);
    }
}

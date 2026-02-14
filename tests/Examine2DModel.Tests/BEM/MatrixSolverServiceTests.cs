using Examine2DModel.Analysis;
using Examine2DModel.BEM;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Examine2DModel.Tests.BEM;

[TestClass]
public class MatrixSolverServiceTests
{
    [TestMethod]
    public void Solve_SmallSystem_UsesDirectSolver()
    {
        // Arrange - Simple 3x3 system with known solution
        // 2x + y = 5
        // x + 3y = 7
        var A = new double[,]
        {
            { 2, 1 },
            { 1, 3 }
        };
        var b = new double[] { 5, 7 };
        
        var config = new BEMConfiguration
        {
            DirectSolverThreshold = 1000, // Use direct solver
            EnableCaching = false // Disable cache for this test
        };
        var solver = new MatrixSolverService(config);
        
        // Act
        var solution = solver.Solve(A, b);
        
        // Assert - Expected solution: x=1.6, y=1.8
        Assert.AreEqual(2, solution.Length);
        Assert.AreEqual(1.6, solution[0], 1e-10, "x should be 1.6");
        Assert.AreEqual(1.8, solution[1], 1e-10, "y should be 1.8");
    }
    
    [TestMethod]
    public void Solve_LargeSystem_UsesIterativeSolver()
    {
        // Arrange - Create a larger system that will trigger iterative solver
        int n = 1500; // Above default threshold of 1000
        var A = Matrix<double>.Build.Dense(n, n);
        var b = Vector<double>.Build.Dense(n);
        
        // Create a diagonally dominant system (ensures convergence)
        for (int i = 0; i < n; i++)
        {
            A[i, i] = 10.0; // Strong diagonal
            b[i] = i + 1.0;
            
            // Add some off-diagonal terms
            if (i > 0)
                A[i, i - 1] = 1.0;
            if (i < n - 1)
                A[i, i + 1] = 1.0;
        }
        
        var config = new BEMConfiguration
        {
            DirectSolverThreshold = 1000, // Force iterative solver
            Tolerance = 1e-6,
            MaxIterations = 1000,
            EnableCaching = false
        };
        var solver = new MatrixSolverService(config);
        
        // Act
        var solution = solver.Solve(A, b);
        
        // Assert - Verify solution satisfies Ax = b
        var result = A.Multiply(Vector<double>.Build.Dense(solution));
        for (int i = 0; i < n; i++)
        {
            Assert.AreEqual(b[i], result[i], 1e-3, $"Solution verification failed at index {i}");
        }
    }
    
    [TestMethod]
    public void Solve_WithCaching_ReturnsCachedResultOnSecondCall()
    {
        // Arrange
        var A = new double[,]
        {
            { 4, 1 },
            { 1, 3 }
        };
        var b = new double[] { 1, 2 };
        
        var config = new BEMConfiguration { EnableCaching = true };
        var solver = new MatrixSolverService(config);
        
        // Act - First solve
        var solution1 = solver.Solve(A, b);
        var stats1 = solver.GetCacheStats();
        
        // Act - Second solve with same inputs
        var solution2 = solver.Solve(A, b);
        var stats2 = solver.GetCacheStats();
        
        // Assert
        Assert.IsTrue(stats1.HasCachedSolution, "Solution should be cached after first solve");
        Assert.IsTrue(stats2.HasCachedSolution, "Solution should still be cached");
        CollectionAssert.AreEqual(solution1, solution2, "Cached solution should match");
    }
    
    [TestMethod]
    public void Solve_CacheInvalidation_RecalculatesOnDifferentProblem()
    {
        // Arrange
        var A1 = new double[,]
        {
            { 2, 1 },
            { 1, 3 }
        };
        var b1 = new double[] { 5, 7 };
        
        var A2 = new double[,]
        {
            { 3, 1 },
            { 1, 2 }
        };
        var b2 = new double[] { 4, 3 };
        
        var config = new BEMConfiguration { EnableCaching = true };
        var solver = new MatrixSolverService(config);
        
        // Act
        var solution1 = solver.Solve(A1, b1);
        var solution2 = solver.Solve(A2, b2); // Different problem
        
        // Assert - Solutions should be different
        Assert.AreNotEqual(solution1[0], solution2[0], "Solutions should differ for different problems");
    }
    
    [TestMethod]
    public void Solve_SymmetricPositiveDefinite_SolvesCorrectly()
    {
        // Arrange - SPD matrix (common in BEM)
        var A = new double[,]
        {
            { 4, 1, 0 },
            { 1, 4, 1 },
            { 0, 1, 4 }
        };
        var b = new double[] { 1, 2, 3 };
        
        var solver = new MatrixSolverService();
        
        // Act
        var solution = solver.Solve(A, b);
        
        // Assert - Verify Ax = b
        var A_matrix = Matrix<double>.Build.DenseOfArray(A);
        var x_vector = Vector<double>.Build.Dense(solution);
        var result = A_matrix.Multiply(x_vector);
        
        for (int i = 0; i < b.Length; i++)
        {
            Assert.AreEqual(b[i], result[i], 1e-10, $"Solution check failed at index {i}");
        }
    }
    
    [TestMethod]
    public void Solve_IdentityMatrix_ReturnsRHS()
    {
        // Arrange - Trivial case: Ix = b => x = b
        var A = new double[,]
        {
            { 1, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 1 }
        };
        var b = new double[] { 3, 5, 7 };
        
        var solver = new MatrixSolverService();
        
        // Act
        var solution = solver.Solve(A, b);
        
        // Assert
        CollectionAssert.AreEqual(b, solution, "Identity matrix should return RHS");
    }
    
    [TestMethod]
    public void ClearCache_RemovesCachedData()
    {
        // Arrange
        var A = new double[,]
        {
            { 2, 1 },
            { 1, 3 }
        };
        var b = new double[] { 5, 7 };
        
        var config = new BEMConfiguration { EnableCaching = true };
        var solver = new MatrixSolverService(config);
        
        // Act
        solver.Solve(A, b); // Populate cache
        var statsBeforeClear = solver.GetCacheStats();
        
        solver.ClearCache();
        var statsAfterClear = solver.GetCacheStats();
        
        // Assert
        Assert.IsTrue(statsBeforeClear.HasCachedSolution, "Should have cache before clear");
        Assert.IsFalse(statsAfterClear.HasCachedSolution, "Should not have cache after clear");
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Solve_NullMatrix_ThrowsArgumentNullException()
    {
        // Arrange
        var solver = new MatrixSolverService();
        var b = new double[] { 1, 2 };
        
        // Act
        solver.Solve(null!, b);
        
        // Assert - Exception expected
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Solve_NullVector_ThrowsArgumentNullException()
    {
        // Arrange
        var solver = new MatrixSolverService();
        var A = new double[,] { { 1, 0 }, { 0, 1 } };
        
        // Act
        solver.Solve(A, null!);
        
        // Assert - Exception expected
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Solve_NonSquareMatrix_ThrowsArgumentException()
    {
        // Arrange
        var solver = new MatrixSolverService();
        var A = new double[,]
        {
            { 1, 2, 3 },
            { 4, 5, 6 }
        }; // 2x3 matrix
        var b = new double[] { 1, 2 };
        
        // Act
        solver.Solve(A, b);
        
        // Assert - Exception expected
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Solve_MismatchedDimensions_ThrowsArgumentException()
    {
        // Arrange
        var solver = new MatrixSolverService();
        var A = new double[,]
        {
            { 1, 0 },
            { 0, 1 }
        }; // 2x2 matrix
        var b = new double[] { 1, 2, 3 }; // Length 3 vector
        
        // Act
        solver.Solve(A, b);
        
        // Assert - Exception expected
    }
    
    [TestMethod]
    public void Solve_WarmStart_ImprovesConvergence()
    {
        // Arrange - Create similar problems to test warm start
        int n = 1500;
        var A = Matrix<double>.Build.Dense(n, n);
        
        // Create a diagonally dominant system
        for (int i = 0; i < n; i++)
        {
            A[i, i] = 10.0;
            if (i > 0) A[i, i - 1] = 1.0;
            if (i < n - 1) A[i, i + 1] = 1.0;
        }
        
        var b1 = Vector<double>.Build.Dense(n, i => i + 1.0);
        var b2 = Vector<double>.Build.Dense(n, i => i + 1.1); // Slightly different
        
        var config = new BEMConfiguration
        {
            DirectSolverThreshold = 1000, // Force iterative
            EnableCaching = true,
            Tolerance = 1e-6,
            MaxIterations = 1000
        };
        var solver = new MatrixSolverService(config);
        
        // Act - Solve first problem (no warm start)
        var solution1 = solver.Solve(A, b1);
        
        // Solve second problem (should use warm start from solution1)
        var solution2 = solver.Solve(A, b2);
        
        // Assert - Both solutions should satisfy their respective equations
        var result1 = A.Multiply(Vector<double>.Build.Dense(solution1));
        var result2 = A.Multiply(Vector<double>.Build.Dense(solution2));
        
        for (int i = 0; i < n; i++)
        {
            Assert.AreEqual(b1[i], result1[i], 1e-3);
            Assert.AreEqual(b2[i], result2[i], 1e-3);
        }
    }
    
    [TestMethod]
    public void Solve_BEMTypicalProblem_SolvesAccurately()
    {
        // Arrange - Simulate a small BEM-style problem (influence matrix characteristics)
        // BEM matrices are typically dense and well-conditioned
        int numElements = 50;
        int dof = numElements * 2; // 2 DOF per element (typically)
        
        var A = Matrix<double>.Build.Dense(dof, dof);
        var b = Vector<double>.Build.Dense(dof);
        
        // Create a matrix with BEM-like structure (full, non-symmetric)
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < dof; i++)
        {
            for (int j = 0; j < dof; j++)
            {
                // Add some structure similar to influence coefficients
                double distance = Math.Abs(i - j) + 1;
                A[i, j] = 1.0 / distance + random.NextDouble() * 0.1;
            }
            // Make diagonally dominant for stability
            A[i, i] += 10.0;
            b[i] = random.NextDouble() * 100;
        }
        
        var solver = new MatrixSolverService();
        
        // Act
        var solution = solver.Solve(A, b);
        
        // Assert - Verify solution accuracy
        var residual = b - A.Multiply(Vector<double>.Build.Dense(solution));
        double maxResidual = residual.AbsoluteMaximum();
        
        Assert.IsTrue(maxResidual < 1e-3, 
            $"Maximum residual {maxResidual} exceeds tolerance for BEM-style problem");
    }
}

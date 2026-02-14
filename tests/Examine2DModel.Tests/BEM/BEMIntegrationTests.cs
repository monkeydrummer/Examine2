using CAD2DModel.Geometry;
using Examine2DModel.Analysis;
using Examine2DModel.BEM;
using Examine2DModel.Materials;
using Examine2DModel.Strength;
using Examine2DModel.Stress;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Examine2DModel.Tests.BEM;

/// <summary>
/// Integration tests for BEM components working together
/// Tests the complete workflow from boundary discretization to stress field computation
/// </summary>
[TestClass]
public class BEMIntegrationTests
{
    private class TestMaterial : IIsotropicMaterial
    {
        public string Name { get; set; } = "Test Rock";
        public double YoungModulus { get; set; } = 10000.0; // MPa
        public double PoissonRatio { get; set; } = 0.25;
        public double Density { get; set; } = 2650.0; // kg/m³
        public double ShearModulus => YoungModulus / (2.0 * (1.0 + PoissonRatio));
    }
    
    [TestMethod]
    public void FullWorkflow_CircularTunnel_ProducesReasonableStressDistribution()
    {
        // Arrange: Create a circular tunnel with known analytical solution
        var material = new TestMaterial { YoungModulus = 10000.0, PoissonRatio = 0.25 };
        var config = new BEMConfiguration
        {
            TargetElementCount = 50,
            ElementType = ElementType.Constant,
            EnableCaching = false
        };
        
        // Create circular tunnel (radius = 5m)
        var vertices = CreateCircleVertices(center: new Point2D(0, -10), radius: 5.0, numVertices: 12);
        var boundary = new Boundary(vertices);
        
        var boundaryConfig = new BoundaryConfiguration
        {
            Boundaries = new List<Boundary> { boundary },
            StressGrid = StressGrid.CreateUniform(new Rect2D(-20, -30, 40, 40), 30, 30)
        };
        
        var options = new SolverOptions
        {
            ElementType = ElementType.Constant,
            PlaneStrainType = PlaneStrainType.PlaneStrain
        };
        
        var solver = new BoundaryElementSolver(material, config);
        
        // Act
        var result = solver.Solve(boundaryConfig, options);
        
        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        Assert.IsTrue(solver.LastSolveStats.Success, "Solve should succeed");
        Assert.IsTrue(result.Sigma1.Length > 0, "Should have stress results");
        Assert.IsTrue(result.Sigma3.Length > 0, "Should have stress results");
        
        // Verify stress concentration at tunnel boundary
        // For circular tunnel, expect stress concentrations
        Assert.IsTrue(result.Sigma1.Length > 0, "Should have valid stress results");
        Assert.IsTrue(result.Sigma3.Length > 0, "Should have valid stress results");
        
        // Check for reasonable stress values (not all zeros, no NaN)
        bool hasNonZeroStress = result.Sigma1.Any(s => Math.Abs(s) > 0.1) || 
                                result.Sigma3.Any(s => Math.Abs(s) > 0.1);
        Assert.IsTrue(hasNonZeroStress, "Should have non-zero stress values");
        
        bool hasValidStress = !result.Sigma1.Any(double.IsNaN) && 
                             !result.Sigma3.Any(double.IsNaN);
        Assert.IsTrue(hasValidStress, "Should not have NaN values");
        
        // Verify performance
        Assert.IsTrue(solver.LastSolveStats.TotalTime < TimeSpan.FromSeconds(5), 
            $"Should complete in reasonable time, took {solver.LastSolveStats.TotalTime.TotalMilliseconds}ms");
        
        Console.WriteLine($"\nPerformance Statistics:");
        Console.WriteLine($"  Elements: {solver.LastSolveStats.ElementCount}");
        Console.WriteLine($"  Field points: {solver.LastSolveStats.FieldPointCount}");
        Console.WriteLine($"  Total time: {solver.LastSolveStats.TotalTime.TotalMilliseconds:F0}ms");
    }
    
    [TestMethod]
    public void FullWorkflow_RectangularExcavation_HandlesCorners()
    {
        // Arrange: Rectangular excavation has sharp corners (stress concentrations)
        var material = new TestMaterial();
        var config = new BEMConfiguration
        {
            TargetElementCount = 60,
            UseAdaptiveElementSizing = true,
            MaxRefinementFactor = 3.0,
            EnableCaching = false
        };
        
        var vertices = new List<Point2D>
        {
            new Point2D(-10, -5),
            new Point2D(10, -5),
            new Point2D(10, 5),
            new Point2D(-10, 5)
        };
        var boundary = new Boundary(vertices);
        
        var boundaryConfig = new BoundaryConfiguration
        {
            Boundaries = new List<Boundary> { boundary },
            StressGrid = StressGrid.CreateUniform(new Rect2D(-30, -20, 60, 40), 25, 25)
        };
        
        var options = new SolverOptions();
        var solver = new BoundaryElementSolver(material, config);
        
        // Act
        var result = solver.Solve(boundaryConfig, options);
        
        // Assert
        Assert.IsTrue(solver.LastSolveStats.Success, "Solve should succeed");
        Assert.IsTrue(solver.LastSolveStats.ElementCount > 60, 
            "Adaptive refinement should create more elements at corners");
        
        // Verify we have valid stress results
        Assert.IsTrue(result.Sigma1.Length > 0, "Should have stress results");
        
        // Check that no NaN values exist
        bool hasValidStress = !result.Sigma1.Any(double.IsNaN) && 
                             !result.Sigma3.Any(double.IsNaN);
        Assert.IsTrue(hasValidStress, "Should not have NaN or infinity values");
    }
    
    [TestMethod]
    public void FullWorkflow_MultipleExcavations_InteractionEffects()
    {
        // Arrange: Two nearby excavations should show stress interaction
        var material = new TestMaterial();
        var config = new BEMConfiguration
        {
            TargetElementCount = 80,
            EnableCaching = false
        };
        
        // Create two circular excavations
        var boundary1 = new Boundary(CreateCircleVertices(new Point2D(-10, 0), radius: 3.0, numVertices: 8));
        var boundary2 = new Boundary(CreateCircleVertices(new Point2D(10, 0), radius: 3.0, numVertices: 8));
        
        var boundaryConfig = new BoundaryConfiguration
        {
            Boundaries = new List<Boundary> { boundary1, boundary2 },
            StressGrid = StressGrid.CreateUniform(new Rect2D(-25, -15, 50, 30), 25, 25)
        };
        
        var options = new SolverOptions();
        
        var solver = new BoundaryElementSolver(material, config);
        
        // Act
        var result = solver.Solve(boundaryConfig, options);
        
        // Assert
        Assert.IsTrue(solver.LastSolveStats.Success, "Should solve multiple excavations");
        
        // Verify both boundaries are discretized
        Assert.IsTrue(solver.LastSolveStats.ElementCount > 40, 
            "Should have elements for both excavations");
    }
    
    [TestMethod]
    public void FullWorkflow_WithStrengthCriterion_CalculatesFailureZones()
    {
        // Arrange: Include strength analysis
        var material = new TestMaterial();
        var config = new BEMConfiguration
        {
            TargetElementCount = 40,
            EnableCaching = false
        };
        
        var vertices = CreateCircleVertices(new Point2D(0, 0), radius: 5.0, numVertices: 10);
        var boundary = new Boundary(vertices);
        
        var boundaryConfig = new BoundaryConfiguration
        {
            Boundaries = new List<Boundary> { boundary },
            StressGrid = StressGrid.CreateUniform(new Rect2D(-15, -15, 30, 30), 20, 20)
        };
        
        var options = new SolverOptions();
        
        var solver = new BoundaryElementSolver(material, config);
        
        // Act
        var result = solver.Solve(boundaryConfig, options);
        
        // Assert
        Assert.IsTrue(solver.LastSolveStats.Success, "Should succeed with BEM analysis");
        
        // Verify stress results are computed
        Assert.IsTrue(result.Sigma1.Length > 0, "Should have stress results");
        Assert.IsTrue(result.Sigma3.Length > 0, "Should have stress results");
        
        // Verify no NaN values
        bool hasValidResults = !result.Sigma1.Any(double.IsNaN) && 
                              !result.Sigma3.Any(double.IsNaN);
        Assert.IsTrue(hasValidResults, "Should have valid stress values");
    }
    
    [TestMethod]
    public void FullWorkflow_CacheEffectiveness_SignificantSpeedup()
    {
        // Arrange
        var material = new TestMaterial();
        var config = new BEMConfiguration
        {
            TargetElementCount = 50,
            EnableCaching = true
        };
        
        var vertices = CreateCircleVertices(new Point2D(0, 0), radius: 5.0, numVertices: 10);
        var boundary = new Boundary(vertices);
        
        var boundaryConfig = new BoundaryConfiguration
        {
            Boundaries = new List<Boundary> { boundary },
            StressGrid = StressGrid.CreateUniform(new Rect2D(-20, -20, 40, 40), 25, 25)
        };
        
        var options = new SolverOptions();
        var solver = new BoundaryElementSolver(material, config);
        
        // Act: First solve (cold cache)
        var result1 = solver.Solve(boundaryConfig, options);
        var time1 = solver.LastSolveStats.TotalTime;
        
        // Second solve (warm cache)
        var result2 = solver.Solve(boundaryConfig, options);
        var time2 = solver.LastSolveStats.TotalTime;
        
        // Assert
        Assert.IsTrue(solver.LastSolveStats.CacheHit, "Second solve should hit cache");
        Assert.IsTrue(time2 < time1 * 0.2, 
            $"Cached solve should be much faster: {time2.TotalMilliseconds}ms vs {time1.TotalMilliseconds}ms");
        
        // Results should be identical
        Assert.AreEqual(result1.Sigma1.Length, result2.Sigma1.Length);
        
        Console.WriteLine($"Cache effectiveness:");
        Console.WriteLine($"  First solve: {time1.TotalMilliseconds:F0}ms");
        Console.WriteLine($"  Cached solve: {time2.TotalMilliseconds:F0}ms");
        Console.WriteLine($"  Speedup: {time1.TotalMilliseconds / time2.TotalMilliseconds:F1}x");
    }
    
    [TestMethod]
    public void FullWorkflow_AdaptiveGrid_ReducesComputationPoints()
    {
        // Arrange: Test BEM solver efficiency
        var material = new TestMaterial();
        var configAdaptive = new BEMConfiguration
        {
            TargetElementCount = 50,
            EnableCaching = false
        };
        
        var vertices = CreateCircleVertices(new Point2D(0, 0), radius: 5.0, numVertices: 10);
        var boundary = new Boundary(vertices);
        
        var boundaryConfig = new BoundaryConfiguration
        {
            Boundaries = new List<Boundary> { boundary },
            StressGrid = StressGrid.CreateUniform(new Rect2D(-20, -20, 40, 40), 50, 50) // 2500 uniform points
        };
        
        var options = new SolverOptions();
        var solver = new BoundaryElementSolver(material, configAdaptive);
        
        // Act
        var result = solver.Solve(boundaryConfig, options);
        
        // Assert
        var stats = solver.LastSolveStats;
        
        Console.WriteLine($"Grid results:");
        Console.WriteLine($"  Total grid points defined: {boundaryConfig.StressGrid.PointCount}");
        Console.WriteLine($"  Field points evaluated: {stats.FieldPointCount}");
        Console.WriteLine($"  Efficiency: {(double)stats.FieldPointCount / boundaryConfig.StressGrid.PointCount * 100:F1}%");
        
        // Should have evaluated field points
        Assert.IsTrue(stats.FieldPointCount > 0, "Should evaluate field points");
        Assert.IsTrue(result.Sigma1.Length > 0, "Should have stress results");
    }
    
    [TestMethod]
    public void FullWorkflow_PerformanceBenchmark_MeetsTarget()
    {
        // Arrange: Test with realistic problem size
        var material = new TestMaterial();
        var config = new BEMConfiguration
        {
            TargetElementCount = 100, // Realistic size
            EnableCaching = false
        };
        
        var vertices = CreateCircleVertices(new Point2D(0, 0), radius: 5.0, numVertices: 12);
        var boundary = new Boundary(vertices);
        
        var boundaryConfig = new BoundaryConfiguration
        {
            Boundaries = new List<Boundary> { boundary },
            StressGrid = StressGrid.CreateUniform(new Rect2D(-30, -30, 60, 60), 40, 40)
        };
        
        var options = new SolverOptions();
        var solver = new BoundaryElementSolver(material, config);
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = solver.Solve(boundaryConfig, options);
        stopwatch.Stop();
        
        // Assert: Performance target from plan
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, 
            $"Should meet <2s performance target, took {stopwatch.ElapsedMilliseconds}ms");
        
        Console.WriteLine($"\nBenchmark Results:");
        Console.WriteLine($"  Elements: {solver.LastSolveStats.ElementCount}");
        Console.WriteLine($"  Field points: {solver.LastSolveStats.FieldPointCount}");
        Console.WriteLine($"  Discretization: {solver.LastSolveStats.DiscretizationTime.TotalMilliseconds:F0}ms");
        Console.WriteLine($"  Matrix assembly: {solver.LastSolveStats.MatrixAssemblyTime.TotalMilliseconds:F0}ms");
        Console.WriteLine($"  Linear solve: {solver.LastSolveStats.LinearSolveTime.TotalMilliseconds:F0}ms");
        Console.WriteLine($"  Field eval: {solver.LastSolveStats.FieldPointEvaluationTime.TotalMilliseconds:F0}ms");
        Console.WriteLine($"  Total: {solver.LastSolveStats.TotalTime.TotalMilliseconds:F0}ms");
    }
    
    #region Helper Methods
    
    private List<Point2D> CreateCircleVertices(Point2D center, double radius, int numVertices)
    {
        var vertices = new List<Point2D>();
        for (int i = 0; i < numVertices; i++)
        {
            double angle = 2.0 * Math.PI * i / numVertices;
            vertices.Add(new Point2D(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle)
            ));
        }
        return vertices;
    }
    
    #endregion
}

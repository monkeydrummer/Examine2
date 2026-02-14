using CAD2DModel.Geometry;
using Examine2DModel.Analysis;
using Examine2DModel.BEM;
using Examine2DModel.Materials;
using Examine2DModel.Stress;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Examine2DModel.Tests.BEM;

[TestClass]
public class BoundaryElementSolverTests
{
    private class TestMaterial : IIsotropicMaterial
    {
        public string Name { get; set; } = "Test Rock";
        public double YoungModulus { get; set; } = 10000.0; // 10 GPa
        public double PoissonRatio { get; set; } = 0.25;
        public double Density { get; set; } = 2650.0; // kg/m³ (typical rock)
        public double ShearModulus => YoungModulus / (2.0 * (1.0 + PoissonRatio));
        public double BulkModulus => YoungModulus / (3.0 * (1.0 - 2.0 * PoissonRatio));
    }
    
    private IIsotropicMaterial CreateTestMaterial()
    {
        return new TestMaterial
        {
            YoungModulus = 10000.0, // MPa
            PoissonRatio = 0.25
        };
    }
    
    private BoundaryConfiguration CreateSimpleCircularExcavation()
    {
        // Create a circular excavation with 12 vertices
        var vertices = new List<Point2D>();
        int numVertices = 12;
        double radius = 5.0;
        
        for (int i = 0; i < numVertices; i++)
        {
            double angle = 2.0 * Math.PI * i / numVertices;
            vertices.Add(new Point2D(
                radius * Math.Cos(angle),
                radius * Math.Sin(angle)
            ));
        }
        
        var boundary = new Boundary(vertices);
        
        return new BoundaryConfiguration
        {
            Boundaries = new List<Boundary> { boundary },
            StressGrid = StressGrid.CreateUniform(
                new Rect2D(-20, -20, 40, 40),
                50, 50
            )
        };
    }
    
    private BoundaryConfiguration CreateRectangularExcavation()
    {
        var vertices = new List<Point2D>
        {
            new Point2D(-10, -5),
            new Point2D(10, -5),
            new Point2D(10, 5),
            new Point2D(-10, 5)
        };
        
        var boundary = new Boundary(vertices);
        
        return new BoundaryConfiguration
        {
            Boundaries = new List<Boundary> { boundary },
            StressGrid = StressGrid.CreateUniform(
                new Rect2D(-30, -20, 60, 40),
                50, 50
            )
        };
    }
    
    [TestMethod]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var material = CreateTestMaterial();
        var config = new BEMConfiguration();
        
        // Act
        var solver = new BoundaryElementSolver(material, config);
        
        // Assert
        Assert.IsNotNull(solver);
    }
    
    [TestMethod]
    public void Constructor_WithNullMaterial_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new BoundaryElementSolver(null!, new BEMConfiguration()));
    }
    
    [TestMethod]
    public void CanSolve_WithValidConfiguration_ReturnsTrue()
    {
        // Arrange
        var material = CreateTestMaterial();
        var solver = new BoundaryElementSolver(material);
        var config = CreateSimpleCircularExcavation();
        
        // Act
        bool canSolve = solver.CanSolve(config);
        
        // Assert
        Assert.IsTrue(canSolve);
    }
    
    [TestMethod]
    public void CanSolve_WithNullConfiguration_ReturnsFalse()
    {
        // Arrange
        var material = CreateTestMaterial();
        var solver = new BoundaryElementSolver(material);
        
        // Act
        bool canSolve = solver.CanSolve(null!);
        
        // Assert
        Assert.IsFalse(canSolve);
    }
    
    [TestMethod]
    public void CanSolve_WithEmptyBoundaries_ReturnsFalse()
    {
        // Arrange
        var material = CreateTestMaterial();
        var solver = new BoundaryElementSolver(material);
        var config = new BoundaryConfiguration
        {
            Boundaries = new List<Boundary>()
        };
        
        // Act
        bool canSolve = solver.CanSolve(config);
        
        // Assert
        Assert.IsFalse(canSolve);
    }
    
    [TestMethod]
    public void Solve_CircularExcavation_ReturnsValidStressField()
    {
        // Arrange
        var material = CreateTestMaterial();
        var bemConfig = new BEMConfiguration
        {
            TargetElementCount = 50, // Small for fast test
            EnableCaching = false // Disable caching for this test
        };
        var solver = new BoundaryElementSolver(material, bemConfig);
        var config = CreateSimpleCircularExcavation();
        var options = new SolverOptions
        {
            ElementType = ElementType.Constant,
            PlaneStrainType = PlaneStrainType.PlaneStrain
        };
        
        // Act
        var result = solver.Solve(config, options);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Grid);
        Assert.IsNotNull(result.Sigma1);
        Assert.IsNotNull(result.Sigma3);
        Assert.IsTrue(result.Sigma1.Length > 0);
        Assert.IsTrue(solver.LastSolveStats.Success);
        Assert.IsTrue(solver.LastSolveStats.ElementCount > 0);
    }
    
    [TestMethod]
    public void Solve_RectangularExcavation_ReturnsValidStressField()
    {
        // Arrange
        var material = CreateTestMaterial();
        var bemConfig = new BEMConfiguration
        {
            TargetElementCount = 40,
            EnableCaching = false
        };
        var solver = new BoundaryElementSolver(material, bemConfig);
        var config = CreateRectangularExcavation();
        var options = new SolverOptions();
        
        // Act
        var result = solver.Solve(config, options);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(solver.LastSolveStats.Success);
        Assert.IsTrue(solver.LastSolveStats.ElementCount > 0);
    }
    
    [TestMethod]
    public void Solve_WithCachingEnabled_FirstSolveIsSlow()
    {
        // Arrange
        var material = CreateTestMaterial();
        var bemConfig = new BEMConfiguration
        {
            TargetElementCount = 50,
            EnableCaching = true
        };
        var solver = new BoundaryElementSolver(material, bemConfig);
        var config = CreateSimpleCircularExcavation();
        var options = new SolverOptions();
        
        // Act
        var result1 = solver.Solve(config, options);
        
        // Assert
        Assert.IsNotNull(result1);
        Assert.IsFalse(solver.LastSolveStats.CacheHit);
        Assert.IsTrue(solver.LastSolveStats.TotalTime > TimeSpan.FromMilliseconds(10));
    }
    
    [TestMethod]
    public void Solve_WithCachingEnabled_SecondSolveIsFast()
    {
        // Arrange
        var material = CreateTestMaterial();
        var bemConfig = new BEMConfiguration
        {
            TargetElementCount = 50,
            EnableCaching = true
        };
        var solver = new BoundaryElementSolver(material, bemConfig);
        var config = CreateSimpleCircularExcavation();
        var options = new SolverOptions();
        
        // Act
        var result1 = solver.Solve(config, options);
        var time1 = solver.LastSolveStats.TotalTime;
        
        var result2 = solver.Solve(config, options);
        var time2 = solver.LastSolveStats.TotalTime;
        
        // Assert
        Assert.IsNotNull(result2);
        Assert.IsTrue(solver.LastSolveStats.CacheHit);
        Assert.IsTrue(time2 < time1 * 0.1, $"Second solve should be much faster: {time2.TotalMilliseconds}ms vs {time1.TotalMilliseconds}ms");
    }
    
    [TestMethod]
    public void Solve_WithDifferentBoundaries_InvalidatesCache()
    {
        // Arrange
        var material = CreateTestMaterial();
        var bemConfig = new BEMConfiguration
        {
            TargetElementCount = 50,
            EnableCaching = true
        };
        var solver = new BoundaryElementSolver(material, bemConfig);
        var config1 = CreateSimpleCircularExcavation();
        var config2 = CreateRectangularExcavation();
        var options = new SolverOptions();
        
        // Act
        var result1 = solver.Solve(config1, options);
        var cacheHit1 = solver.LastSolveStats.CacheHit;
        
        var result2 = solver.Solve(config2, options);
        var cacheHit2 = solver.LastSolveStats.CacheHit;
        
        // Assert
        Assert.IsFalse(cacheHit1, "First solve should not be cache hit");
        Assert.IsFalse(cacheHit2, "Different configuration should invalidate cache");
    }
    
    [TestMethod]
    public void Solve_PerformanceTest_CompletesUnderTwoSeconds()
    {
        // Arrange
        var material = CreateTestMaterial();
        var bemConfig = new BEMConfiguration
        {
            TargetElementCount = 100, // Realistic size
            EnableCaching = false
        };
        var solver = new BoundaryElementSolver(material, bemConfig);
        var config = CreateSimpleCircularExcavation();
        var options = new SolverOptions();
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = solver.Solve(config, options);
        stopwatch.Stop();
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000, 
            $"Solve should complete under 2 seconds, took {stopwatch.ElapsedMilliseconds}ms");
        
        // Print statistics for debugging
        Console.WriteLine(solver.LastSolveStats.ToString());
    }
    
    [TestMethod]
    public void ClearCache_InvalidatesAllCaches()
    {
        // Arrange
        var material = CreateTestMaterial();
        var bemConfig = new BEMConfiguration { EnableCaching = true };
        var solver = new BoundaryElementSolver(material, bemConfig);
        var config = CreateSimpleCircularExcavation();
        var options = new SolverOptions();
        
        // Solve once to populate cache
        solver.Solve(config, options);
        var (hasElements, hasMatrix, hasResults) = solver.GetCacheStatus();
        Assert.IsTrue(hasResults, "Should have cached results after first solve");
        
        // Act
        solver.ClearCache();
        var (hasElements2, hasMatrix2, hasResults2) = solver.GetCacheStatus();
        
        // Assert
        Assert.IsFalse(hasResults2, "Cache should be cleared");
    }
    
    [TestMethod]
    public async Task SolveAsync_ReturnsValidResult()
    {
        // Arrange
        var material = CreateTestMaterial();
        var bemConfig = new BEMConfiguration { TargetElementCount = 50, EnableCaching = false };
        var solver = new BoundaryElementSolver(material, bemConfig);
        var config = CreateSimpleCircularExcavation();
        var options = new SolverOptions();
        
        // Act
        var result = await solver.SolveAsync(config, options);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(solver.LastSolveStats.Success);
    }
    
    [TestMethod]
    public void Solve_WithAdaptiveElementSizing_CreatesMoreElementsAtCorners()
    {
        // Arrange
        var material = CreateTestMaterial();
        var bemConfigAdaptive = new BEMConfiguration
        {
            TargetElementCount = 50,
            UseAdaptiveElementSizing = true,
            MaxRefinementFactor = 4.0,
            EnableCaching = false
        };
        var bemConfigUniform = new BEMConfiguration
        {
            TargetElementCount = 50,
            UseAdaptiveElementSizing = false,
            EnableCaching = false
        };
        
        var solverAdaptive = new BoundaryElementSolver(material, bemConfigAdaptive, new MatrixSolverService());
        var solverUniform = new BoundaryElementSolver(material, bemConfigUniform, new MatrixSolverService());
        var config = CreateRectangularExcavation(); // Rectangle has sharp corners
        var options = new SolverOptions();
        
        // Act
        var resultAdaptive = solverAdaptive.Solve(config, options);
        var resultUniform = solverUniform.Solve(config, options);
        
        // Assert - adaptive should create more elements
        Assert.IsTrue(solverAdaptive.LastSolveStats.ElementCount >= solverUniform.LastSolveStats.ElementCount,
            $"Adaptive: {solverAdaptive.LastSolveStats.ElementCount}, Uniform: {solverUniform.LastSolveStats.ElementCount}");
    }
    
    [TestMethod]
    public void Solve_MultipleExcavations_HandlesCorrectly()
    {
        // Arrange
        var material = CreateTestMaterial();
        var bemConfig = new BEMConfiguration { TargetElementCount = 100, EnableCaching = false };
        var solver = new BoundaryElementSolver(material, bemConfig);
        
        // Create two circular excavations
        var boundary1 = new Boundary(CreateCircleVertices(new Point2D(-10, 0), 3, 8));
        var boundary2 = new Boundary(CreateCircleVertices(new Point2D(10, 0), 3, 8));
        
        var config = new BoundaryConfiguration
        {
            Boundaries = new List<Boundary> { boundary1, boundary2 },
            StressGrid = StressGrid.CreateUniform(new Rect2D(-30, -20, 60, 40), 50, 50)
        };
        var options = new SolverOptions();
        
        // Act
        var result = solver.Solve(config, options);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(solver.LastSolveStats.Success);
        Assert.IsTrue(solver.LastSolveStats.ElementCount > 0);
    }
    
    [TestMethod]
    public void LastSolveStats_ContainsPerformanceBreakdown()
    {
        // Arrange
        var material = CreateTestMaterial();
        var solver = new BoundaryElementSolver(material);
        var config = CreateSimpleCircularExcavation();
        var options = new SolverOptions();
        
        // Act
        solver.Solve(config, options);
        var stats = solver.LastSolveStats;
        
        // Assert
        Assert.IsTrue(stats.Success);
        Assert.IsTrue(stats.DiscretizationTime > TimeSpan.Zero);
        Assert.IsTrue(stats.MatrixAssemblyTime >= TimeSpan.Zero); // Can be zero if cached
        Assert.IsTrue(stats.LinearSolveTime > TimeSpan.Zero);
        Assert.IsTrue(stats.FieldPointEvaluationTime > TimeSpan.Zero);
        Assert.IsTrue(stats.TotalTime > TimeSpan.Zero);
        
        // Print for debugging
        Console.WriteLine(stats.ToString());
    }
    
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
}

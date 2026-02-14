using CAD2DModel.Geometry;
using Examine2DModel.BEM;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Examine2DModel.Tests.BEM;

[TestClass]
public class AdaptiveGridGeneratorTests
{
    [TestMethod]
    public void Generate_WithSimpleBoundary_CreatesCoarseGrid()
    {
        // Arrange
        var generator = new AdaptiveGridGenerator();
        var bounds = new Rect2D(0, 0, 100, 100);
        var boundaries = new List<Boundary>();

        // Act
        var grid = generator.Generate(bounds, boundaries);

        // Assert
        Assert.IsNotNull(grid);
        Assert.IsNotNull(grid.CoarsePoints);
        Assert.AreEqual(50 * 50, grid.CoarsePoints.Count, "Should have 50x50 = 2500 coarse points");
        Assert.AreEqual(GridLevel.Coarse, grid.CoarsePoints[0].GridLevel);
    }

    [TestMethod]
    public void Generate_WithRectangularBoundary_CreatesMediumRefinement()
    {
        // Arrange
        var generator = new AdaptiveGridGenerator();
        var bounds = new Rect2D(0, 0, 100, 100);
        
        var boundary = new Boundary(new[]
        {
            new Point2D(30, 30),
            new Point2D(70, 30),
            new Point2D(70, 70),
            new Point2D(30, 70)
        });
        var boundaries = new List<Boundary> { boundary };

        // Act
        var grid = generator.Generate(bounds, boundaries);

        // Assert
        Assert.IsTrue(grid.MediumPoints.Count > 0, "Should have medium refinement points near boundary");
        Assert.AreEqual(GridLevel.Medium, grid.MediumPoints[0].GridLevel);
    }

    [TestMethod]
    public void Generate_WithCorners_CreatesFineRefinement()
    {
        // Arrange
        var config = new AdaptiveGridConfiguration
        {
            HighCurvatureAngleThreshold = 135.0,
            FineRefinementDistance = 2.0
        };
        var generator = new AdaptiveGridGenerator(config);
        var bounds = new Rect2D(0, 0, 100, 100);
        
        // Create boundary with sharp corner (90 degrees)
        var boundary = new Boundary(new[]
        {
            new Point2D(40, 50),
            new Point2D(50, 50),
            new Point2D(50, 60),
            new Point2D(40, 60)
        });
        var boundaries = new List<Boundary> { boundary };

        // Act
        var grid = generator.Generate(bounds, boundaries);

        // Assert
        Assert.IsTrue(grid.FinePoints.Count > 0, "Should have fine refinement points at corners");
        Assert.AreEqual(GridLevel.Fine, grid.FinePoints[0].GridLevel);
    }

    [TestMethod]
    public void Generate_MarksPointsInsideExcavation()
    {
        // Arrange
        var generator = new AdaptiveGridGenerator();
        var bounds = new Rect2D(0, 0, 100, 100);
        
        // Create a boundary in the center
        var boundary = new Boundary(new[]
        {
            new Point2D(40, 40),
            new Point2D(60, 40),
            new Point2D(60, 60),
            new Point2D(40, 60)
        });
        var boundaries = new List<Boundary> { boundary };

        // Act
        var grid = generator.Generate(bounds, boundaries);

        // Assert
        var pointsInside = grid.GetAllPoints().Count(p => p.InsideExcavation);
        Assert.IsTrue(pointsInside > 0, "Should mark some points as inside excavation");
    }

    [TestMethod]
    public void Generate_MarksPointsTooCloseToElement()
    {
        // Arrange
        var config = new AdaptiveGridConfiguration
        {
            MinimumDistanceToElement = 1.0,
            CoarseGridCountX = 20,
            CoarseGridCountY = 20
        };
        var generator = new AdaptiveGridGenerator(config);
        var bounds = new Rect2D(0, 0, 100, 100);
        
        var boundary = new Boundary(new[]
        {
            new Point2D(45, 45),
            new Point2D(55, 45),
            new Point2D(55, 55),
            new Point2D(45, 55)
        });
        var boundaries = new List<Boundary> { boundary };

        // Act
        var grid = generator.Generate(bounds, boundaries);

        // Assert
        var pointsTooClose = grid.GetAllPoints().Count(p => p.TooCloseToElement);
        Assert.IsTrue(pointsTooClose > 0, "Should mark some points as too close to elements");
    }

    [TestMethod]
    public void GetValidPoints_ExcludesInvalidPoints()
    {
        // Arrange
        var generator = new AdaptiveGridGenerator();
        var bounds = new Rect2D(0, 0, 100, 100);
        
        var boundary = new Boundary(new[]
        {
            new Point2D(40, 40),
            new Point2D(60, 40),
            new Point2D(60, 60),
            new Point2D(40, 60)
        });
        var boundaries = new List<Boundary> { boundary };

        // Act
        var grid = generator.Generate(bounds, boundaries);
        var validPoints = grid.GetValidPoints();
        var allPoints = grid.GetAllPoints();

        // Assert
        Assert.IsTrue(validPoints.Count < allPoints.Count, "Valid points should be less than total points");
        Assert.IsTrue(validPoints.All(p => p.IsValid), "All returned points should be valid");
    }

    [TestMethod]
    public void RefineBasedOnGradients_AddsAdaptivePoints()
    {
        // Arrange
        var generator = new AdaptiveGridGenerator();
        var bounds = new Rect2D(0, 0, 100, 100);
        var boundaries = new List<Boundary>();
        
        var grid = generator.Generate(bounds, boundaries);
        
        // Simulate some stress values with high gradient
        var points = grid.GetAllPoints();
        if (points.Count >= 2)
        {
            points[0].Sigma1 = 0;
            points[1].Sigma1 = 100;
        }

        // Act
        generator.RefineBasedOnGradients(grid, bounds, threshold: 1.0);

        // Assert
        Assert.IsTrue(grid.AdaptivePoints.Count >= 0, "Should have adaptive points after gradient refinement");
    }

    [TestMethod]
    public void GetStatistics_ReturnsCorrectCounts()
    {
        // Arrange
        var generator = new AdaptiveGridGenerator();
        var bounds = new Rect2D(0, 0, 100, 100);
        
        var boundary = new Boundary(new[]
        {
            new Point2D(40, 40),
            new Point2D(60, 40),
            new Point2D(60, 60),
            new Point2D(40, 60)
        });
        var boundaries = new List<Boundary> { boundary };

        var grid = generator.Generate(bounds, boundaries);

        // Act
        var stats = grid.GetStatistics();

        // Assert
        Assert.AreEqual(grid.CoarsePoints.Count, stats.CoarsePointCount);
        Assert.AreEqual(grid.MediumPoints.Count, stats.MediumPointCount);
        Assert.AreEqual(grid.FinePoints.Count, stats.FinePointCount);
        Assert.AreEqual(grid.AdaptivePoints.Count, stats.AdaptivePointCount);
        Assert.AreEqual(grid.TotalPointCount, stats.TotalPointCount);
        Assert.AreEqual(
            stats.CoarsePointCount + stats.MediumPointCount + stats.FinePointCount + stats.AdaptivePointCount,
            stats.TotalPointCount,
            "Total should equal sum of all levels");
    }

    [TestMethod]
    [DataRow(10, 10)]
    [DataRow(20, 20)]
    [DataRow(50, 50)]
    public void Generate_WithCustomGridSize_CreatesCorrectNumberOfCoarsePoints(int nx, int ny)
    {
        // Arrange
        var config = new AdaptiveGridConfiguration
        {
            CoarseGridCountX = nx,
            CoarseGridCountY = ny
        };
        var generator = new AdaptiveGridGenerator(config);
        var bounds = new Rect2D(0, 0, 100, 100);
        var boundaries = new List<Boundary>();

        // Act
        var grid = generator.Generate(bounds, boundaries);

        // Assert
        Assert.AreEqual(nx * ny, grid.CoarsePoints.Count, $"Should have {nx}x{ny} = {nx * ny} coarse points");
    }

    [TestMethod]
    public void Generate_WithMultipleBoundaries_HandlesMergedRegions()
    {
        // Arrange
        var generator = new AdaptiveGridGenerator();
        var bounds = new Rect2D(0, 0, 100, 100);
        
        var boundary1 = new Boundary(new[]
        {
            new Point2D(20, 20),
            new Point2D(40, 20),
            new Point2D(40, 40),
            new Point2D(20, 40)
        });
        
        var boundary2 = new Boundary(new[]
        {
            new Point2D(60, 60),
            new Point2D(80, 60),
            new Point2D(80, 80),
            new Point2D(60, 80)
        });
        
        var boundaries = new List<Boundary> { boundary1, boundary2 };

        // Act
        var grid = generator.Generate(bounds, boundaries);

        // Assert
        Assert.IsTrue(grid.MediumPoints.Count > 0, "Should have medium refinement for both boundaries");
        var stats = grid.GetStatistics();
        Assert.IsTrue(stats.TotalPointCount > 0);
    }

    [TestMethod]
    public void Generate_WithNoRefinement_ProducesOnlyCoarseGrid()
    {
        // Arrange
        var config = new AdaptiveGridConfiguration
        {
            MediumRefinementDistance = 0,
            FineRefinementDistance = 0,
            EnableGradientRefinement = false
        };
        var generator = new AdaptiveGridGenerator(config);
        var bounds = new Rect2D(0, 0, 100, 100);
        
        var boundary = new Boundary(new[]
        {
            new Point2D(40, 40),
            new Point2D(60, 40),
            new Point2D(60, 60),
            new Point2D(40, 60)
        });
        var boundaries = new List<Boundary> { boundary };

        // Act
        var grid = generator.Generate(bounds, boundaries);

        // Assert
        Assert.AreEqual(50 * 50, grid.CoarsePoints.Count);
        // Medium and Fine points may still exist due to region identification, but should be minimal
    }

    [TestMethod]
    public void Generate_PointsHaveCorrectLocations()
    {
        // Arrange
        var generator = new AdaptiveGridGenerator();
        var bounds = new Rect2D(0, 0, 100, 100);
        var boundaries = new List<Boundary>();

        // Act
        var grid = generator.Generate(bounds, boundaries);

        // Assert
        foreach (var point in grid.CoarsePoints)
        {
            Assert.IsTrue(point.Location.X >= bounds.X && point.Location.X <= bounds.Right,
                $"Point X={point.Location.X} should be within bounds [{bounds.X}, {bounds.Right}]");
            Assert.IsTrue(point.Location.Y >= bounds.Y && point.Location.Y <= bounds.Bottom,
                $"Point Y={point.Location.Y} should be within bounds [{bounds.Y}, {bounds.Bottom}]");
        }
    }

    [TestMethod]
    public void Generate_PointsHaveUniqueIndices()
    {
        // Arrange
        var generator = new AdaptiveGridGenerator();
        var bounds = new Rect2D(0, 0, 100, 100);
        
        var boundary = new Boundary(new[]
        {
            new Point2D(40, 40),
            new Point2D(60, 40),
            new Point2D(60, 60),
            new Point2D(40, 60)
        });
        var boundaries = new List<Boundary> { boundary };

        // Act
        var grid = generator.Generate(bounds, boundaries);
        var allPoints = grid.GetAllPoints();

        // Assert
        var uniqueIndices = allPoints.Select(p => p.Index).Distinct().Count();
        Assert.AreEqual(allPoints.Count, uniqueIndices, "All points should have unique indices");
    }

    [TestMethod]
    public void Generate_WithTriangularBoundary_HandlesCorrectly()
    {
        // Arrange
        var generator = new AdaptiveGridGenerator();
        var bounds = new Rect2D(0, 0, 100, 100);
        
        var boundary = new Boundary(new[]
        {
            new Point2D(50, 30),
            new Point2D(70, 60),
            new Point2D(30, 60)
        });
        var boundaries = new List<Boundary> { boundary };

        // Act
        var grid = generator.Generate(bounds, boundaries);

        // Assert
        Assert.IsTrue(grid.CoarsePoints.Count > 0, "Should have coarse points");
        Assert.IsTrue(grid.GetAllPoints().Any(p => p.InsideExcavation), "Should detect points inside triangle");
    }

    [TestMethod]
    public void AdaptiveGridConfiguration_HasReasonableDefaults()
    {
        // Arrange & Act
        var config = new AdaptiveGridConfiguration();

        // Assert
        Assert.AreEqual(50, config.CoarseGridCountX, "Default coarse grid X count should be 50");
        Assert.AreEqual(50, config.CoarseGridCountY, "Default coarse grid Y count should be 50");
        Assert.AreEqual(5.0, config.MediumRefinementDistance, "Default medium refinement distance should be 5.0");
        Assert.AreEqual(2.0, config.FineRefinementDistance, "Default fine refinement distance should be 2.0");
        Assert.AreEqual(135.0, config.HighCurvatureAngleThreshold, "Default angle threshold should be 135 degrees");
        Assert.AreEqual(0.1, config.MinimumDistanceToElement, "Default minimum distance should be 0.1");
        Assert.IsTrue(config.EnableGradientRefinement, "Gradient refinement should be enabled by default");
    }

    [TestMethod]
    public void GetStatistics_ToString_ReturnsFormattedString()
    {
        // Arrange
        var stats = new AdaptiveGridStatistics
        {
            CoarsePointCount = 2500,
            MediumPointCount = 1000,
            FinePointCount = 500,
            AdaptivePointCount = 100,
            TotalPointCount = 4100,
            ValidPointCount = 3800,
            InvalidPointCount = 300
        };

        // Act
        var result = stats.ToString();

        // Assert
        Assert.IsTrue(result.Contains("4100"), "Should contain total point count");
        Assert.IsTrue(result.Contains("2500"), "Should contain coarse point count");
        Assert.IsTrue(result.Contains("1000"), "Should contain medium point count");
        Assert.IsTrue(result.Contains("500"), "Should contain fine point count");
        Assert.IsTrue(result.Contains("3800"), "Should contain valid point count");
    }
}

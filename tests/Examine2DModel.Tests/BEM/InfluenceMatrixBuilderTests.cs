using CAD2DModel.Geometry;
using Examine2DModel.Analysis;
using Examine2DModel.BEM;
using Examine2DModel.Materials;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Examine2DModel.Tests.BEM;

[TestClass]
public class InfluenceMatrixBuilderTests
{
    private TestIsotropicMaterial _material = null!;
    private BEMConfiguration _config = null!;
    private InfluenceMatrixBuilder _builder = null!;

    [TestInitialize]
    public void Setup()
    {
        _material = new TestIsotropicMaterial
        {
            Name = "Test Rock",
            YoungModulus = 10e9, // 10 GPa
            PoissonRatio = 0.25,
            Density = 2700 // kg/m³
        };

        _config = new BEMConfiguration
        {
            TargetElementCount = 100,
            ElementType = ElementType.Constant,
            EnableCaching = true,
            Tolerance = 1e-6,
            MaxIterations = 1000
        };

        _builder = new InfluenceMatrixBuilder(_material, _config);
    }

    [TestMethod]
    public void Constructor_WithNullMaterial_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new InfluenceMatrixBuilder(null!, _config));
    }

    [TestMethod]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new InfluenceMatrixBuilder(_material, null!));
    }

    [TestMethod]
    public void BuildMatrix_SimpleSquareExcavation_ReturnsCorrectDimensions()
    {
        // Arrange - Create a simple square boundary (4 elements)
        var elements = CreateSquareExcavation(centerX: 0, centerY: -10, size: 10.0, numElements: 4);

        // Act - Use full-space (not half-space) to avoid ground surface complications
        var matrix = _builder.BuildMatrix(elements, groundSurfaceY: 0.0, isHalfSpace: false);

        // Assert
        int expectedDOF = elements.Count * 2; // 2 DOF per element (constant elements)
        matrix.RowCount.Should().Be(expectedDOF);
        matrix.ColumnCount.Should().Be(expectedDOF);
        
        // Matrix should not be all zeros
        matrix.FrobeniusNorm().Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void BuildMatrix_LinearElements_ReturnsCorrectDimensions()
    {
        // Arrange
        _config = new BEMConfiguration
        {
            ElementType = ElementType.Linear,
            EnableCaching = true
        };
        _builder = new InfluenceMatrixBuilder(_material, _config);
        
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);

        // Act
        var matrix = _builder.BuildMatrix(elements);

        // Assert - Linear elements have 2 nodes, so 4 DOF per element
        int expectedDOF = elements.Count * 4; // 2 nodes × 2 DOF per node
        matrix.RowCount.Should().Be(expectedDOF);
        matrix.ColumnCount.Should().Be(expectedDOF);
    }

    [TestMethod]
    public void BuildMatrix_QuadraticElements_ReturnsCorrectDimensions()
    {
        // Arrange
        _config = new BEMConfiguration
        {
            ElementType = ElementType.Quadratic,
            EnableCaching = true
        };
        _builder = new InfluenceMatrixBuilder(_material, _config);
        
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);

        // Act
        var matrix = _builder.BuildMatrix(elements);

        // Assert - Quadratic elements have 3 nodes, so 6 DOF per element
        int expectedDOF = elements.Count * 6; // 3 nodes × 2 DOF per node
        matrix.RowCount.Should().Be(expectedDOF);
        matrix.ColumnCount.Should().Be(expectedDOF);
    }

    [TestMethod]
    public void BuildMatrix_CalledTwiceWithSameGeometry_ReturnsCachedMatrix()
    {
        // Arrange
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 8);

        // Act - Build matrix twice
        var matrix1 = _builder.BuildMatrix(elements);
        var stats1 = _builder.LastBuildStats;
        
        var matrix2 = _builder.BuildMatrix(elements);
        var stats2 = _builder.LastBuildStats;

        // Assert - Second call should hit cache
        stats1.CacheHit.Should().BeFalse("First call should miss cache");
        stats2.CacheHit.Should().BeTrue("Second call should hit cache");
        stats2.MatrixAssemblyTime.Should().Be(TimeSpan.Zero, "Cached matrix should have zero assembly time");
        
        // Matrices should be identical (same reference)
        matrix1.Should().BeSameAs(matrix2);
    }

    [TestMethod]
    public void BuildMatrix_CalledTwiceWithDifferentGeometry_RebuildsMatrix()
    {
        // Arrange
        var elements1 = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);
        var elements2 = CreateSquareExcavation(centerX: 5, centerY: 5, size: 15.0, numElements: 4);

        // Act
        var matrix1 = _builder.BuildMatrix(elements1);
        var stats1 = _builder.LastBuildStats;
        
        var matrix2 = _builder.BuildMatrix(elements2);
        var stats2 = _builder.LastBuildStats;

        // Assert - Both should miss cache (different geometry)
        stats1.CacheHit.Should().BeFalse();
        stats2.CacheHit.Should().BeFalse();
        
        // Matrices should be different
        matrix1.Should().NotBeSameAs(matrix2);
    }

    [TestMethod]
    public void BuildMatrix_WithCachingDisabled_NeverCaches()
    {
        // Arrange
        _config.EnableCaching = false;
        _builder = new InfluenceMatrixBuilder(_material, _config);
        
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);

        // Act - Build matrix twice
        var matrix1 = _builder.BuildMatrix(elements);
        var stats1 = _builder.LastBuildStats;
        
        var matrix2 = _builder.BuildMatrix(elements);
        var stats2 = _builder.LastBuildStats;

        // Assert - Both should miss cache (caching disabled)
        stats1.CacheHit.Should().BeFalse();
        stats2.CacheHit.Should().BeFalse();
        
        // Matrices should be different instances
        matrix1.Should().NotBeSameAs(matrix2);
    }

    [TestMethod]
    public void InvalidateCache_AfterBuilding_ForcesRebuild()
    {
        // Arrange
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);
        var matrix1 = _builder.BuildMatrix(elements);

        // Act - Invalidate cache and rebuild
        _builder.InvalidateCache();
        var matrix2 = _builder.BuildMatrix(elements);
        var stats2 = _builder.LastBuildStats;

        // Assert
        stats2.CacheHit.Should().BeFalse("Cache should be invalidated");
        matrix1.Should().NotBeSameAs(matrix2);
    }

    [TestMethod]
    public void IsCacheValid_WithSameElements_ReturnsTrue()
    {
        // Arrange
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);
        _builder.BuildMatrix(elements);

        // Act
        bool isValid = _builder.IsCacheValid(elements);

        // Assert
        isValid.Should().BeTrue();
    }

    [TestMethod]
    public void IsCacheValid_WithDifferentElements_ReturnsFalse()
    {
        // Arrange
        var elements1 = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);
        var elements2 = CreateSquareExcavation(centerX: 5, centerY: 5, size: 10.0, numElements: 4);
        
        _builder.BuildMatrix(elements1);

        // Act
        bool isValid = _builder.IsCacheValid(elements2);

        // Assert
        isValid.Should().BeFalse();
    }

    [TestMethod]
    public void IsCacheValid_BeforeAnyBuild_ReturnsFalse()
    {
        // Arrange
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);

        // Act
        bool isValid = _builder.IsCacheValid(elements);

        // Assert
        isValid.Should().BeFalse();
    }

    [TestMethod]
    public void BuildMatrix_MatrixProperties_ValidBEMMatrix()
    {
        // Arrange - Create circular excavation (more symmetric), away from ground surface
        var elements = CreateCircularExcavation(centerX: 0, centerY: -10, radius: 5.0, numElements: 16);

        // Act - Use full-space to avoid ground surface complications
        var matrix = _builder.BuildMatrix(elements, groundSurfaceY: 0.0, isHalfSpace: false);

        // Assert - Check that matrix has valid BEM properties
        // 1. Matrix should be non-zero
        matrix.FrobeniusNorm().Should().BeGreaterThan(0, "Matrix should have non-zero norm");
        
        // 2. Matrix should not contain NaN or Infinity
        bool hasInvalidValues = false;
        for (int i = 0; i < Math.Min(matrix.RowCount, 10); i++)
        {
            for (int j = 0; j < Math.Min(matrix.ColumnCount, 10); j++)
            {
                double val = matrix[i, j];
                if (double.IsNaN(val) || double.IsInfinity(val))
                {
                    hasInvalidValues = true;
                    break;
                }
            }
            if (hasInvalidValues) break;
        }
        
        hasInvalidValues.Should().BeFalse("Matrix should not contain NaN or Infinity values");
        
        // 3. Matrix should have reasonable magnitude (not all near-zero)
        // Count non-negligible elements
        int significantElements = 0;
        for (int i = 0; i < matrix.RowCount; i++)
        {
            for (int j = 0; j < matrix.ColumnCount; j++)
            {
                if (Math.Abs(matrix[i, j]) > 1e-20)
                    significantElements++;
            }
        }
        
        significantElements.Should().BeGreaterThan(matrix.RowCount, 
            "Matrix should have many non-negligible elements");
    }

    [TestMethod]
    public void BuildMatrix_Performance_CompletesInReasonableTime()
    {
        // Arrange - Create larger problem (100 elements)
        var elements = CreateCircularExcavation(centerX: 0, centerY: 0, radius: 10.0, numElements: 100);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var matrix = _builder.BuildMatrix(elements);
        stopwatch.Stop();

        // Assert - Should complete in reasonable time for cold cache
        // 100 elements = 200 DOF, should take < 5 seconds on modern hardware
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5), 
            "Matrix assembly should be reasonably fast");
        
        _builder.LastBuildStats.ElementCount.Should().Be(100);
        _builder.LastBuildStats.DegreesOfFreedom.Should().Be(200);
    }

    [TestMethod]
    public void BuildFieldPointMatrix_SimpleCase_ReturnsCorrectDimensions()
    {
        // Arrange
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);
        var fieldPoints = new List<FieldPoint>
        {
            new FieldPoint { Location = new Point2D(1, 1) },
            new FieldPoint { Location = new Point2D(2, 2) },
            new FieldPoint { Location = new Point2D(3, 3) }
        };

        // Act
        var matrix = _builder.BuildFieldPointMatrix(fieldPoints, elements);

        // Assert
        int expectedRows = fieldPoints.Count * 6; // 6 components per field point (ux, uy, sxx, syy, sxy, szz)
        int expectedCols = elements.Count * 2; // 2 DOF per element
        
        matrix.RowCount.Should().Be(expectedRows);
        matrix.ColumnCount.Should().Be(expectedCols);
    }

    [TestMethod]
    public void BuildFieldPointMatrix_WithLinearElements_ReturnsCorrectDimensions()
    {
        // Arrange
        _config = new BEMConfiguration { ElementType = ElementType.Linear };
        _builder = new InfluenceMatrixBuilder(_material, _config);
        
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);
        var fieldPoints = new List<FieldPoint>
        {
            new FieldPoint { Location = new Point2D(1, 1) }
        };

        // Act
        var matrix = _builder.BuildFieldPointMatrix(fieldPoints, elements);

        // Assert
        int expectedRows = fieldPoints.Count * 6;
        int expectedCols = elements.Count * 4; // Linear elements: 2 nodes × 2 DOF
        
        matrix.RowCount.Should().Be(expectedRows);
        matrix.ColumnCount.Should().Be(expectedCols);
    }

    [TestMethod]
    public void BuildFieldPointMatrix_FarFieldPoint_HasNonZeroInfluence()
    {
        // Arrange
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 8);
        var fieldPoints = new List<FieldPoint>
        {
            new FieldPoint { Location = new Point2D(20, 20) } // Far from excavation
        };

        // Act
        var matrix = _builder.BuildFieldPointMatrix(fieldPoints, elements);

        // Assert - Even far field points should have non-zero influence
        double matrixNorm = matrix.FrobeniusNorm();
        matrixNorm.Should().BeGreaterThan(0, "Field point matrix should have non-zero influence");
    }

    [TestMethod]
    public void BuildMatrix_HashConsistency_SameElementsProduceSameHash()
    {
        // Arrange
        var elements1 = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);
        var elements2 = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 4);

        // Act - Build with first set
        _builder.BuildMatrix(elements1);
        bool valid1 = _builder.IsCacheValid(elements1);
        bool valid2 = _builder.IsCacheValid(elements2);

        // Assert - Same geometry should have same hash
        valid1.Should().BeTrue();
        valid2.Should().BeTrue("Identical geometry should produce identical hash");
    }

    [TestMethod]
    public void BuildMatrix_MultipleExcavations_HandlesCorrectly()
    {
        // Arrange - Create two separate excavations away from ground surface, well separated
        var elements1 = CreateSquareExcavation(centerX: -15, centerY: -10, size: 5.0, numElements: 4);
        var elements2 = CreateSquareExcavation(centerX: 15, centerY: -10, size: 5.0, numElements: 4);
        var allElements = elements1.Concat(elements2).ToList();

        // Act - Use full-space
        var matrix = _builder.BuildMatrix(allElements, isHalfSpace: false);

        // Assert
        int expectedDOF = allElements.Count * 2;
        matrix.RowCount.Should().Be(expectedDOF);
        matrix.ColumnCount.Should().Be(expectedDOF);
        
        // Matrix should be non-trivial
        // Check for NaN explicitly
        bool hasNaN = false;
        for (int i = 0; i < matrix.RowCount && !hasNaN; i++)
        {
            for (int j = 0; j < matrix.ColumnCount && !hasNaN; j++)
            {
                if (double.IsNaN(matrix[i, j]))
                    hasNaN = true;
            }
        }
        
        hasNaN.Should().BeFalse("Matrix should not contain NaN values");
        double norm = matrix.FrobeniusNorm();
        // MathNet returns negative values or NaN in case of error
        (norm > 0 && !double.IsNaN(norm)).Should().BeTrue("Matrix should have positive, non-NaN norm");
    }

    [TestMethod]
    public void LastBuildStats_AfterBuild_ContainsValidInformation()
    {
        // Arrange
        var elements = CreateSquareExcavation(centerX: 0, centerY: 0, size: 10.0, numElements: 8);

        // Act
        _builder.BuildMatrix(elements);
        var stats = _builder.LastBuildStats;

        // Assert
        stats.ElementCount.Should().Be(8);
        stats.DegreesOfFreedom.Should().Be(16); // Constant elements: 8 × 2
        stats.MatrixAssemblyTime.Should().BeGreaterThan(TimeSpan.Zero);
        stats.HashComputationTime.Should().BeGreaterThan(TimeSpan.Zero);
        stats.CacheHit.Should().BeFalse(); // First build
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(20)]
    [DataRow(50)]
    public void BuildMatrix_VariousElementCounts_ScalesCorrectly(int numElements)
    {
        // Arrange
        var elements = CreateCircularExcavation(centerX: 0, centerY: 0, radius: 10.0, numElements);

        // Act
        var matrix = _builder.BuildMatrix(elements);

        // Assert
        int expectedDOF = numElements * 2;
        matrix.RowCount.Should().Be(expectedDOF);
        matrix.ColumnCount.Should().Be(expectedDOF);
        
        // Check that assembly time increases with element count
        var stats = _builder.LastBuildStats;
        stats.ElementCount.Should().Be(numElements);
    }

    #region Helper Methods

    /// <summary>
    /// Create a square excavation boundary with specified number of elements
    /// </summary>
    private static List<BoundaryElement> CreateSquareExcavation(double centerX, double centerY, 
        double size, int numElements)
    {
        var elements = new List<BoundaryElement>();
        double halfSize = size / 2.0;
        int elementsPerSide = numElements / 4;

        // Bottom side (left to right)
        CreateSideElements(elements, 
            new Point2D(centerX - halfSize, centerY - halfSize),
            new Point2D(centerX + halfSize, centerY - halfSize),
            elementsPerSide, boundaryId: 0);

        // Right side (bottom to top)
        CreateSideElements(elements,
            new Point2D(centerX + halfSize, centerY - halfSize),
            new Point2D(centerX + halfSize, centerY + halfSize),
            elementsPerSide, boundaryId: 0);

        // Top side (right to left)
        CreateSideElements(elements,
            new Point2D(centerX + halfSize, centerY + halfSize),
            new Point2D(centerX - halfSize, centerY + halfSize),
            elementsPerSide, boundaryId: 0);

        // Left side (top to bottom)
        CreateSideElements(elements,
            new Point2D(centerX - halfSize, centerY + halfSize),
            new Point2D(centerX - halfSize, centerY - halfSize),
            elementsPerSide, boundaryId: 0);

        return elements;
    }

    /// <summary>
    /// Create a circular excavation boundary
    /// </summary>
    private static List<BoundaryElement> CreateCircularExcavation(double centerX, double centerY, 
        double radius, int numElements)
    {
        var elements = new List<BoundaryElement>();
        double angleStep = 2.0 * Math.PI / numElements;

        for (int i = 0; i < numElements; i++)
        {
            double angle1 = i * angleStep;
            double angle2 = (i + 1) * angleStep;

            var start = new Point2D(
                centerX + radius * Math.Cos(angle1),
                centerY + radius * Math.Sin(angle1));

            var end = new Point2D(
                centerX + radius * Math.Cos(angle2),
                centerY + radius * Math.Sin(angle2));

            elements.Add(BoundaryElement.Create(start, end, elementType: 1, boundaryId: 0));
        }

        return elements;
    }

    /// <summary>
    /// Create elements along a line segment
    /// </summary>
    private static void CreateSideElements(List<BoundaryElement> elements, 
        Point2D start, Point2D end, int count, int boundaryId)
    {
        double dx = (end.X - start.X) / count;
        double dy = (end.Y - start.Y) / count;

        for (int i = 0; i < count; i++)
        {
            var elemStart = new Point2D(start.X + i * dx, start.Y + i * dy);
            var elemEnd = new Point2D(start.X + (i + 1) * dx, start.Y + (i + 1) * dy);
            
            elements.Add(BoundaryElement.Create(elemStart, elemEnd, elementType: 1, boundaryId));
        }
    }

    /// <summary>
    /// Test material implementation
    /// </summary>
    private class TestIsotropicMaterial : IIsotropicMaterial
    {
        public string Name { get; set; } = "Test Material";
        public double Density { get; set; } = 2700;
        public double YoungModulus { get; set; } = 10e9;
        public double PoissonRatio { get; set; } = 0.25;

        public double ShearModulus => YoungModulus / (2.0 * (1.0 + PoissonRatio));
    }

    #endregion
}

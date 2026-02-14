using CAD2DModel.Geometry;
using Examine2DModel.Analysis;
using Examine2DModel.BEM;

namespace Examine2DModel.Tests.BEM;

[TestClass]
public class BoundaryElementDiscretizerTests
{
    [TestMethod]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new BoundaryElementDiscretizer(null!));
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var config = new BEMConfiguration { TargetElementCount = 100 };
        var discretizer = new BoundaryElementDiscretizer(config);
        var boundaries = new List<Boundary>();
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(boundaries);
        
        // Assert
        Assert.IsNotNull(elements);
        Assert.AreEqual(0, elements.Count);
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_SimpleSquare_CreatesCorrectNumberOfElements()
    {
        // Arrange
        var config = new BEMConfiguration 
        { 
            TargetElementCount = 100,
            UseAdaptiveElementSizing = false // Disable for predictable results
        };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        // Create a 10x10 square (perimeter = 40)
        var square = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(new[] { square });
        
        // Assert
        Assert.IsNotNull(elements);
        // Perimeter = 40, target element size = 40/100 = 0.4
        // Each side of 10 units should have: 10/0.4 = 25 elements
        // Total = 4 sides * 25 = 100 elements
        Assert.AreEqual(100, elements.Count, "Should create approximately 100 elements for 40-unit perimeter");
        
        // Verify all elements have the same boundary ID
        Assert.IsTrue(elements.All(e => e.BoundaryId == 0));
    }
    
    [TestMethod]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(200)]
    public void DiscretizeBoundaries_RespectTargetElementCount(int targetCount)
    {
        // Arrange
        var config = new BEMConfiguration 
        { 
            TargetElementCount = targetCount,
            UseAdaptiveElementSizing = false
        };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        // Create a simple square
        var square = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(new[] { square });
        
        // Assert
        // Should be approximately the target count (within 10%)
        double tolerance = targetCount * 0.1;
        Assert.IsTrue(
            Math.Abs(elements.Count - targetCount) <= tolerance,
            $"Element count {elements.Count} should be within {tolerance} of target {targetCount}");
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_MultipleBoundaries_AssignsUniqueBoundaryIds()
    {
        // Arrange
        var config = new BEMConfiguration { TargetElementCount = 100 };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        var boundary1 = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        var boundary2 = new Boundary(new[]
        {
            new Point2D(20, 20),
            new Point2D(30, 20),
            new Point2D(30, 30),
            new Point2D(20, 30)
        });
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(new[] { boundary1, boundary2 });
        
        // Assert
        var boundaryIds = elements.Select(e => e.BoundaryId).Distinct().ToList();
        Assert.AreEqual(2, boundaryIds.Count, "Should have 2 unique boundary IDs");
        Assert.IsTrue(boundaryIds.Contains(0), "Should have boundary ID 0");
        Assert.IsTrue(boundaryIds.Contains(1), "Should have boundary ID 1");
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_ElementProperties_AreCorrectlySet()
    {
        // Arrange
        var config = new BEMConfiguration 
        { 
            TargetElementCount = 20,
            ElementType = ElementType.Linear,
            UseAdaptiveElementSizing = false
        };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        var boundary = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(new[] { boundary });
        
        // Assert
        Assert.IsTrue(elements.Count > 0);
        
        foreach (var element in elements)
        {
            // Verify element type
            Assert.AreEqual(2, element.ElementType, "Should be linear element (type 2)");
            
            // Verify boundary conditions are initialized
            Assert.AreEqual(1, element.BoundaryConditionType, "Should have traction BC type");
            Assert.AreEqual(0.0, element.NormalBoundaryCondition);
            Assert.AreEqual(0.0, element.ShearBoundaryCondition);
            
            // Verify geometric properties
            Assert.IsTrue(element.Length > 0, "Element length should be positive");
            Assert.IsNotNull(element.MidPoint);
            
            // Verify direction cosines are normalized
            double magnitude = Math.Sqrt(
                element.CosineDirection * element.CosineDirection +
                element.SineDirection * element.SineDirection);
            Assert.AreEqual(1.0, magnitude, 0.0001, "Direction cosines should be normalized");
        }
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_HorizontalSegment_HasCorrectDirectionCosines()
    {
        // Arrange
        var config = new BEMConfiguration { TargetElementCount = 10 };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        // Horizontal segment from (0,0) to (10,0)
        var boundary = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 1),
            new Point2D(0, 1)
        });
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(new[] { boundary });
        
        // Assert
        var horizontalElements = elements.Where(e => 
            Math.Abs(e.StartPoint.Y - e.EndPoint.Y) < 0.001 &&
            e.StartPoint.Y < 0.5).ToList();
        
        Assert.IsTrue(horizontalElements.Count > 0, "Should have horizontal elements");
        
        foreach (var element in horizontalElements)
        {
            Assert.AreEqual(1.0, element.CosineDirection, 0.001, "Horizontal element should have cos = 1");
            Assert.AreEqual(0.0, element.SineDirection, 0.001, "Horizontal element should have sin = 0");
        }
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_VerticalSegment_HasCorrectDirectionCosines()
    {
        // Arrange
        var config = new BEMConfiguration { TargetElementCount = 10 };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        // Vertical segment from (0,0) to (0,10)
        var boundary = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(1, 0),
            new Point2D(1, 10),
            new Point2D(0, 10)
        });
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(new[] { boundary });
        
        // Assert
        var verticalElements = elements.Where(e => 
            Math.Abs(e.StartPoint.X - e.EndPoint.X) < 0.001 &&
            e.StartPoint.X < 0.5).ToList();
        
        Assert.IsTrue(verticalElements.Count > 0, "Should have vertical elements");
        
        foreach (var element in verticalElements)
        {
            Assert.AreEqual(0.0, element.CosineDirection, 0.001, "Vertical element should have cos = 0");
            // Direction can be up (+1) or down (-1), but magnitude should be 1
            Assert.AreEqual(1.0, Math.Abs(element.SineDirection), 0.001, "Vertical element should have |sin| = 1");
        }
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_ExternalBoundary_IsMarkedAsGroundSurface()
    {
        // Arrange
        var config = new BEMConfiguration { TargetElementCount = 20 };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        var externalBoundary = new ExternalBoundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(new[] { externalBoundary });
        
        // Assert
        Assert.IsTrue(elements.Count > 0);
        Assert.IsTrue(elements.All(e => e.IsGroundSurface), "External boundary elements should be marked as ground surface");
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_RegularBoundary_IsNotMarkedAsGroundSurface()
    {
        // Arrange
        var config = new BEMConfiguration { TargetElementCount = 20 };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        var regularBoundary = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(new[] { regularBoundary });
        
        // Assert
        Assert.IsTrue(elements.Count > 0);
        Assert.IsTrue(elements.All(e => !e.IsGroundSurface), "Regular boundary elements should NOT be marked as ground surface");
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_WithAdaptiveRefinement_CreatesMoreElementsAtCorners()
    {
        // Arrange
        var configNoAdaptive = new BEMConfiguration 
        { 
            TargetElementCount = 100,
            UseAdaptiveElementSizing = false
        };
        var configWithAdaptive = new BEMConfiguration 
        { 
            TargetElementCount = 100,
            UseAdaptiveElementSizing = true,
            MaxRefinementFactor = 3.0
        };
        
        var discretizerNoAdaptive = new BoundaryElementDiscretizer(configNoAdaptive);
        var discretizerWithAdaptive = new BoundaryElementDiscretizer(configWithAdaptive);
        
        // Square has sharp 90-degree corners
        var square = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        // Act
        var elementsNoAdaptive = discretizerNoAdaptive.DiscretizeBoundaries(new[] { square });
        var elementsWithAdaptive = discretizerWithAdaptive.DiscretizeBoundaries(new[] { square });
        
        // Assert
        Assert.IsTrue(elementsWithAdaptive.Count > elementsNoAdaptive.Count,
            $"Adaptive refinement should create more elements: {elementsWithAdaptive.Count} vs {elementsNoAdaptive.Count}");
    }
    
    [TestMethod]
    public void GetStatistics_WithElements_ReturnsCorrectStatistics()
    {
        // Arrange
        var config = new BEMConfiguration { TargetElementCount = 100 };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        var boundary = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        var elements = discretizer.DiscretizeBoundaries(new[] { boundary });
        
        // Act
        var stats = discretizer.GetStatistics(elements);
        
        // Assert
        Assert.AreEqual(elements.Count, stats.TotalElementCount);
        Assert.IsTrue(stats.MinElementLength > 0);
        Assert.IsTrue(stats.MaxElementLength > 0);
        Assert.IsTrue(stats.AverageElementLength > 0);
        Assert.IsTrue(stats.MinElementLength <= stats.AverageElementLength);
        Assert.IsTrue(stats.AverageElementLength <= stats.MaxElementLength);
        Assert.AreEqual(1, stats.ElementsByBoundary.Count);
    }
    
    [TestMethod]
    public void GetStatistics_WithEmptyList_ReturnsEmptyStatistics()
    {
        // Arrange
        var config = new BEMConfiguration { TargetElementCount = 100 };
        var discretizer = new BoundaryElementDiscretizer(config);
        var elements = new List<BoundaryElement>();
        
        // Act
        var stats = discretizer.GetStatistics(elements);
        
        // Assert
        Assert.AreEqual(0, stats.TotalElementCount);
        Assert.AreEqual(0, stats.ElementsByBoundary.Count);
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_ElementType_MatchesConfiguration()
    {
        // Arrange - Test all element types
        var testCases = new[]
        {
            (ElementType.Constant, 1),
            (ElementType.Linear, 2),
            (ElementType.Quadratic, 3)
        };
        
        foreach (var (configType, expectedType) in testCases)
        {
            var config = new BEMConfiguration 
            { 
                TargetElementCount = 20,
                ElementType = configType
            };
            var discretizer = new BoundaryElementDiscretizer(config);
            
            var boundary = new Boundary(new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            });
            
            // Act
            var elements = discretizer.DiscretizeBoundaries(new[] { boundary });
            
            // Assert
            Assert.IsTrue(elements.Count > 0);
            Assert.IsTrue(elements.All(e => e.ElementType == expectedType),
                $"All elements should be type {expectedType} for {configType}");
        }
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_VerySmallSegment_CreatesAtLeastOneElement()
    {
        // Arrange
        var config = new BEMConfiguration { TargetElementCount = 1000 };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        // Very small boundary with large target element count
        var tinyBoundary = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(0.1, 0),
            new Point2D(0.1, 0.1),
            new Point2D(0, 0.1)
        });
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(new[] { tinyBoundary });
        
        // Assert
        Assert.IsTrue(elements.Count >= 4, "Should create at least one element per segment");
    }
    
    [TestMethod]
    public void DiscretizeBoundaries_MidpointCalculation_IsAccurate()
    {
        // Arrange
        var config = new BEMConfiguration { TargetElementCount = 10 };
        var discretizer = new BoundaryElementDiscretizer(config);
        
        var boundary = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        // Act
        var elements = discretizer.DiscretizeBoundaries(new[] { boundary });
        
        // Assert
        foreach (var element in elements)
        {
            double expectedMidX = (element.StartPoint.X + element.EndPoint.X) / 2.0;
            double expectedMidY = (element.StartPoint.Y + element.EndPoint.Y) / 2.0;
            
            Assert.AreEqual(expectedMidX, element.MidPoint.X, 1e-10, "Midpoint X should be accurate");
            Assert.AreEqual(expectedMidY, element.MidPoint.Y, 1e-10, "Midpoint Y should be accurate");
        }
    }
}

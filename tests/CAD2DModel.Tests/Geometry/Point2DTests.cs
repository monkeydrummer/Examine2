using CAD2DModel.Geometry;
using FluentAssertions;

namespace CAD2DModel.Tests.Geometry;

[TestClass]
public class Point2DTests
{
    [TestMethod]
    public void Constructor_ShouldInitializeXAndY()
    {
        // Arrange & Act
        var point = new Point2D(3.0, 4.0);
        
        // Assert
        point.X.Should().Be(3.0);
        point.Y.Should().Be(4.0);
    }
    
    [TestMethod]
    public void DistanceTo_ShouldCalculateCorrectDistance()
    {
        // Arrange
        var point1 = new Point2D(0, 0);
        var point2 = new Point2D(3, 4);
        
        // Act
        var distance = point1.DistanceTo(point2);
        
        // Assert
        distance.Should().BeApproximately(5.0, 1e-10);
    }
    
    [TestMethod]
    [DataRow(0.0, 0.0, 0.0, 0.0, 0.0)]
    [DataRow(0.0, 0.0, 3.0, 4.0, 5.0)]
    [DataRow(1.0, 1.0, 4.0, 5.0, 5.0)]
    [DataRow(-3.0, -4.0, 0.0, 0.0, 5.0)]
    public void DistanceTo_VariousPoints_ShouldCalculateCorrectly(
        double x1, double y1, double x2, double y2, double expected)
    {
        // Arrange
        var point1 = new Point2D(x1, y1);
        var point2 = new Point2D(x2, y2);
        
        // Act
        var distance = point1.DistanceTo(point2);
        
        // Assert
        distance.Should().BeApproximately(expected, 1e-10);
    }
    
    [TestMethod]
    public void VectorTo_ShouldCreateCorrectVector()
    {
        // Arrange
        var point1 = new Point2D(1, 2);
        var point2 = new Point2D(4, 6);
        
        // Act
        var vector = point1.VectorTo(point2);
        
        // Assert
        vector.X.Should().Be(3.0);
        vector.Y.Should().Be(4.0);
    }
    
    [TestMethod]
    public void Offset_ShouldMovePointCorrectly()
    {
        // Arrange
        var point = new Point2D(1, 2);
        
        // Act
        var offset = point.Offset(3, 4);
        
        // Assert
        offset.X.Should().Be(4.0);
        offset.Y.Should().Be(6.0);
    }
    
    [TestMethod]
    public void OperatorPlus_ShouldAddVectorToPoint()
    {
        // Arrange
        var point = new Point2D(1, 2);
        var vector = new Vector2D(3, 4);
        
        // Act
        var result = point + vector;
        
        // Assert
        result.X.Should().Be(4.0);
        result.Y.Should().Be(6.0);
    }
    
    [TestMethod]
    public void Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var point1 = new Point2D(1, 2);
        var point2 = new Point2D(1, 2);
        var point3 = new Point2D(3, 4);
        
        // Act & Assert
        (point1 == point2).Should().BeTrue();
        (point1 == point3).Should().BeFalse();
        (point1 != point3).Should().BeTrue();
    }
}

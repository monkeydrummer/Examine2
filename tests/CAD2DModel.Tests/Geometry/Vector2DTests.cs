using CAD2DModel.Geometry;
using FluentAssertions;

namespace CAD2DModel.Tests.Geometry;

[TestClass]
public class Vector2DTests
{
    [TestMethod]
    public void Constructor_ShouldInitializeXAndY()
    {
        // Arrange & Act
        var vector = new Vector2D(3.0, 4.0);
        
        // Assert
        vector.X.Should().Be(3.0);
        vector.Y.Should().Be(4.0);
    }
    
    [TestMethod]
    public void Length_ShouldCalculateCorrectly()
    {
        // Arrange
        var vector = new Vector2D(3, 4);
        
        // Act
        var length = vector.Length;
        
        // Assert
        length.Should().BeApproximately(5.0, 1e-10);
    }
    
    [TestMethod]
    [DataRow(3.0, 4.0, 5.0)]
    [DataRow(0.0, 0.0, 0.0)]
    [DataRow(1.0, 0.0, 1.0)]
    [DataRow(0.0, 1.0, 1.0)]
    [DataRow(-3.0, 4.0, 5.0)]
    public void Length_VariousVectors_ShouldCalculateCorrectly(
        double x, double y, double expectedLength)
    {
        // Arrange
        var vector = new Vector2D(x, y);
        
        // Act
        var length = vector.Length;
        
        // Assert
        length.Should().BeApproximately(expectedLength, 1e-10);
    }
    
    [TestMethod]
    public void Normalized_ShouldReturnUnitVector()
    {
        // Arrange
        var vector = new Vector2D(3, 4);
        
        // Act
        var normalized = vector.Normalized();
        
        // Assert
        normalized.Length.Should().BeApproximately(1.0, 1e-10);
        normalized.X.Should().BeApproximately(0.6, 1e-10);
        normalized.Y.Should().BeApproximately(0.8, 1e-10);
    }
    
    [TestMethod]
    public void Dot_ShouldCalculateCorrectly()
    {
        // Arrange
        var v1 = new Vector2D(2, 3);
        var v2 = new Vector2D(4, 5);
        
        // Act
        var dot = v1.Dot(v2);
        
        // Assert
        dot.Should().Be(23.0); // 2*4 + 3*5 = 23
    }
    
    [TestMethod]
    public void Cross_ShouldCalculateCorrectly()
    {
        // Arrange
        var v1 = new Vector2D(2, 3);
        var v2 = new Vector2D(4, 5);
        
        // Act
        var cross = v1.Cross(v2);
        
        // Assert
        cross.Should().Be(-2.0); // 2*5 - 3*4 = -2
    }
    
    [TestMethod]
    public void Perpendicular_ShouldReturn90DegreeRotation()
    {
        // Arrange
        var vector = new Vector2D(3, 4);
        
        // Act
        var perpendicular = vector.Perpendicular();
        
        // Assert
        perpendicular.X.Should().Be(-4.0);
        perpendicular.Y.Should().Be(3.0);
        vector.Dot(perpendicular).Should().BeApproximately(0, 1e-10);
    }
    
    [TestMethod]
    public void Rotate_ShouldRotateCorrectly()
    {
        // Arrange
        var vector = new Vector2D(1, 0);
        
        // Act - rotate 90 degrees (π/2 radians)
        var rotated = vector.Rotate(Math.PI / 2);
        
        // Assert
        rotated.X.Should().BeApproximately(0.0, 1e-10);
        rotated.Y.Should().BeApproximately(1.0, 1e-10);
    }
    
    [TestMethod]
    public void OperatorPlus_ShouldAddVectors()
    {
        // Arrange
        var v1 = new Vector2D(1, 2);
        var v2 = new Vector2D(3, 4);
        
        // Act
        var result = v1 + v2;
        
        // Assert
        result.X.Should().Be(4.0);
        result.Y.Should().Be(6.0);
    }
    
    [TestMethod]
    public void OperatorMultiply_ShouldScaleVector()
    {
        // Arrange
        var vector = new Vector2D(2, 3);
        
        // Act
        var result = vector * 2.5;
        
        // Assert
        result.X.Should().Be(5.0);
        result.Y.Should().Be(7.5);
    }
}

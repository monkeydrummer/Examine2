using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
using FluentAssertions;

namespace CAD2DModel.Tests.Services;

[TestClass]
public class GeometryEngineTests
{
    private IGeometryEngine _engine = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _engine = new GeometryEngine();
    }
    
    [TestMethod]
    public void FindIntersection_IntersectingLines_ShouldReturnIntersectionPoint()
    {
        // Arrange
        var line1 = new LineSegment(new Point2D(0, 0), new Point2D(2, 2));
        var line2 = new LineSegment(new Point2D(0, 2), new Point2D(2, 0));
        
        // Act
        var intersection = _engine.FindIntersection(line1, line2);
        
        // Assert
        intersection.Should().NotBeNull();
        intersection!.Value.X.Should().BeApproximately(1.0, 1e-10);
        intersection.Value.Y.Should().BeApproximately(1.0, 1e-10);
    }
    
    [TestMethod]
    public void FindIntersection_ParallelLines_ShouldReturnNull()
    {
        // Arrange
        var line1 = new LineSegment(new Point2D(0, 0), new Point2D(2, 0));
        var line2 = new LineSegment(new Point2D(0, 1), new Point2D(2, 1));
        
        // Act
        var intersection = _engine.FindIntersection(line1, line2);
        
        // Assert
        intersection.Should().BeNull();
    }
    
    [TestMethod]
    public void FindIntersection_NonIntersectingLines_ShouldReturnNull()
    {
        // Arrange
        var line1 = new LineSegment(new Point2D(0, 0), new Point2D(1, 0));
        var line2 = new LineSegment(new Point2D(2, 0), new Point2D(3, 0));
        
        // Act
        var intersection = _engine.FindIntersection(line1, line2);
        
        // Assert
        intersection.Should().BeNull();
    }
    
    [TestMethod]
    public void IsPointInside_PointInsideSquare_ShouldReturnTrue()
    {
        // Arrange
        var boundary = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        var point = new Point2D(5, 5);
        
        // Act
        var isInside = _engine.IsPointInside(point, boundary);
        
        // Assert
        isInside.Should().BeTrue();
    }
    
    [TestMethod]
    public void IsPointInside_PointOutsideSquare_ShouldReturnFalse()
    {
        // Arrange
        var boundary = new Boundary(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });
        
        var point = new Point2D(15, 5);
        
        // Act
        var isInside = _engine.IsPointInside(point, boundary);
        
        // Assert
        isInside.Should().BeFalse();
    }
    
    [TestMethod]
    public void DoSegmentsIntersect_IntersectingSegments_ShouldReturnTrue()
    {
        // Arrange
        var seg1 = new LineSegment(new Point2D(0, 0), new Point2D(2, 2));
        var seg2 = new LineSegment(new Point2D(0, 2), new Point2D(2, 0));
        
        // Act
        var intersects = _engine.DoSegmentsIntersect(seg1, seg2);
        
        // Assert
        intersects.Should().BeTrue();
    }
    
    [TestMethod]
    public void DistanceToLineSegment_ShouldCalculateCorrectDistance()
    {
        // Arrange
        var point = new Point2D(0, 1);
        var segment = new LineSegment(new Point2D(0, 0), new Point2D(2, 0));
        
        // Act
        var distance = _engine.DistanceToLineSegment(point, segment);
        
        // Assert
        distance.Should().BeApproximately(1.0, 1e-10);
    }
}

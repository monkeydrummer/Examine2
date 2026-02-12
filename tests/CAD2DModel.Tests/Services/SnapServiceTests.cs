using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
using FluentAssertions;

namespace CAD2DModel.Tests.Services;

[TestClass]
public class SnapServiceTests
{
    private ISnapService _snapService = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _snapService = new SnapService
        {
            SnapTolerance = 10.0,
            GridSpacing = 1.0,
            ActiveSnapModes = SnapMode.Vertex | SnapMode.Midpoint | SnapMode.Grid
        };
    }
    
    [TestMethod]
    public void SnapToVertex_NearVertex_ShouldSnapToVertex()
    {
        // Arrange
        var polyline = new Polyline(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10)
        });
        
        var point = new Point2D(0.5, 0.5);
        
        // Act
        var result = _snapService.SnapToVertex(point, new[] { polyline });
        
        // Assert
        result.Should().NotBeNull();
        result!.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Vertex);
        result.SnappedPoint.X.Should().BeApproximately(0, 1e-10);
        result.SnappedPoint.Y.Should().BeApproximately(0, 1e-10);
    }
    
    [TestMethod]
    public void SnapToVertex_FarFromVertex_ShouldReturnNull()
    {
        // Arrange
        var polyline = new Polyline(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });
        
        var point = new Point2D(15, 15);
        
        // Act
        var result = _snapService.SnapToVertex(point, new[] { polyline });
        
        // Assert
        result.Should().BeNull();
    }
    
    [TestMethod]
    public void SnapToMidpoint_NearMidpoint_ShouldSnapToMidpoint()
    {
        // Arrange
        var polyline = new Polyline(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });
        
        var point = new Point2D(5.5, 0.5);
        
        // Act
        var result = _snapService.SnapToMidpoint(point, new[] { polyline });
        
        // Assert
        result.Should().NotBeNull();
        result!.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Midpoint);
        result.SnappedPoint.X.Should().BeApproximately(5.0, 1e-10);
        result.SnappedPoint.Y.Should().BeApproximately(0.0, 1e-10);
    }
    
    [TestMethod]
    public void SnapToGrid_ShouldSnapToNearestGridPoint()
    {
        // Arrange
        var point = new Point2D(1.3, 2.7);
        
        // Act
        var result = _snapService.SnapToGrid(point);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Grid);
        result.SnappedPoint.X.Should().BeApproximately(1.0, 1e-10);
        result.SnappedPoint.Y.Should().BeApproximately(3.0, 1e-10);
    }
    
    [TestMethod]
    public void SnapToOrtho_ShouldSnapToHorizontalOrVertical()
    {
        // Arrange
        var referencePoint = new Point2D(0, 0);
        var point = new Point2D(5, 1); // Closer to horizontal
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Ortho);
        result.SnappedPoint.X.Should().BeApproximately(5.0, 1e-10);
        result.SnappedPoint.Y.Should().BeApproximately(0.0, 1e-10); // Snapped to horizontal
    }
    
    [TestMethod]
    public void Snap_WithMultipleModes_ShouldPrioritizeVertex()
    {
        // Arrange
        var polyline = new Polyline(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0)
        });
        
        var point = new Point2D(0.5, 0.5);
        
        // Act
        var result = _snapService.Snap(point, new[] { polyline });
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Vertex);
        result.SnappedPoint.Should().Be(new Point2D(0, 0));
    }
}

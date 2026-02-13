using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
using CAD2DModel.Camera;
using FluentAssertions;

namespace CAD2DModel.Tests.Services;

[TestClass]
public class SnapServiceTests
{
    private ISnapService _snapService = null!;
    private Camera2D _camera = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _snapService = new SnapService
        {
            SnapTolerancePixels = 10.0,
            VertexSnapTolerancePixels = 15.0,
            GridSpacing = 1.0,
            OrthoAngleToleranceDegrees = 5.0,
            ActiveSnapModes = SnapMode.Vertex | SnapMode.Midpoint | SnapMode.Ortho
        };
        
        // Create a test camera with scale = 1.0 (so pixels = world units)
        _camera = new Camera2D
        {
            Center = new Point2D(0, 0),
            Scale = 1.0,
            ViewportSize = new Size(800, 600)
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
        var result = _snapService.SnapToVertex(point, new[] { polyline }, _camera);
        
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
        var result = _snapService.SnapToVertex(point, new[] { polyline }, _camera);
        
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
        var result = _snapService.SnapToMidpoint(point, new[] { polyline }, _camera);
        
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
        var result = _snapService.SnapToGrid(point, _camera);
        
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
        var point = new Point2D(10, 0.5); // ~2.86 degrees, within 5 degree tolerance
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Ortho);
        result.SnappedPoint.Y.Should().BeApproximately(0.0, 1e-10); // Snapped to horizontal
    }
    
    [TestMethod]
    public void SnapToOrtho_VerticalCloser_ShouldSnapToVertical()
    {
        // Arrange
        var referencePoint = new Point2D(0, 0);
        var point = new Point2D(0.5, 10); // ~2.86 degrees from vertical
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Ortho);
        result.SnappedPoint.X.Should().BeApproximately(0.0, 1e-10); // Snapped to vertical
    }
    
    [TestMethod]
    public void SnapToOrtho_ExactlyDiagonal_ShouldSnapTo45Degrees()
    {
        // Arrange
        var referencePoint = new Point2D(0, 0);
        var point = new Point2D(10, 10); // 45 degrees - should snap to 45° diagonal
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Ortho);
        // Should maintain 45 degree angle
        var distance = Math.Sqrt(point.X * point.X + point.Y * point.Y);
        result.SnappedPoint.X.Should().BeApproximately(distance * Math.Cos(45 * Math.PI / 180.0), 1e-10);
        result.SnappedPoint.Y.Should().BeApproximately(distance * Math.Sin(45 * Math.PI / 180.0), 1e-10);
    }
    
    [TestMethod]
    public void SnapToOrtho_135Degrees_ShouldSnapToDiagonal()
    {
        // Arrange
        var referencePoint = new Point2D(0, 0);
        var angle = 135.0 * Math.PI / 180.0;
        var distance = 10.0;
        var point = new Point2D(distance * Math.Cos(angle), distance * Math.Sin(angle)); // 135 degrees
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Ortho);
        result.SnappedPoint.X.Should().BeApproximately(distance * Math.Cos(angle), 1e-8);
        result.SnappedPoint.Y.Should().BeApproximately(distance * Math.Sin(angle), 1e-8);
    }
    
    [TestMethod]
    public void SnapToOrtho_225Degrees_ShouldSnapToDiagonal()
    {
        // Arrange
        var referencePoint = new Point2D(0, 0);
        var angle = 225.0 * Math.PI / 180.0;
        var distance = 10.0;
        var point = new Point2D(distance * Math.Cos(angle), distance * Math.Sin(angle)); // 225 degrees
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Ortho);
        result.SnappedPoint.X.Should().BeApproximately(distance * Math.Cos(angle), 1e-8);
        result.SnappedPoint.Y.Should().BeApproximately(distance * Math.Sin(angle), 1e-8);
    }
    
    [TestMethod]
    public void SnapToOrtho_315Degrees_ShouldSnapToDiagonal()
    {
        // Arrange
        var referencePoint = new Point2D(0, 0);
        var angle = 315.0 * Math.PI / 180.0;
        var distance = 10.0;
        var point = new Point2D(distance * Math.Cos(angle), distance * Math.Sin(angle)); // 315 degrees
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Ortho);
        result.SnappedPoint.X.Should().BeApproximately(distance * Math.Cos(angle), 1e-8);
        result.SnappedPoint.Y.Should().BeApproximately(distance * Math.Sin(angle), 1e-8);
    }
    
    [TestMethod]
    public void SnapToOrtho_WithNonOriginReference_ShouldWorkCorrectly()
    {
        // Arrange
        var referencePoint = new Point2D(10, 10);
        var point = new Point2D(20, 10.5); // ~2.86 degrees from horizontal
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Ortho);
        result.SnappedPoint.Y.Should().BeApproximately(10.0, 1e-10); // Snapped to horizontal at Y=10
    }
    
    [TestMethod]
    public void SnapToOrtho_OutsideAngleTolerance_ShouldNotSnap()
    {
        // Arrange
        var referencePoint = new Point2D(0, 0);
        var angle = 30.0 * Math.PI / 180.0; // 30 degrees - not near any snap angle
        var distance = 10.0;
        var point = new Point2D(distance * Math.Cos(angle), distance * Math.Sin(angle));
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeFalse();
        result.SnapType.Should().Be(SnapMode.None);
        result.SnappedPoint.Should().Be(point); // Should return original point
    }
    
    [TestMethod]
    public void SnapToOrtho_JustWithinAngleTolerance_ShouldSnap()
    {
        // Arrange
        var referencePoint = new Point2D(0, 0);
        // 4 degrees from horizontal (just within 5 degree tolerance)
        var angleRadians = 4.0 * Math.PI / 180.0;
        var distance = 10.0;
        var point = new Point2D(
            distance * Math.Cos(angleRadians),
            distance * Math.Sin(angleRadians)
        );
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Ortho);
        result.SnappedPoint.Y.Should().BeApproximately(0.0, 1e-10); // Snapped to horizontal
    }
    
    [TestMethod]
    public void SnapToOrtho_JustOutsideAngleTolerance_ShouldNotSnap()
    {
        // Arrange
        var referencePoint = new Point2D(0, 0);
        // 6 degrees from horizontal (just outside 5 degree tolerance)
        var angleRadians = 6.0 * Math.PI / 180.0;
        var distance = 10.0;
        var point = new Point2D(
            distance * Math.Cos(angleRadians),
            distance * Math.Sin(angleRadians)
        );
        
        // Act
        var result = _snapService.SnapToOrtho(point, referencePoint);
        
        // Assert
        result.IsSnapped.Should().BeFalse();
        result.SnapType.Should().Be(SnapMode.None);
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
        var result = _snapService.Snap(point, new[] { polyline }, _camera);
        
        // Assert
        result.IsSnapped.Should().BeTrue();
        result.SnapType.Should().Be(SnapMode.Vertex);
        result.SnappedPoint.Should().Be(new Point2D(0, 0));
    }
}

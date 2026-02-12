using CAD2DModel.Camera;
using CAD2DModel.Geometry;
using FluentAssertions;

namespace CAD2DModel.Tests.Camera;

/// <summary>
/// Tests for zoom-to-fit functionality
/// </summary>
[TestClass]
public class ZoomFitTests
{
    /// <summary>
    /// Helper method that replicates the ZoomFit logic from MainWindow
    /// </summary>
    private static (double scale, Point2D center) CalculateZoomFit(
        Rect2D bounds, 
        Size viewportSize, 
        double marginPercent = 0.1)
    {
        // Add margin
        var expandedBounds = bounds;
        expandedBounds.Inflate(bounds.Width * marginPercent, bounds.Height * marginPercent);
        
        // Calculate scale (world units per screen pixel)
        // Higher scale means more zoomed out
        double scaleX = expandedBounds.Width / viewportSize.Width;
        double scaleY = expandedBounds.Height / viewportSize.Height;
        double scale = Math.Max(scaleX, scaleY); // Use Max to ensure everything fits
        
        return (scale, expandedBounds.Center);
    }
    
    [TestMethod]
    public void ZoomFit_SquareBoundsInSquareViewport_ShouldFitExactly()
    {
        // Arrange
        var bounds = new Rect2D(-50, -50, 100, 100); // 100x100 square centered at origin
        var viewport = new Size(800, 800); // Square viewport
        
        // Act
        var (scale, center) = CalculateZoomFit(bounds, viewport, marginPercent: 0.0);
        
        // Assert
        scale.Should().BeApproximately(0.125, 1e-10); // 100 world units / 800 pixels
        center.X.Should().Be(0);
        center.Y.Should().Be(0);
    }
    
    [TestMethod]
    public void ZoomFit_WideRectangleInSquareViewport_ShouldFitWidth()
    {
        // Arrange
        var bounds = new Rect2D(-100, -25, 200, 50); // 200x50 rectangle (wide)
        var viewport = new Size(800, 800);
        
        // Act
        var (scale, center) = CalculateZoomFit(bounds, viewport, marginPercent: 0.0);
        
        // Assert
        scale.Should().BeApproximately(0.25, 1e-10); // 200 / 800 (limited by width)
        center.X.Should().Be(0);
        center.Y.Should().Be(0);
    }
    
    [TestMethod]
    public void ZoomFit_TallRectangleInSquareViewport_ShouldFitHeight()
    {
        // Arrange
        var bounds = new Rect2D(-25, -100, 50, 200); // 50x200 rectangle (tall)
        var viewport = new Size(800, 800);
        
        // Act
        var (scale, center) = CalculateZoomFit(bounds, viewport, marginPercent: 0.0);
        
        // Assert
        scale.Should().BeApproximately(0.25, 1e-10); // 200 / 800 (limited by height)
        center.X.Should().Be(0);
        center.Y.Should().Be(0);
    }
    
    [TestMethod]
    public void ZoomFit_WithMargin_ShouldZoomOutMore()
    {
        // Arrange
        var bounds = new Rect2D(-50, -50, 100, 100);
        var viewport = new Size(800, 800);
        
        // Act
        var (scaleNoMargin, _) = CalculateZoomFit(bounds, viewport, marginPercent: 0.0);
        var (scaleWithMargin, _) = CalculateZoomFit(bounds, viewport, marginPercent: 0.1);
        
        // Assert
        scaleWithMargin.Should().BeGreaterThan(scaleNoMargin); // More zoomed out with margin
        scaleWithMargin.Should().BeApproximately(0.1375, 1e-10); // (100 * 1.2) / 800
    }
    
    [TestMethod]
    public void ZoomFit_OffsetBounds_ShouldCenterCorrectly()
    {
        // Arrange
        var bounds = new Rect2D(100, 200, 100, 100); // Square offset from origin
        var viewport = new Size(800, 800);
        
        // Act
        var (scale, center) = CalculateZoomFit(bounds, viewport, marginPercent: 0.0);
        
        // Assert
        scale.Should().BeApproximately(0.125, 1e-10);
        center.X.Should().Be(150); // Center of bounds
        center.Y.Should().Be(250);
    }
    
    [TestMethod]
    public void ZoomFit_RealWorldExample_CircleAndBox()
    {
        // Arrange - simulating the sample geometry from MainWindow
        // Circular excavation with radius 5 inside a box from -15 to 15
        var excavationBounds = new Rect2D(-5, -5, 10, 10);
        var boxBounds = new Rect2D(-15, -15, 30, 30);
        var combinedBounds = excavationBounds.Union(boxBounds);
        
        var viewport = new Size(1400, 900); // Default window size
        
        // Act
        var (scale, center) = CalculateZoomFit(combinedBounds, viewport, marginPercent: 0.1);
        
        // Assert
        combinedBounds.Width.Should().Be(30);
        combinedBounds.Height.Should().Be(30);
        
        // With 10% margin: 30 * 1.2 = 36
        // Scale should be limited by height: 36 / 900 = 0.04
        scale.Should().BeApproximately(0.04, 1e-10);
        center.Should().Be(Point2D.Zero);
    }
    
    [TestMethod]
    public void ZoomFit_VerySmallBounds_ShouldZoomInSignificantly()
    {
        // Arrange
        var bounds = new Rect2D(-1, -1, 2, 2); // Very small 2x2 square
        var viewport = new Size(800, 800);
        
        // Act
        var (scale, center) = CalculateZoomFit(bounds, viewport, marginPercent: 0.1);
        
        // Assert
        // With margin: 2 * 1.2 = 2.4
        // Scale: 2.4 / 800 = 0.003
        scale.Should().BeApproximately(0.003, 1e-10);
        scale.Should().BeLessThan(0.01); // Very zoomed in (small scale value)
    }
    
    [TestMethod]
    public void ZoomFit_VeryLargeBounds_ShouldZoomOutSignificantly()
    {
        // Arrange
        var bounds = new Rect2D(-1000, -1000, 2000, 2000); // Large 2000x2000 square
        var viewport = new Size(800, 800);
        
        // Act
        var (scale, center) = CalculateZoomFit(bounds, viewport, marginPercent: 0.1);
        
        // Assert
        // With margin: 2000 * 1.2 = 2400
        // Scale: 2400 / 800 = 3.0
        scale.Should().BeApproximately(3.0, 1e-10);
        scale.Should().BeGreaterThan(1.0); // Very zoomed out (large scale value)
    }
    
    [TestMethod]
    public void ZoomFit_WideViewport_ShouldHandleCorrectly()
    {
        // Arrange
        var bounds = new Rect2D(-50, -50, 100, 100);
        var viewport = new Size(1600, 400); // Very wide viewport (4:1 ratio)
        
        // Act
        var (scale, center) = CalculateZoomFit(bounds, viewport, marginPercent: 0.0);
        
        // Assert
        // Width scale: 100/1600 = 0.0625
        // Height scale: 100/400 = 0.25
        // Should use height (larger value)
        scale.Should().BeApproximately(0.25, 1e-10);
    }
    
    [TestMethod]
    public void ZoomFit_TallViewport_ShouldHandleCorrectly()
    {
        // Arrange
        var bounds = new Rect2D(-50, -50, 100, 100);
        var viewport = new Size(400, 1600); // Very tall viewport (1:4 ratio)
        
        // Act
        var (scale, center) = CalculateZoomFit(bounds, viewport, marginPercent: 0.0);
        
        // Assert
        // Width scale: 100/400 = 0.25
        // Height scale: 100/1600 = 0.0625
        // Should use width (larger value)
        scale.Should().BeApproximately(0.25, 1e-10);
    }
    
    [TestMethod]
    public void ZoomFit_ScaleInterpretation_HigherScaleMeansMoreZoomedOut()
    {
        // Arrange
        var smallBounds = new Rect2D(-10, -10, 20, 20);
        var largeBounds = new Rect2D(-100, -100, 200, 200);
        var viewport = new Size(800, 800);
        
        // Act
        var (smallScale, _) = CalculateZoomFit(smallBounds, viewport, marginPercent: 0.0);
        var (largeScale, _) = CalculateZoomFit(largeBounds, viewport, marginPercent: 0.0);
        
        // Assert
        // Larger bounds require higher scale (more zoomed out)
        largeScale.Should().BeGreaterThan(smallScale);
        
        smallScale.Should().BeApproximately(0.025, 1e-10); // 20/800
        largeScale.Should().BeApproximately(0.25, 1e-10);  // 200/800
    }
    
    [TestMethod]
    public void ZoomFit_EnsuresAllContentVisible_NotJustPartial()
    {
        // Arrange - rectangular bounds that need to fit completely
        var bounds = new Rect2D(0, 0, 1200, 600); // Wider than tall
        var viewport = new Size(800, 600);
        
        // Act
        var (scale, center) = CalculateZoomFit(bounds, viewport, marginPercent: 0.0);
        
        // Assert
        // Width scale: 1200/800 = 1.5
        // Height scale: 600/600 = 1.0
        // Must use 1.5 (Max) to ensure width fits
        scale.Should().BeApproximately(1.5, 1e-10);
        
        // Verify the entire bounds would be visible
        var camera = new Camera2D
        {
            ViewportSize = viewport,
            Scale = scale,
            Center = center
        };
        
        var worldBounds = camera.WorldBounds;
        worldBounds.Width.Should().BeGreaterThanOrEqualTo(bounds.Width);
        worldBounds.Height.Should().BeGreaterThanOrEqualTo(bounds.Height);
    }
}

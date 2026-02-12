using CAD2DModel.Camera;
using CAD2DModel.Geometry;
using FluentAssertions;

namespace CAD2DModel.Tests.Camera;

[TestClass]
public class Camera2DTests
{
    [TestMethod]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var camera = new Camera2D();
        
        // Assert
        camera.Center.Should().Be(Point2D.Zero);
        camera.Scale.Should().Be(1.0);
        camera.ViewportSize.Width.Should().Be(800);
        camera.ViewportSize.Height.Should().Be(600);
    }
    
    [TestMethod]
    public void WorldWidth_ShouldCalculateCorrectly()
    {
        // Arrange
        var camera = new Camera2D
        {
            ViewportSize = new Size(800, 600),
            Scale = 2.0 // 2 world units per pixel
        };
        
        // Act
        var worldWidth = camera.WorldWidth;
        
        // Assert
        worldWidth.Should().Be(1600.0); // 800 pixels * 2 world units/pixel
    }
    
    [TestMethod]
    public void WorldHeight_ShouldCalculateCorrectly()
    {
        // Arrange
        var camera = new Camera2D
        {
            ViewportSize = new Size(800, 600),
            Scale = 2.0 // 2 world units per pixel
        };
        
        // Act
        var worldHeight = camera.WorldHeight;
        
        // Assert
        worldHeight.Should().Be(1200.0); // 600 pixels * 2 world units/pixel
    }
    
    [TestMethod]
    public void WorldBounds_ShouldCalculateCorrectly()
    {
        // Arrange
        var camera = new Camera2D
        {
            Center = new Point2D(100, 200),
            ViewportSize = new Size(800, 600),
            Scale = 1.0
        };
        
        // Act
        var bounds = camera.WorldBounds;
        
        // Assert
        bounds.X.Should().Be(-300); // 100 - 800/2
        bounds.Y.Should().Be(-100); // 200 - 600/2
        bounds.Width.Should().Be(800);
        bounds.Height.Should().Be(600);
    }
    
    [TestMethod]
    [DataRow(0.0, 0.0, 1.0, 400.0, 300.0, 400.0, 300.0)] // Center at origin, scale 1.0
    [DataRow(100.0, 200.0, 1.0, 400.0, 300.0, 500.0, 500.0)] // Offset center
    [DataRow(0.0, 0.0, 2.0, 400.0, 300.0, 200.0, 150.0)] // Zoomed out (scale=2)
    [DataRow(0.0, 0.0, 0.5, 400.0, 300.0, 800.0, 600.0)] // Zoomed in (scale=0.5)
    public void ScreenToWorld_VariousInputs_ShouldConvertCorrectly(
        double centerX, double centerY, double scale,
        double screenX, double screenY,
        double expectedWorldX, double expectedWorldY)
    {
        // Arrange
        var camera = new Camera2D
        {
            Center = new Point2D(centerX, centerY),
            ViewportSize = new Size(800, 600),
            Scale = scale
        };
        
        // Act
        var worldPoint = camera.ScreenToWorld(new Point(screenX, screenY));
        
        // Assert
        worldPoint.X.Should().BeApproximately(expectedWorldX, 1e-10);
        worldPoint.Y.Should().BeApproximately(expectedWorldY, 1e-10);
    }
    
    [TestMethod]
    [DataRow(0.0, 0.0, 1.0, 400.0, 300.0, 400.0, 300.0)] // Center at origin, scale 1.0
    [DataRow(100.0, 200.0, 1.0, 500.0, 500.0, 400.0, 300.0)] // Offset center
    [DataRow(0.0, 0.0, 2.0, 800.0, 600.0, 400.0, 300.0)] // Zoomed out (scale=2)
    [DataRow(0.0, 0.0, 0.5, 200.0, 150.0, 400.0, 300.0)] // Zoomed in (scale=0.5)
    public void WorldToScreen_VariousInputs_ShouldConvertCorrectly(
        double centerX, double centerY, double scale,
        double worldX, double worldY,
        double expectedScreenX, double expectedScreenY)
    {
        // Arrange
        var camera = new Camera2D
        {
            Center = new Point2D(centerX, centerY),
            ViewportSize = new Size(800, 600),
            Scale = scale
        };
        
        // Act
        var screenPoint = camera.WorldToScreen(new Point2D(worldX, worldY));
        
        // Assert
        screenPoint.X.Should().BeApproximately(expectedScreenX, 1e-10);
        screenPoint.Y.Should().BeApproximately(expectedScreenY, 1e-10);
    }
    
    [TestMethod]
    public void ScreenToWorld_ThenWorldToScreen_ShouldReturnOriginalPoint()
    {
        // Arrange
        var camera = new Camera2D
        {
            Center = new Point2D(100, 200),
            ViewportSize = new Size(800, 600),
            Scale = 1.5
        };
        var originalScreen = new Point(250, 350);
        
        // Act
        var worldPoint = camera.ScreenToWorld(originalScreen);
        var backToScreen = camera.WorldToScreen(worldPoint);
        
        // Assert
        backToScreen.X.Should().BeApproximately(originalScreen.X, 1e-10);
        backToScreen.Y.Should().BeApproximately(originalScreen.Y, 1e-10);
    }
    
    [TestMethod]
    public void ZoomIn_ShouldDecreaseScale()
    {
        // Arrange
        var camera = new Camera2D { Scale = 1.0 };
        double zoomFactor = 0.8;
        
        // Act
        camera.Scale *= zoomFactor;
        
        // Assert
        camera.Scale.Should().Be(0.8); // Less world units per pixel = zoomed in
    }
    
    [TestMethod]
    public void ZoomOut_ShouldIncreaseScale()
    {
        // Arrange
        var camera = new Camera2D { Scale = 1.0 };
        double zoomFactor = 1.25;
        
        // Act
        camera.Scale *= zoomFactor;
        
        // Assert
        camera.Scale.Should().Be(1.25); // More world units per pixel = zoomed out
    }
}

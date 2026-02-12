using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Camera;

/// <summary>
/// 2D camera for viewport navigation
/// </summary>
public partial class Camera2D : ObservableObject
{
    [ObservableProperty]
    private Point2D _center = Point2D.Zero;
    
    [ObservableProperty]
    private double _scale = 1.0; // world units per screen pixel
    
    [ObservableProperty]
    private Size _viewportSize = new Size(800, 600);
    
    public double WorldWidth => ViewportSize.Width * Scale;
    public double WorldHeight => ViewportSize.Height * Scale;
    
    public Rect2D WorldBounds => new(
        Center.X - WorldWidth / 2,
        Center.Y - WorldHeight / 2,
        WorldWidth,
        WorldHeight
    );
    
    public Point2D ScreenToWorld(Point screenPoint)
    {
        double worldX = Center.X + (screenPoint.X - ViewportSize.Width / 2) * Scale;
        double worldY = Center.Y + (screenPoint.Y - ViewportSize.Height / 2) * Scale;
        return new Point2D(worldX, worldY);
    }
    
    public Point WorldToScreen(Point2D worldPoint)
    {
        double screenX = (worldPoint.X - Center.X) / Scale + ViewportSize.Width / 2;
        double screenY = (worldPoint.Y - Center.Y) / Scale + ViewportSize.Height / 2;
        return new Point(screenX, screenY);
    }
    
    public Transform2D GetWorldToScreenTransform()
    {
        // Translate to origin, scale, translate to screen center
        double scaleInv = 1.0 / Scale;
        return new Transform2D(
            scaleInv, 0,
            0, scaleInv,
            -Center.X * scaleInv + ViewportSize.Width / 2,
            -Center.Y * scaleInv + ViewportSize.Height / 2
        );
    }
    
    public Transform2D GetScreenToWorldTransform()
    {
        // Translate from screen center, scale, translate to world center
        return new Transform2D(
            Scale, 0,
            0, Scale,
            Center.X - ViewportSize.Width * Scale / 2,
            Center.Y - ViewportSize.Height * Scale / 2
        );
    }
}

/// <summary>
/// Simple Size structure
/// </summary>
public record struct Size(double Width, double Height);

/// <summary>
/// Simple Point structure for screen coordinates
/// </summary>
public record struct Point(double X, double Y);

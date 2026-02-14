using SkiaSharp;

namespace CAD2DView.Rendering;

/// <summary>
/// Configuration settings for ruler display
/// </summary>
public class RulerConfiguration
{
    /// <summary>
    /// Gets or sets whether the ruler is visible
    /// </summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the ruler width in inches (for left ruler)
    /// </summary>
    public double WidthInches { get; set; } = 0.19;
    
    /// <summary>
    /// Gets or sets the ruler height in inches (for bottom ruler)
    /// </summary>
    public double HeightInches { get; set; } = 0.19;
    
    /// <summary>
    /// Gets or sets the background color of the ruler
    /// </summary>
    public SKColor BackgroundColor { get; set; } = SKColors.White;
    
    /// <summary>
    /// Gets or sets whether to show the mouse crosshair on the ruler
    /// </summary>
    public bool ShowCrosshair { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the tick sizes for metric style (10 ticks between major divisions).
    /// Index 0: major tick (height 5)
    /// Index 5: mid-point tick (height 4)
    /// Other indices: minor ticks (height 2)
    /// </summary>
    public int[] TickSizes { get; set; } = { 5, 2, 2, 2, 2, 4, 2, 2, 2, 2 };
    
    /// <summary>
    /// Gets or sets the DPI (dots per inch) for converting inches to pixels.
    /// Default is 96 DPI which is standard for Windows displays.
    /// </summary>
    public double Dpi { get; set; } = 96.0;
}

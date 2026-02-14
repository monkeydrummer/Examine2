using CAD2DModel.Camera;
using CAD2DModel.Geometry;
using CAD2DModel.Rendering;
using SkiaSharp;

namespace CAD2DView.Rendering;

/// <summary>
/// Renders ruler with tick marks and labels on the left and bottom edges of the viewport.
/// Ported from C++ CWSAxis::GDI_draw functionality.
/// </summary>
public class RulerRenderer
{
    /// <summary>
    /// Renders the ruler on the canvas with automatic tick spacing and optional mouse crosshair.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
    /// <param name="camera">The camera for world-to-screen transformations.</param>
    /// <param name="config">The ruler configuration settings.</param>
    /// <param name="mousePosition">Optional mouse position in screen coordinates for crosshair display.</param>
    public void Render(SKCanvas canvas, Camera2D camera, RulerConfiguration config, SKPoint? mousePosition = null)
    {
        if (!config.IsVisible)
            return;

        var viewportSize = camera.ViewportSize;
        
        // Step 1: Calculate ruler size in pixels
        int rulerWidthPixels = (int)(config.WidthInches * config.Dpi);
        int rulerHeightPixels = (int)(config.HeightInches * config.Dpi);
        
        // Step 2: Draw background blocks
        DrawBackgroundBlocks(canvas, rulerWidthPixels, rulerHeightPixels, viewportSize, config.BackgroundColor);
        
        // Step 3: Draw black border edges
        DrawBorderEdges(canvas, rulerWidthPixels, rulerHeightPixels, viewportSize);
        
        // Step 4: Calculate tick spacing with NiceInterval
        var (minX, maxX, deltaX, minY, maxY, deltaY, numTicksX, numTicksY) = 
            CalculateTickSpacing(camera, config, viewportSize);
        
        // Step 5: Draw vertical ruler (left edge)
        DrawVerticalRuler(canvas, camera, config, rulerWidthPixels, rulerHeightPixels, 
                         minY, maxY, deltaY, numTicksY, viewportSize);
        
        // Step 6: Draw horizontal ruler (bottom edge)
        DrawHorizontalRuler(canvas, camera, config, rulerWidthPixels, rulerHeightPixels, 
                           minX, maxX, deltaX, numTicksX, viewportSize);
        
        // Step 7: Draw mouse crosshair (if enabled)
        if (config.ShowCrosshair && mousePosition.HasValue)
        {
            DrawMouseCrosshair(canvas, mousePosition.Value, rulerWidthPixels, rulerHeightPixels, viewportSize);
        }
    }
    
    private void DrawBackgroundBlocks(SKCanvas canvas, int rulerWidth, int rulerHeight, 
                                     Size viewportSize, SKColor backgroundColor)
    {
        using var backgroundPaint = new SKPaint
        {
            Color = backgroundColor.WithAlpha(255), // Ensure fully opaque
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Src // Use source blending to ensure opacity
        };
        
        // Left ruler area (vertical)
        var leftRect = new SKRect(0, 0, rulerWidth, (float)viewportSize.Height);
        canvas.DrawRect(leftRect, backgroundPaint);
        
        // Bottom ruler area (horizontal)
        var bottomRect = new SKRect(0, (float)viewportSize.Height - rulerHeight, 
                                   (float)viewportSize.Width, (float)viewportSize.Height);
        canvas.DrawRect(bottomRect, backgroundPaint);
    }
    
    private void DrawBorderEdges(SKCanvas canvas, int rulerWidth, int rulerHeight, Size viewportSize)
    {
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            IsAntialias = false
        };
        
        // Right edge of left ruler
        canvas.DrawLine(rulerWidth, 0, rulerWidth, 
                       (float)viewportSize.Height - rulerHeight, borderPaint);
        
        // Top edge of bottom ruler
        canvas.DrawLine(rulerWidth, (float)viewportSize.Height - rulerHeight, 
                       (float)viewportSize.Width, (float)viewportSize.Height - rulerHeight, borderPaint);
    }
    
    private (double minX, double maxX, double deltaX, double minY, double maxY, double deltaY, 
             int numTicksX, int numTicksY) 
        CalculateTickSpacing(Camera2D camera, RulerConfiguration config, Size viewportSize)
    {
        // Calculate screen size in inches
        double screenWidthInches = viewportSize.Width / config.Dpi;
        double screenHeightInches = viewportSize.Height / config.Dpi;
        
        // Determine number of major ticks (approximately 1.4 ticks per inch)
        int numTicksX = Math.Max(1, (int)(screenWidthInches * 1.4));
        int numTicksY = Math.Max(1, (int)(screenHeightInches * 1.4));
        
        // Get world bounds
        var worldBounds = camera.WorldBounds;
        
        // Calculate nice intervals
        double minX = worldBounds.X;
        double maxX = worldBounds.Right;
        double minY = worldBounds.Y;
        double maxY = worldBounds.Bottom;
        
        NiceInterval.CalculateNiceInterval(ref minX, ref maxX, out double deltaX, numTicksX);
        NiceInterval.CalculateNiceInterval(ref minY, ref maxY, out double deltaY, numTicksY);
        
        return (minX, maxX, deltaX, minY, maxY, deltaY, numTicksX, numTicksY);
    }
    
    private void DrawVerticalRuler(SKCanvas canvas, Camera2D camera, RulerConfiguration config,
                                  int rulerWidth, int rulerHeight, double minY, double maxY, 
                                  double deltaY, int numTicks, Size viewportSize)
    {
        using var tickPaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            IsAntialias = false
        };
        
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 8 * (float)config.Dpi / 72, // 8 point font
            IsAntialias = true,
            TextAlign = SKTextAlign.Right, // Right align so text ends at the tick position
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        
        int yIndent = rulerWidth - 2;
        int yOffset = (int)viewportSize.Height - rulerHeight;
        
        // Draw ticks and labels from bottom to top
        for (int j = numTicks - 1; j >= 0; j--)
        {
            double worldY = minY + j * deltaY;
            var screenPt = camera.WorldToScreen(new Point2D(0, worldY));
            int thisMajorTickY = (int)screenPt.Y;
            
            var nextScreenPt = camera.WorldToScreen(new Point2D(0, minY + (j + 1) * deltaY));
            int nextMajorTickY = (int)nextScreenPt.Y;
            
            // Draw label if tick is within visible area
            if (thisMajorTickY < yOffset && thisMajorTickY > 5)
            {
                string text = FormatTickLabel(worldY);
                
                // Draw rotated text (90 degrees counter-clockwise)
                // Text is right-aligned, so it will end at position 0 (which is the tick mark)
                canvas.Save();
                canvas.Translate(4, thisMajorTickY);
                canvas.RotateDegrees(-90);
                canvas.DrawText(text, -4, textPaint.TextSize * 0.35f, textPaint);
                canvas.Restore();
            }
            
            // Draw sub-ticks
            for (int i = 0; i < config.TickSizes.Length; i++)
            {
                int tickLocation = thisMajorTickY + 
                    (nextMajorTickY - thisMajorTickY) * i / config.TickSizes.Length;
                
                if (tickLocation < yOffset && tickLocation > 0)
                {
                    canvas.DrawLine(yIndent, tickLocation, 
                                   yIndent - config.TickSizes[i], tickLocation, tickPaint);
                }
            }
        }
    }
    
    private void DrawHorizontalRuler(SKCanvas canvas, Camera2D camera, RulerConfiguration config,
                                    int rulerWidth, int rulerHeight, double minX, double maxX, 
                                    double deltaX, int numTicks, Size viewportSize)
    {
        using var tickPaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            IsAntialias = false
        };
        
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 8 * (float)config.Dpi / 72, // 8 point font
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        
        int yOffset = (int)viewportSize.Height - rulerHeight + 2;
        int leftSide = rulerWidth;
        int rightSide = (int)viewportSize.Width;
        
        // Draw ticks and labels from left to right
        for (int j = 0; j < numTicks; j++)
        {
            double worldX = minX + j * deltaX;
            var screenPt = camera.WorldToScreen(new Point2D(worldX, 0));
            int thisMajorTickX = (int)screenPt.X;
            
            var nextScreenPt = camera.WorldToScreen(new Point2D(minX + (j + 1) * deltaX, 0));
            int nextMajorTickX = (int)nextScreenPt.X;
            
            // Draw label if tick is within visible area
            if (thisMajorTickX > leftSide && thisMajorTickX < rightSide)
            {
                string text = FormatTickLabel(worldX);
                canvas.DrawText(text, thisMajorTickX, 
                              (float)viewportSize.Height - 2, textPaint);
            }
            
            // Draw sub-ticks
            for (int i = 0; i < config.TickSizes.Length; i++)
            {
                int tickLocation = thisMajorTickX + 
                    (nextMajorTickX - thisMajorTickX) * i / config.TickSizes.Length;
                
                if (tickLocation > leftSide && tickLocation < rightSide)
                {
                    canvas.DrawLine(tickLocation, yOffset, 
                                   tickLocation, yOffset + config.TickSizes[i], tickPaint);
                }
            }
        }
    }
    
    private void DrawMouseCrosshair(SKCanvas canvas, SKPoint mousePos, 
                                   int rulerWidth, int rulerHeight, Size viewportSize)
    {
        using var crosshairPaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 0),
            IsAntialias = false
        };
        
        // Horizontal line on left ruler
        canvas.DrawLine(0, mousePos.Y, rulerWidth, mousePos.Y, crosshairPaint);
        
        // Vertical line on bottom ruler
        float bottomY = (float)viewportSize.Height - rulerHeight;
        canvas.DrawLine(mousePos.X, bottomY, mousePos.X, (float)viewportSize.Height, crosshairPaint);
    }
    
    private string FormatTickLabel(double value)
    {
        // Format similar to C++ %g format: auto-select decimal places
        // Remove trailing zeros and unnecessary decimal point
        if (Math.Abs(value) < 1e-10)
            return "0";
        
        // For small numbers, use standard notation
        if (Math.Abs(value) >= 0.01 && Math.Abs(value) < 10000)
        {
            string formatted = value.ToString("G6"); // Up to 6 significant figures
            
            // Remove trailing zeros after decimal point
            if (formatted.Contains('.'))
            {
                formatted = formatted.TrimEnd('0').TrimEnd('.');
            }
            
            return formatted;
        }
        
        // For very large or very small numbers, use scientific notation
        return value.ToString("G4");
    }
}

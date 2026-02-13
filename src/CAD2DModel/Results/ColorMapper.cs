namespace CAD2DModel.Results;

/// <summary>
/// Represents an RGB color
/// </summary>
public struct ColorRGB
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    
    public ColorRGB(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }
    
    public static ColorRGB FromRGB(byte r, byte g, byte b) => new ColorRGB(r, g, b);
}

/// <summary>
/// Color schemes for contour visualization
/// </summary>
public enum ColorScheme
{
    /// <summary>
    /// Rainbow: blue -> cyan -> green -> yellow -> red
    /// </summary>
    Rainbow,
    
    /// <summary>
    /// Jet: similar to rainbow but with darker colors
    /// </summary>
    Jet,
    
    /// <summary>
    /// Viridis: perceptually uniform, colorblind-friendly
    /// </summary>
    Viridis,
    
    /// <summary>
    /// Grayscale: black to white
    /// </summary>
    Grayscale,
    
    /// <summary>
    /// Cool to warm: blue -> white -> red
    /// </summary>
    CoolWarm
}

/// <summary>
/// Maps scalar values to colors for contour visualization
/// </summary>
public class ColorMapper
{
    public ColorScheme Scheme { get; set; } = ColorScheme.Jet;
    
    /// <summary>
    /// Map a normalized value (0.0 to 1.0) to a color
    /// </summary>
    public ColorRGB GetColor(double normalizedValue)
    {
        // Clamp to [0, 1]
        normalizedValue = Math.Max(0.0, Math.Min(1.0, normalizedValue));
        
        return Scheme switch
        {
            ColorScheme.Rainbow => GetRainbowColor(normalizedValue),
            ColorScheme.Jet => GetJetColor(normalizedValue),
            ColorScheme.Viridis => GetViridisColor(normalizedValue),
            ColorScheme.Grayscale => GetGrayscaleColor(normalizedValue),
            ColorScheme.CoolWarm => GetCoolWarmColor(normalizedValue),
            _ => GetJetColor(normalizedValue)
        };
    }
    
    /// <summary>
    /// Map a value in a given range to a color
    /// </summary>
    public ColorRGB MapValue(double value, double minValue, double maxValue)
    {
        if (Math.Abs(maxValue - minValue) < 1e-10)
            return new ColorRGB(128, 128, 128);
        
        double normalized = (value - minValue) / (maxValue - minValue);
        return GetColor(normalized);
    }
    
    /// <summary>
    /// Get contour levels for a given range
    /// </summary>
    public List<double> GetContourLevels(double minValue, double maxValue, int numLevels)
    {
        var levels = new List<double>();
        
        if (numLevels < 2)
            numLevels = 2;
        
        for (int i = 0; i <= numLevels; i++)
        {
            double level = minValue + (maxValue - minValue) * i / numLevels;
            levels.Add(level);
        }
        
        return levels;
    }
    
    private ColorRGB GetRainbowColor(double t)
    {
        // Blue -> Cyan -> Green -> Yellow -> Red
        if (t < 0.25)
        {
            // Blue to Cyan
            double local = t / 0.25;
            return new ColorRGB(0, (byte)(255 * local), 255);
        }
        else if (t < 0.5)
        {
            // Cyan to Green
            double local = (t - 0.25) / 0.25;
            return new ColorRGB(0, 255, (byte)(255 * (1 - local)));
        }
        else if (t < 0.75)
        {
            // Green to Yellow
            double local = (t - 0.5) / 0.25;
            return new ColorRGB((byte)(255 * local), 255, 0);
        }
        else
        {
            // Yellow to Red
            double local = (t - 0.75) / 0.25;
            return new ColorRGB(255, (byte)(255 * (1 - local)), 0);
        }
    }
    
    private ColorRGB GetJetColor(double t)
    {
        // Classic Jet colormap
        double r = Math.Max(0, Math.Min(1, 1.5 - Math.Abs(4 * t - 3)));
        double g = Math.Max(0, Math.Min(1, 1.5 - Math.Abs(4 * t - 2)));
        double b = Math.Max(0, Math.Min(1, 1.5 - Math.Abs(4 * t - 1)));
        
        return new ColorRGB((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
    
    private ColorRGB GetViridisColor(double t)
    {
        // Simplified Viridis approximation (actual uses lookup table)
        // This is a polynomial approximation
        double r = 0.282 + 0.277 * t - 0.233 * t * t;
        double g = 0.005 + 0.559 * t + 0.436 * t * t;
        double b = 0.333 + 0.338 * t - 0.671 * t * t;
        
        r = Math.Max(0, Math.Min(1, r));
        g = Math.Max(0, Math.Min(1, g));
        b = Math.Max(0, Math.Min(1, b));
        
        return new ColorRGB((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
    
    private ColorRGB GetGrayscaleColor(double t)
    {
        byte gray = (byte)(t * 255);
        return new ColorRGB(gray, gray, gray);
    }
    
    private ColorRGB GetCoolWarmColor(double t)
    {
        // Blue -> White -> Red
        if (t < 0.5)
        {
            // Blue to White
            double local = t * 2;
            byte r = (byte)(255 * local);
            byte g = (byte)(255 * local);
            byte b = 255;
            return new ColorRGB(r, g, b);
        }
        else
        {
            // White to Red
            double local = (t - 0.5) * 2;
            byte r = 255;
            byte g = (byte)(255 * (1 - local));
            byte b = (byte)(255 * (1 - local));
            return new ColorRGB(r, g, b);
        }
    }
}

namespace CAD2DModel.Results;

/// <summary>
/// Settings for contour visualization
/// </summary>
public class ContourSettings
{
    /// <summary>
    /// Whether contours are currently visible
    /// </summary>
    public bool IsVisible { get; set; } = false;
    
    /// <summary>
    /// The result field to display
    /// </summary>
    public ResultField Field { get; set; } = ResultField.VonMisesStress;
    
    /// <summary>
    /// Number of contour levels to display
    /// </summary>
    public int NumberOfLevels { get; set; } = 10;
    
    /// <summary>
    /// Whether to show filled contours (true) or just contour lines (false)
    /// </summary>
    public bool ShowFilledContours { get; set; } = true;
    
    /// <summary>
    /// Whether to show contour lines
    /// </summary>
    public bool ShowContourLines { get; set; } = true;
    
    /// <summary>
    /// Opacity of filled contours (0.0 to 1.0)
    /// </summary>
    public double FillOpacity { get; set; } = 1.0;
    
    /// <summary>
    /// Color scheme for contour visualization
    /// </summary>
    public ColorScheme ColorScheme { get; set; } = ColorScheme.Rainbow;
    
    /// <summary>
    /// Minimum value for contour range (null = auto)
    /// </summary>
    public double? MinValue { get; set; } = null;
    
    /// <summary>
    /// Maximum value for contour range (null = auto)
    /// </summary>
    public double? MaxValue { get; set; } = null;
}

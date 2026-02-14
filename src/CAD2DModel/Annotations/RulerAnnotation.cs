using CAD2DModel.Geometry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CAD2DModel.Annotations;

/// <summary>
/// Ruler annotation with distance measurement display
/// </summary>
public partial class RulerAnnotation : LinearAnnotation
{
    [ObservableProperty]
    private int _decimalPlaces = 2;
    
    [ObservableProperty]
    private bool _showUnits = true;
    
    [ObservableProperty]
    private string _units = "mm";
    
    [ObservableProperty]
    private bool _convertToInches;
    
    [ObservableProperty]
    private bool _showAngle;
    
    public RulerAnnotation() : base()
    {
        // Default ruler appearance
        Color = Color.Black;
        LineWeight = 1.5f;
        FontSize = 10f;
        FontFamily = "Arial";
        DrawTextBackground = true;
    }
    
    public RulerAnnotation(Point2D start, Point2D end) : base(start, end)
    {
        // Default ruler appearance
        Color = Color.Black;
        LineWeight = 1.5f;
        FontSize = 10f;
        FontFamily = "Arial";
        DrawTextBackground = true;
    }
    
    /// <summary>
    /// Get the formatted measurement text
    /// </summary>
    public string GetMeasurementText()
    {
        double distance = Length;
        
        // Convert to inches if requested
        if (ConvertToInches)
        {
            distance = distance / 25.4; // Convert mm to inches
        }
        
        // Format the number
        string format = $"F{DecimalPlaces}";
        string measurement = distance.ToString(format);
        
        // Add units if requested
        string unitsText = ConvertToInches ? "in" : Units;
        if (ShowUnits)
        {
            measurement += " " + unitsText;
        }
        
        // Add angle if requested
        if (ShowAngle)
        {
            double angle = AngleDegrees;
            measurement += $" ∠{angle:F1}°";
        }
        
        return measurement;
    }
    
    /// <summary>
    /// Override to automatically update text with measurement
    /// </summary>
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        // Update text when relevant properties change
        if (e.PropertyName == nameof(StartPoint) ||
            e.PropertyName == nameof(EndPoint) ||
            e.PropertyName == nameof(DecimalPlaces) ||
            e.PropertyName == nameof(ShowUnits) ||
            e.PropertyName == nameof(ConvertToInches) ||
            e.PropertyName == nameof(ShowAngle))
        {
            Text = GetMeasurementText();
        }
    }
}

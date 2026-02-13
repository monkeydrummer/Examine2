using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CAD2DModel.Results;

namespace Examine2DView.Controls;

/// <summary>
/// Control that displays a color legend for contour visualization
/// </summary>
public partial class ContourLegendControl : UserControl
{
    private ColorMapper _colorMapper = new ColorMapper();
    private double _currentMinValue;
    private double _currentMaxValue;
    private int _currentNumLevels = 10;
    
    public ContourLegendControl()
    {
        InitializeComponent();
        Loaded += (s, e) => UpdateLegend();
        SizeChanged += (s, e) => RedrawWithCurrentData();
    }
    
    private void ResizeThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        // Resize the control
        double newWidth = Width + e.HorizontalChange;
        double newHeight = Height + e.VerticalChange;
        
        // Apply constraints
        Width = Math.Max(MinWidth, newWidth);
        Height = Math.Max(MinHeight, newHeight);
    }
    
    /// <summary>
    /// Update the legend with contour data
    /// </summary>
    public void UpdateLegend(ContourData? contourData, ContourSettings settings)
    {
        if (contourData == null || !contourData.IsValid)
        {
            Visibility = Visibility.Collapsed;
            return;
        }
        
        Visibility = settings.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        
        if (!settings.IsVisible)
            return;
        
        // Update title
        TitleText.Text = GetFieldDisplayName(settings.Field);
        UnitsText.Text = GetFieldUnits(settings.Field);
        
        // Determine value range
        double minValue = settings.MinValue ?? contourData.MinValue;
        double maxValue = settings.MaxValue ?? contourData.MaxValue;
        
        // Store for redrawing on resize
        _currentMinValue = minValue;
        _currentMaxValue = maxValue;
        _currentNumLevels = settings.NumberOfLevels;
        
        // Update min/max value labels
        MaxValueText.Text = $"Max: {FormatValue(maxValue)}";
        MinValueText.Text = $"Min: {FormatValue(minValue)}";
        
        // Update color mapper scheme
        _colorMapper.Scheme = settings.ColorScheme;
        
        // Draw color bar
        DrawColorBar(minValue, maxValue);
        
        // Draw labels
        DrawLabels(minValue, maxValue, settings.NumberOfLevels);
    }
    
    private void UpdateLegend()
    {
        // Initial empty update
    }
    
    private void RedrawWithCurrentData()
    {
        // Redraw with current cached values when control is resized
        if (_currentMinValue != 0 || _currentMaxValue != 0)
        {
            DrawColorBar(_currentMinValue, _currentMaxValue);
            DrawLabels(_currentMinValue, _currentMaxValue, _currentNumLevels);
        }
    }
    
    private void DrawColorBar(double minValue, double maxValue)
    {
        ColorBarCanvas.Children.Clear();
        
        double height = ColorBarCanvas.ActualHeight > 0 ? ColorBarCanvas.ActualHeight : 200;
        double width = ColorBarCanvas.Width;
        
        // Draw color gradient from top (max) to bottom (min)
        int numSegments = 100;
        double segmentHeight = height / numSegments;
        
        for (int i = 0; i < numSegments; i++)
        {
            // Normalized value (0 = min, 1 = max)
            // Top of canvas = max value, bottom = min value
            double normalizedValue = 1.0 - (double)i / numSegments;
            
            var color = _colorMapper.GetColor(normalizedValue);
            var brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
            
            var rect = new Rectangle
            {
                Width = width,
                Height = segmentHeight + 1, // +1 to avoid gaps
                Fill = brush
            };
            
            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, i * segmentHeight);
            
            ColorBarCanvas.Children.Add(rect);
        }
        
        // Draw border around color bar
        var border = new Rectangle
        {
            Width = width,
            Height = height,
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            Fill = Brushes.Transparent
        };
        
        ColorBarCanvas.Children.Add(border);
    }
    
    private void DrawLabels(double minValue, double maxValue, int numLevels)
    {
        LabelsCanvas.Children.Clear();
        
        double height = ColorBarCanvas.ActualHeight > 0 ? ColorBarCanvas.ActualHeight : 200;
        
        // Create labels at contour levels
        var levels = _colorMapper.GetContourLevels(minValue, maxValue, numLevels);
        
        // Add labels from max to min (top to bottom)
        foreach (var value in levels)
        {
            double normalizedPos = (value - minValue) / (maxValue - minValue);
            double yPos = height * (1.0 - normalizedPos);
            
            var label = new TextBlock
            {
                Text = FormatValue(value),
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Top
            };
            
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, yPos - 6); // -6 to center vertically on the level
            
            LabelsCanvas.Children.Add(label);
        }
    }
    
    private string FormatValue(double value)
    {
        if (Math.Abs(value) < 0.01)
            return "0.00";
        else if (Math.Abs(value) < 1)
            return value.ToString("F3");
        else if (Math.Abs(value) < 10)
            return value.ToString("F2");
        else if (Math.Abs(value) < 100)
            return value.ToString("F1");
        else
            return value.ToString("F0");
    }
    
    private string GetFieldDisplayName(ResultField field)
    {
        return field switch
        {
            ResultField.VonMisesStress => "Von Mises\nStress",
            ResultField.PrincipalStress1 => "Principal\nStress σ₁",
            ResultField.PrincipalStress3 => "Principal\nStress σ₃",
            ResultField.StressX => "Stress σₓ",
            ResultField.StressY => "Stress σᵧ",
            ResultField.StressXY => "Shear\nStress τₓᵧ",
            ResultField.DisplacementMagnitude => "Displacement\nMagnitude",
            ResultField.DisplacementX => "Displacement\nX",
            ResultField.DisplacementY => "Displacement\nY",
            _ => field.ToString()
        };
    }
    
    private string GetFieldUnits(ResultField field)
    {
        return field switch
        {
            ResultField.VonMisesStress => "MPa",
            ResultField.PrincipalStress1 => "MPa",
            ResultField.PrincipalStress3 => "MPa",
            ResultField.StressX => "MPa",
            ResultField.StressY => "MPa",
            ResultField.StressXY => "MPa",
            ResultField.DisplacementMagnitude => "mm",
            ResultField.DisplacementX => "mm",
            ResultField.DisplacementY => "mm",
            _ => ""
        };
    }
}

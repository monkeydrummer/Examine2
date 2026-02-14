using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Examine2DModel.Analysis;
using Examine2DModel.BEM;

namespace Examine2DView.Controls;

/// <summary>
/// User control for analysis settings configuration
/// </summary>
public partial class AnalysisSettingsControl : UserControl
{
    private bool _isUpdatingFromCode = false;
    
    public AnalysisSettingsControl()
    {
        InitializeComponent();
        
        // Set default values
        PlaneStrainTypeCombo.SelectedIndex = 0; // PlaneStrain
        ElementTypeCombo.SelectedIndex = 1; // Linear
        TargetElementCountSlider.Value = 100;
        TargetElementCountTextBox.Text = "100";
        UseAdaptiveElementSizingCheck.IsChecked = true;
        MaxRefinementFactorSlider.Value = 4.0;
        EnableCachingCheck.IsChecked = true;
        
        UpdatePreviewText();
    }
    
    /// <summary>
    /// Load settings from BEMConfiguration object
    /// </summary>
    public void LoadSettings(BEMConfiguration config)
    {
        _isUpdatingFromCode = true;
        try
        {
            // Find matching plane strain type
            foreach (ComboBoxItem item in PlaneStrainTypeCombo.Items)
            {
                if (item.Tag?.ToString() == config.PlaneStrainType.ToString())
                {
                    PlaneStrainTypeCombo.SelectedItem = item;
                    break;
                }
            }
            
            // Find matching element type
            foreach (ComboBoxItem item in ElementTypeCombo.Items)
            {
                if (item.Tag?.ToString() == config.ElementType.ToString())
                {
                    ElementTypeCombo.SelectedItem = item;
                    break;
                }
            }
            
            TargetElementCountSlider.Value = config.TargetElementCount;
            TargetElementCountTextBox.Text = config.TargetElementCount.ToString();
            UseAdaptiveElementSizingCheck.IsChecked = config.UseAdaptiveElementSizing;
            MaxRefinementFactorSlider.Value = config.MaxRefinementFactor;
            EnableCachingCheck.IsChecked = config.EnableCaching;
            
            UpdatePreviewText();
        }
        finally
        {
            _isUpdatingFromCode = false;
        }
    }
    
    /// <summary>
    /// Save settings to BEMConfiguration object
    /// </summary>
    public void SaveSettings(BEMConfiguration config)
    {
        // Parse plane strain type
        if (PlaneStrainTypeCombo.SelectedItem is ComboBoxItem planeStrainItem && 
            planeStrainItem.Tag != null)
        {
            if (Enum.TryParse<PlaneStrainType>(planeStrainItem.Tag.ToString(), out var planeStrainType))
            {
                config.PlaneStrainType = planeStrainType;
            }
        }
        
        // Parse element type
        if (ElementTypeCombo.SelectedItem is ComboBoxItem elementTypeItem && 
            elementTypeItem.Tag != null)
        {
            if (Enum.TryParse<ElementType>(elementTypeItem.Tag.ToString(), out var elementType))
            {
                config.ElementType = elementType;
            }
        }
        
        config.TargetElementCount = (int)TargetElementCountSlider.Value;
        config.UseAdaptiveElementSizing = UseAdaptiveElementSizingCheck.IsChecked ?? true;
        config.MaxRefinementFactor = MaxRefinementFactorSlider.Value;
        config.EnableCaching = EnableCachingCheck.IsChecked ?? true;
    }
    
    /// <summary>
    /// Update the preview text with estimated solve time
    /// </summary>
    private void UpdatePreviewText()
    {
        // Skip if controls aren't initialized yet
        if (ElementCountPreview == null || TargetElementCountSlider == null || 
            UseAdaptiveElementSizingCheck == null || MaxRefinementFactorSlider == null)
            return;
            
        int elementCount = (int)TargetElementCountSlider.Value;
        bool useAdaptive = UseAdaptiveElementSizingCheck.IsChecked ?? true;
        double refinementFactor = MaxRefinementFactorSlider.Value;
        
        // Estimate actual element count if adaptive sizing is enabled
        int estimatedElements = elementCount;
        if (useAdaptive)
        {
            // Adaptive sizing typically results in 20-40% more elements due to refinement at corners
            estimatedElements = (int)(elementCount * (1.0 + (refinementFactor - 1.0) * 0.1));
        }
        
        // Estimate solve time based on element count (very rough approximation)
        double estimatedTimeMs;
        if (estimatedElements < 100)
        {
            estimatedTimeMs = 100; // Very fast for small problems
        }
        else if (estimatedElements < 200)
        {
            estimatedTimeMs = 200;
        }
        else if (estimatedElements < 300)
        {
            estimatedTimeMs = 400;
        }
        else if (estimatedElements < 500)
        {
            estimatedTimeMs = 600;
        }
        else
        {
            // Scales roughly with O(N²) for matrix assembly + O(N³) for solve
            estimatedTimeMs = 600 + (estimatedElements - 500) * 2;
        }
        
        string timeStr;
        if (estimatedTimeMs < 1000)
        {
            timeStr = $"~{estimatedTimeMs:F0} ms";
        }
        else
        {
            timeStr = $"~{estimatedTimeMs / 1000.0:F1} seconds";
        }
        
        ElementCountPreview.Text = $"≈ {estimatedElements} elements → {timeStr} solve time";
        
        // Color coding for performance
        if (estimatedTimeMs < 500)
        {
            ElementCountPreview.Foreground = System.Windows.Media.Brushes.Green;
        }
        else if (estimatedTimeMs < 1000)
        {
            ElementCountPreview.Foreground = System.Windows.Media.Brushes.DarkOrange;
        }
        else
        {
            ElementCountPreview.Foreground = System.Windows.Media.Brushes.Red;
        }
    }
    
    private void TargetElementCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Skip if controls aren't initialized yet
        if (TargetElementCountTextBox == null) return;
        if (_isUpdatingFromCode) return;
        
        _isUpdatingFromCode = true;
        try
        {
            TargetElementCountTextBox.Text = ((int)e.NewValue).ToString();
            UpdatePreviewText();
        }
        finally
        {
            _isUpdatingFromCode = false;
        }
    }
    
    private void TargetElementCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingFromCode) return;
        
        if (int.TryParse(TargetElementCountTextBox.Text, out int value))
        {
            // Clamp value to slider range
            value = Math.Max(50, Math.Min(500, value));
            
            _isUpdatingFromCode = true;
            try
            {
                TargetElementCountSlider.Value = value;
                UpdatePreviewText();
            }
            finally
            {
                _isUpdatingFromCode = false;
            }
        }
    }
    
    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow numeric input
        e.Handled = !IsTextNumeric(e.Text);
    }
    
    private static bool IsTextNumeric(string text)
    {
        return Regex.IsMatch(text, "^[0-9]+$");
    }
    
    private void MaxRefinementFactorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingFromCode) return;
        UpdatePreviewText();
    }
    
    private void AdaptiveSizingOptions_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingFromCode) return;
        
        // Enable/disable refinement factor slider based on adaptive sizing checkbox
        if (MaxRefinementFactorSlider != null)
        {
            MaxRefinementFactorSlider.IsEnabled = UseAdaptiveElementSizingCheck.IsChecked ?? true;
        }
        
        UpdatePreviewText();
    }
}

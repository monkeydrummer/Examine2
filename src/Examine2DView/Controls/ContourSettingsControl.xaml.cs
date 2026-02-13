using System.Windows;
using System.Windows.Controls;
using CAD2DModel.Results;

namespace Examine2DView.Controls;

/// <summary>
/// User control for contour visualization settings
/// </summary>
public partial class ContourSettingsControl : UserControl
{
    public ContourSettingsControl()
    {
        InitializeComponent();
        
        // Set default values
        VisibilityCheck.IsChecked = false;
        ResultFieldCombo.SelectedIndex = 0; // Von Mises Stress
        ColorSchemeCombo.SelectedIndex = 0; // Rainbow
        LevelsSlider.Value = 10;
        ShowFilledCheck.IsChecked = true;
        ShowLinesCheck.IsChecked = true;
        OpacitySlider.Value = 1.0;
    }
    
    /// <summary>
    /// Load settings from ContourSettings object
    /// </summary>
    public void LoadSettings(ContourSettings settings)
    {
        VisibilityCheck.IsChecked = settings.IsVisible;
        
        // Find matching result field combo box item
        foreach (ComboBoxItem item in ResultFieldCombo.Items)
        {
            if (item.Tag?.ToString() == settings.Field.ToString())
            {
                ResultFieldCombo.SelectedItem = item;
                break;
            }
        }
        
        // Find matching color scheme combo box item
        foreach (ComboBoxItem item in ColorSchemeCombo.Items)
        {
            if (item.Tag?.ToString() == settings.ColorScheme.ToString())
            {
                ColorSchemeCombo.SelectedItem = item;
                break;
            }
        }
        
        LevelsSlider.Value = settings.NumberOfLevels;
        ShowFilledCheck.IsChecked = settings.ShowFilledContours;
        ShowLinesCheck.IsChecked = settings.ShowContourLines;
        OpacitySlider.Value = settings.FillOpacity;
        
        MinValueText.Text = settings.MinValue?.ToString() ?? "";
        MaxValueText.Text = settings.MaxValue?.ToString() ?? "";
    }
    
    /// <summary>
    /// Save settings to ContourSettings object
    /// </summary>
    public void SaveSettings(ContourSettings settings)
    {
        settings.IsVisible = VisibilityCheck.IsChecked ?? false;
        
        // Parse result field
        if (ResultFieldCombo.SelectedItem is ComboBoxItem resultFieldItem && 
            resultFieldItem.Tag != null)
        {
            if (Enum.TryParse<ResultField>(resultFieldItem.Tag.ToString(), out var field))
            {
                settings.Field = field;
            }
        }
        
        // Parse color scheme
        if (ColorSchemeCombo.SelectedItem is ComboBoxItem colorSchemeItem && 
            colorSchemeItem.Tag != null)
        {
            if (Enum.TryParse<ColorScheme>(colorSchemeItem.Tag.ToString(), out var scheme))
            {
                settings.ColorScheme = scheme;
            }
        }
        
        settings.NumberOfLevels = (int)LevelsSlider.Value;
        settings.ShowFilledContours = ShowFilledCheck.IsChecked ?? true;
        settings.ShowContourLines = ShowLinesCheck.IsChecked ?? true;
        settings.FillOpacity = OpacitySlider.Value;
        
        // Parse value range
        if (double.TryParse(MinValueText.Text, out double minValue))
        {
            settings.MinValue = minValue;
        }
        else
        {
            settings.MinValue = null;
        }
        
        if (double.TryParse(MaxValueText.Text, out double maxValue))
        {
            settings.MaxValue = maxValue;
        }
        else
        {
            settings.MaxValue = null;
        }
    }
}

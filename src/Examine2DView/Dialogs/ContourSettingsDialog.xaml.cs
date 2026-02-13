using System.Windows;
using CAD2DModel.Results;

namespace Examine2DView.Dialogs;

/// <summary>
/// Dialog for configuring contour visualization settings
/// </summary>
public partial class ContourSettingsDialog : Window
{
    private readonly ContourSettings _settings;
    
    /// <summary>
    /// Event raised when Apply button is clicked
    /// </summary>
    public event EventHandler? SettingsApplied;
    
    public ContourSettingsDialog(ContourSettings settings)
    {
        InitializeComponent();
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        
        // Load current settings into the control
        SettingsControl.LoadSettings(_settings);
    }
    
    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // Save settings from control
        SettingsControl.SaveSettings(_settings);
        
        // Raise event to trigger contour regeneration
        SettingsApplied?.Invoke(this, EventArgs.Empty);
    }
    
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // Save settings from control
        SettingsControl.SaveSettings(_settings);
        DialogResult = true;
        Close();
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

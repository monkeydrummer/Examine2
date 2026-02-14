using System.Windows;
using Examine2DModel.BEM;

namespace Examine2DView.Dialogs;

/// <summary>
/// Dialog for configuring BEM analysis settings
/// </summary>
public partial class AnalysisSettingsDialog : Window
{
    private readonly BEMConfiguration _configuration;
    
    /// <summary>
    /// Event raised when Apply button is clicked
    /// </summary>
    public event EventHandler? SettingsApplied;
    
    public AnalysisSettingsDialog(BEMConfiguration configuration)
    {
        InitializeComponent();
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        // Load current settings into the control
        SettingsControl.LoadSettings(_configuration);
    }
    
    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // Save settings from control
        SettingsControl.SaveSettings(_configuration);
        
        // Raise event to trigger any necessary updates
        SettingsApplied?.Invoke(this, EventArgs.Empty);
    }
    
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // Save settings from control
        SettingsControl.SaveSettings(_configuration);
        DialogResult = true;
        Close();
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

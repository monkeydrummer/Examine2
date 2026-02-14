using System.Windows;

namespace Examine2DView.Dialogs;

/// <summary>
/// Dialog for configuring automatic external boundary creation
/// </summary>
public partial class AutoExternalBoundaryDialog : Window
{
    public AutoExternalBoundaryDialog()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Gets the expansion factor (how much bigger than existing geometry)
    /// </summary>
    public double ExpansionFactor => ExpansionFactorSlider.Value;
    
    /// <summary>
    /// Gets the minimum margin around geometry (in model units)
    /// </summary>
    public double MinimumMargin => MinMarginSlider.Value;
    
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

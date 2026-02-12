using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CAD2DViewModels.ViewModels;

/// <summary>
/// Main application view model
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private CanvasViewModel? _activeCanvas;
    
    [ObservableProperty]
    private string _title = "Examine2D - Boundary Element Analysis";
    
    [ObservableProperty]
    private bool _isProjectOpen;
    
    [ObservableProperty]
    private string? _currentProjectPath;
    
    public ObservableCollection<CanvasViewModel> OpenCanvases { get; } = new();
    
    public MainViewModel()
    {
    }
    
    [RelayCommand]
    private void NewProject()
    {
        IsProjectOpen = true;
        CurrentProjectPath = null;
        Title = "Examine2D - Untitled";
    }
    
    [RelayCommand]
    private void OpenProject()
    {
        // TODO: Show open file dialog
        // For now, just simulate opening a project
        IsProjectOpen = true;
        CurrentProjectPath = @"C:\Projects\Example.e2d";
        Title = $"Examine2D - {System.IO.Path.GetFileName(CurrentProjectPath)}";
    }
    
    [RelayCommand]
    private void SaveProject()
    {
        if (string.IsNullOrEmpty(CurrentProjectPath))
        {
            SaveProjectAs();
        }
        else
        {
            // TODO: Save project to CurrentProjectPath
        }
    }
    
    [RelayCommand]
    private void SaveProjectAs()
    {
        // TODO: Show save file dialog
    }
    
    [RelayCommand]
    private void CloseProject()
    {
        // TODO: Prompt to save if modified
        IsProjectOpen = false;
        CurrentProjectPath = null;
        Title = "Examine2D - Boundary Element Analysis";
        OpenCanvases.Clear();
        ActiveCanvas = null;
    }
    
    [RelayCommand]
    private void Exit()
    {
        // TODO: Prompt to save if modified
        // Application will handle the actual exit
    }
    
    [RelayCommand]
    private void ShowAbout()
    {
        // TODO: Show about dialog
    }
}

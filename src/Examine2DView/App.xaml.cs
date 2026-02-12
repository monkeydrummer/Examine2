using System.Windows;
using CAD2DModel.DI;
using CAD2DViewModels.ViewModels;
using Examine2DModel.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Examine2DView;

/// <summary>
/// Application entry point
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Configure dependency injection
        var services = new ServiceCollection();
        
        // Add CAD2D services
        services.AddCAD2DServices();
        
        // Add Examine2D services
        services.AddExamine2DServices();
        
        // Add ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<CanvasViewModel>();
        services.AddTransient<ModelingViewModel>();
        services.AddTransient<AnalysisViewModel>();
        
        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        
        // Create and show main window with service provider
        var mainWindow = new MainWindow(
            _serviceProvider.GetRequiredService<MainViewModel>(),
            _serviceProvider);
        mainWindow.Show();
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

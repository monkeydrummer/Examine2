using CAD2DModel.Camera;
using CAD2DModel.Commands;
using CAD2DModel.Interaction;
using CAD2DModel.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CAD2DViewModels.ViewModels;

/// <summary>
/// ViewModel for the CAD canvas
/// </summary>
public partial class CanvasViewModel : ObservableObject
{
    private readonly IGeometryModel _model;
    private readonly ICommandManager _commandManager;
    private readonly IModeManager _modeManager;
    private readonly ISnapService _snapService;
    
    [ObservableProperty]
    private Camera2D _camera;
    
    [ObservableProperty]
    private IInteractionMode? _currentMode;
    
    [ObservableProperty]
    private string _statusText = "Ready";
    
    [ObservableProperty]
    private bool _isGridVisible = true;
    
    [ObservableProperty]
    private bool _isSnapEnabled = true;
    
    public CanvasViewModel(
        IGeometryModel model,
        ICommandManager commandManager,
        IModeManager modeManager,
        ISnapService snapService)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        _modeManager = modeManager ?? throw new ArgumentNullException(nameof(modeManager));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
        
        _camera = new Camera2D
        {
            Center = new CAD2DModel.Geometry.Point2D(0, 0),
            Scale = 1.0,
            ViewportSize = new CAD2DModel.Camera.Size(800, 600)
        };
        
        // Subscribe to mode manager events
        _modeManager.ModeChanged += OnModeChanged;
        
        // Subscribe to command manager events
        _commandManager.StateChanged += OnCommandStateChanged;
    }
    
    public ObservableCollection<CAD2DModel.Geometry.IEntity> Entities =>
        new ObservableCollection<CAD2DModel.Geometry.IEntity>(_model.Entities);
    
    [RelayCommand]
    private void ZoomIn()
    {
        Camera.Scale *= 0.8;
    }
    
    [RelayCommand]
    private void ZoomOut()
    {
        Camera.Scale *= 1.25;
    }
    
    [RelayCommand]
    private void ZoomFit()
    {
        // Calculate bounds of all entities
        var entities = _model.Entities.ToList();
        if (entities.Count == 0)
            return;
        
        var polylines = entities.OfType<CAD2DModel.Geometry.Polyline>().ToList();
        if (polylines.Count == 0)
            return;
        
        var bounds = polylines[0].GetBounds();
        foreach (var polyline in polylines.Skip(1))
        {
            bounds = bounds.Union(polyline.GetBounds());
        }
        
        // Add 10% margin
        bounds.Inflate(bounds.Width * 0.1, bounds.Height * 0.1);
        
        // Calculate scale and center
        double scaleX = Camera.ViewportSize.Width / bounds.Width;
        double scaleY = Camera.ViewportSize.Height / bounds.Height;
        Camera.Scale = Math.Min(scaleX, scaleY);
        Camera.Center = bounds.Center;
    }
    
    [RelayCommand]
    private void Undo()
    {
        if (_commandManager.CanUndo)
        {
            _commandManager.Undo();
            UpdateStatusText();
        }
    }
    
    [RelayCommand]
    private void Redo()
    {
        if (_commandManager.CanRedo)
        {
            _commandManager.Redo();
            UpdateStatusText();
        }
    }
    
    [RelayCommand]
    private void ToggleGrid()
    {
        IsGridVisible = !IsGridVisible;
    }
    
    [RelayCommand]
    private void ToggleSnap()
    {
        IsSnapEnabled = !IsSnapEnabled;
    }
    
    private void OnModeChanged(object? sender, ModeChangedEventArgs e)
    {
        CurrentMode = e.NewMode;
        StatusText = e.NewMode.StatusPrompt;
    }
    
    private void OnCommandStateChanged(object? sender, EventArgs e)
    {
        UpdateStatusText();
    }
    
    private void UpdateStatusText()
    {
        if (_commandManager.CanUndo)
        {
            StatusText = $"Ready - Can undo: {_commandManager.UndoDescription}";
        }
        else
        {
            StatusText = "Ready";
        }
    }
}

using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CAD2DViewModels.ViewModels;

/// <summary>
/// ViewModel for modeling (geometry creation and editing)
/// </summary>
public partial class ModelingViewModel : ObservableObject
{
    private readonly IGeometryModel _model;
    private readonly ICommandManager _commandManager;
    private readonly ISelectionService _selectionService;
    
    [ObservableProperty]
    private Polyline? _selectedPolyline;
    
    [ObservableProperty]
    private Vertex? _selectedVertex;
    
    [ObservableProperty]
    private string _selectedToolName = "Select";
    
    public ModelingViewModel(
        IGeometryModel model,
        ICommandManager commandManager,
        ISelectionService selectionService)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        
        _selectionService.SelectionChanged += OnSelectionChanged;
    }
    
    public ObservableCollection<Polyline> Polylines =>
        new ObservableCollection<Polyline>(_model.Entities.OfType<Polyline>());
    
    public ObservableCollection<Boundary> Boundaries =>
        new ObservableCollection<Boundary>(_model.Entities.OfType<Boundary>());
    
    [RelayCommand]
    private void CreatePolyline()
    {
        SelectedToolName = "Create Polyline";
        // TODO: Enter polyline creation mode
    }
    
    [RelayCommand]
    private void CreateBoundary()
    {
        SelectedToolName = "Create Boundary";
        // TODO: Enter boundary creation mode
    }
    
    [RelayCommand]
    private void SelectTool()
    {
        SelectedToolName = "Select";
        // TODO: Enter select mode
    }
    
    [RelayCommand]
    private void DeleteSelected()
    {
        var selectedEntities = _selectionService.SelectedEntities.ToList();
        if (selectedEntities.Count == 0)
            return;
        
        foreach (var entity in selectedEntities)
        {
            if (entity is Polyline polyline)
            {
                var command = new CAD2DModel.Commands.Implementations.RemovePolylineCommand(_model, polyline);
                _commandManager.Execute(command);
            }
        }
        
        _selectionService.ClearSelection();
    }
    
    [RelayCommand]
    private void AddVertex()
    {
        if (SelectedPolyline == null)
            return;
        
        // Add vertex at the end of the polyline
        var newLocation = new Point2D(0, 0); // TODO: Get from mouse position
        var command = new CAD2DModel.Commands.Implementations.AddVertexCommand(
            SelectedPolyline,
            newLocation);
        
        _commandManager.Execute(command);
    }
    
    [RelayCommand]
    private void RemoveVertex()
    {
        if (SelectedPolyline == null || SelectedVertex == null)
            return;
        
        var command = new CAD2DModel.Commands.Implementations.RemoveVertexCommand(
            SelectedPolyline,
            SelectedVertex);
        
        _commandManager.Execute(command);
    }
    
    [RelayCommand]
    private void ClosePath()
    {
        if (SelectedPolyline == null)
            return;
        
        var command = new CAD2DModel.Commands.Implementations.PropertyChangeCommand<bool>(
            "Close path",
            isClosed => SelectedPolyline.IsClosed = isClosed,
            SelectedPolyline.IsClosed,
            true);
        
        _commandManager.Execute(command);
    }
    
    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        var selected = _selectionService.SelectedEntities.FirstOrDefault();
        SelectedPolyline = selected as Polyline;
        SelectedVertex = null;
    }
}

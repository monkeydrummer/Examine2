using System.Windows;
using System.Windows.Input;
using CAD2DModel.Camera;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Interaction;
using CAD2DModel.Interaction.Implementations.Modes;
using CAD2DModel.Services;
using CAD2DViewModels.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Examine2DView;

/// <summary>
/// Main application window
/// </summary>
public partial class MainWindow : Window
{
    private readonly IServiceProvider? _serviceProvider;
    private IModeManager? _modeManager;
    
    // Mode switching commands
    public System.Windows.Input.ICommand SelectModeCommand { get; }
    public System.Windows.Input.ICommand AddBoundaryModeCommand { get; }
    public System.Windows.Input.ICommand AddPolylineModeCommand { get; }
    public System.Windows.Input.ICommand MoveVertexModeCommand { get; }
    
    // Zoom commands
    public System.Windows.Input.ICommand ZoomInCommand { get; }
    public System.Windows.Input.ICommand ZoomOutCommand { get; }
    public System.Windows.Input.ICommand ZoomFitCommand { get; }
    
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        
        // Initialize mode commands
        SelectModeCommand = new RelayCommand(EnterSelectMode);
        AddBoundaryModeCommand = new RelayCommand(EnterAddBoundaryMode);
        AddPolylineModeCommand = new RelayCommand(EnterAddPolylineMode);
        MoveVertexModeCommand = new RelayCommand(EnterMoveVertexMode);
        
        // Initialize zoom commands
        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        ZoomFitCommand = new RelayCommand(ZoomFit);
        
        // DataContext will be set by DI container
    }
    
    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
    
    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider) : this(viewModel)
    {
        _serviceProvider = serviceProvider;
    }
    
    private void EnterSelectMode()
    {
        if (_modeManager == null || _serviceProvider == null)
            return;
        
        var commandManager = _serviceProvider.GetService<ICommandManager>();
        var selectionService = _serviceProvider.GetService<ISelectionService>();
        var geometryModel = _serviceProvider.GetService<IGeometryModel>();
        
        if (commandManager != null && selectionService != null && geometryModel != null)
        {
            var mode = new SelectMode(_modeManager, commandManager, selectionService, geometryModel);
            _modeManager.EnterMode(mode);
        }
    }
    
    private void EnterAddBoundaryMode()
    {
        if (_modeManager == null || _serviceProvider == null)
            return;
        
        var commandManager = _serviceProvider.GetService<ICommandManager>();
        var geometryModel = _serviceProvider.GetService<IGeometryModel>();
        var snapService = _serviceProvider.GetService<ISnapService>();
        
        if (commandManager != null && geometryModel != null && snapService != null)
        {
            var mode = new AddBoundaryMode(_modeManager, commandManager, geometryModel, snapService);
            _modeManager.EnterMode(mode);
        }
    }
    
    private void EnterAddPolylineMode()
    {
        // TODO: Implement AddPolylineMode when ready
        System.Windows.MessageBox.Show("Add Polyline mode coming soon!", "Not Implemented", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void EnterMoveVertexMode()
    {
        if (_modeManager == null || _serviceProvider == null)
            return;
        
        var commandManager = _serviceProvider.GetService<ICommandManager>();
        var selectionService = _serviceProvider.GetService<ISelectionService>();
        var snapService = _serviceProvider.GetService<ISnapService>();
        var geometryModel = _serviceProvider.GetService<IGeometryModel>();
        
        if (commandManager != null && selectionService != null && snapService != null && geometryModel != null)
        {
            var mode = new MoveVertexMode(_modeManager, commandManager, selectionService, snapService, geometryModel);
            _modeManager.EnterMode(mode);
        }
    }
    
    private void ZoomIn()
    {
        if (CanvasControl?.Camera != null)
        {
            CanvasControl.Camera.Scale *= 0.8;
            CanvasControl.InvalidateVisual();
        }
    }
    
    private void ZoomOut()
    {
        if (CanvasControl?.Camera != null)
        {
            CanvasControl.Camera.Scale *= 1.25;
            CanvasControl.InvalidateVisual();
        }
    }
    
    private void ZoomFit()
    {
        if (CanvasControl?.Camera == null)
            return;
        
        // Calculate bounds from all entities
        var allPolylines = CanvasControl.Polylines.ToList();
        var allBoundaries = CanvasControl.Boundaries.ToList();
        
        if (allPolylines.Count == 0 && allBoundaries.Count == 0)
            return;
        
        // Get bounds from all entities
        var entities = allPolylines.Cast<Polyline>().Concat(allBoundaries.Cast<Polyline>()).ToList();
        if (entities.Count == 0)
            return;
        
        var bounds = entities[0].GetBounds();
        foreach (var entity in entities.Skip(1))
        {
            bounds = bounds.Union(entity.GetBounds());
        }
        
        // Add 10% margin
        bounds.Inflate(bounds.Width * 0.1, bounds.Height * 0.1);
        
        // Calculate scale (world units per screen pixel)
        // Higher scale means more zoomed out
        double scaleX = bounds.Width / CanvasControl.Camera.ViewportSize.Width;
        double scaleY = bounds.Height / CanvasControl.Camera.ViewportSize.Height;
        CanvasControl.Camera.Scale = Math.Max(scaleX, scaleY); // Use Max to ensure everything fits
        CanvasControl.Camera.Center = bounds.Center;
        
        CanvasControl.InvalidateVisual();
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set up canvas with mode manager
        SetupCanvas();
        
        // Add sample geometry to demonstrate rendering
        CreateSampleGeometry();
    }
    
    private void SetupCanvas()
    {
        if (_serviceProvider == null)
            return;
        
        // Get the mode manager from DI
        _modeManager = _serviceProvider.GetService<IModeManager>();
        if (_modeManager != null)
        {
            // Set the camera on the mode manager so modes can access it
            _modeManager.Camera = CanvasControl.Camera;
            CanvasControl.ModeManager = _modeManager;
            
            // Set selection service for rendering
            var selectionService = _serviceProvider.GetService<ISelectionService>();
            if (selectionService != null)
            {
                CanvasControl.SelectionService = selectionService;
            }
            
            // Connect status bar to canvas status updates
            CanvasControl.StatusTextChanged += (s, statusText) =>
            {
                if (StatusText != null)
                {
                    StatusText.Text = statusText ?? "Ready";
                }
            };
            
            // Connect coordinates display to mouse position
            CanvasControl.MousePositionChanged += (s, worldPos) =>
            {
                if (CoordinatesText != null)
                {
                    CoordinatesText.Text = $"X: {worldPos.X:F3} Y: {worldPos.Y:F3}";
                }
            };
            
            // Update scale display on camera changes
            if (CanvasControl.Camera != null)
            {
                CanvasControl.Camera.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Camera2D.Scale) && ScaleText != null)
                    {
                        ScaleText.Text = $"Scale: {CanvasControl.Camera.Scale:F2}";
                    }
                };
            }
        }
        
        // Get the geometry model and sync collections
        var geometryModel = _serviceProvider.GetService<IGeometryModel>();
        if (geometryModel != null)
        {
            // Subscribe to model changes to update canvas collections
            geometryModel.Entities.CollectionChanged += (s, e) =>
            {
                // Sync model entities to canvas collections
                SyncEntitiesToCanvas();
            };
            
            // Initial sync
            SyncEntitiesToCanvas();
        }
        
        // Setup snap controls
        SetupSnapControls();
    }
    
    private void SetupSnapControls()
    {
        if (_serviceProvider == null)
            return;
        
        var snapService = _serviceProvider.GetService<ISnapService>();
        if (snapService == null)
            return;
        
        // Wire up snap toggle checkboxes
        SnapVertexCheck.Checked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Vertex, true);
        SnapVertexCheck.Unchecked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Vertex, false);
        
        SnapMidpointCheck.Checked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Midpoint, true);
        SnapMidpointCheck.Unchecked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Midpoint, false);
        
        SnapGridCheck.Checked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Grid, true);
        SnapGridCheck.Unchecked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Grid, false);
        
        SnapOrthoCheck.Checked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Ortho, true);
        SnapOrthoCheck.Unchecked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Ortho, false);
        
        SnapNearestCheck.Checked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Nearest, true);
        SnapNearestCheck.Unchecked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Nearest, false);
        
        SnapIntersectionCheck.Checked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Intersection, true);
        SnapIntersectionCheck.Unchecked += (s, e) => UpdateSnapMode(snapService, CAD2DModel.Services.SnapMode.Intersection, false);
    }
    
    private void UpdateSnapMode(ISnapService snapService, CAD2DModel.Services.SnapMode mode, bool enabled)
    {
        if (enabled)
        {
            snapService.ActiveSnapModes |= mode;
        }
        else
        {
            snapService.ActiveSnapModes &= ~mode;
        }
    }
    
    private void SyncEntitiesToCanvas()
    {
        if (_serviceProvider == null)
            return;
        
        var geometryModel = _serviceProvider.GetService<IGeometryModel>();
        if (geometryModel == null)
            return;
        
        // Clear canvas collections
        CanvasControl.Polylines.Clear();
        CanvasControl.Boundaries.Clear();
        
        // Add entities from model
        foreach (var entity in geometryModel.Entities)
        {
            if (entity is Boundary boundary)
            {
                CanvasControl.Boundaries.Add(boundary);
            }
            else if (entity is Polyline polyline)
            {
                CanvasControl.Polylines.Add(polyline);
            }
        }
    }
    
    private void CreateSampleGeometry()
    {
        // Create a sample boundary (excavation)
        var excavation = new Boundary
        {
            Name = "Sample Excavation",
            IsClosed = true
        };
        
        // Circular excavation (discretized as polygon)
        double radius = 5.0;
        int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            excavation.AddVertex(new Point2D(
                radius * Math.Cos(angle),
                radius * Math.Sin(angle)
            ));
        }
        
        CanvasControl.Boundaries.Add(excavation);
        
        // Create a sample polyline
        var queryLine = new Polyline
        {
            Name = "Sample Query Line",
            IsClosed = false
        };
        
        queryLine.AddVertex(new Point2D(-10, 0));
        queryLine.AddVertex(new Point2D(-5, 2));
        queryLine.AddVertex(new Point2D(0, 0));
        queryLine.AddVertex(new Point2D(5, -2));
        queryLine.AddVertex(new Point2D(10, 0));
        
        CanvasControl.Polylines.Add(queryLine);
        
        // Create external boundary box
        var externalBox = new Boundary
        {
            Name = "External Box",
            IsClosed = true
        };
        
        externalBox.AddVertex(new Point2D(-15, -15));
        externalBox.AddVertex(new Point2D(15, -15));
        externalBox.AddVertex(new Point2D(15, 15));
        externalBox.AddVertex(new Point2D(-15, 15));
        
        CanvasControl.Boundaries.Add(externalBox);
        
        // Zoom to fit
        CanvasControl.Camera.Center = Point2D.Zero;
        CanvasControl.Camera.Scale = 0.05; // Zoom out to see the whole scene
        CanvasControl.InvalidateVisual();
    }
}

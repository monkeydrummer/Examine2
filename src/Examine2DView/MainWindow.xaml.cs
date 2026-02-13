using System.Windows;
using System.Windows.Input;
using CAD2DModel.Camera;
using CAD2DModel.Commands;
using CAD2DModel.Geometry;
using CAD2DModel.Interaction;
using CAD2DModel.Interaction.Implementations.Modes;
using CAD2DModel.Services;
using CAD2DModel.Results;
using CAD2DViewModels.ViewModels;
using Examine2DView.Dialogs;
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
    public System.Windows.Input.ICommand AddExternalBoundaryModeCommand { get; }
    public System.Windows.Input.ICommand AddBoundaryModeCommand { get; }
    public System.Windows.Input.ICommand AddPolylineModeCommand { get; }
    public System.Windows.Input.ICommand MoveVertexModeCommand { get; }
    public System.Windows.Input.ICommand MoveBoundaryModeCommand { get; }
    
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
        AddExternalBoundaryModeCommand = new RelayCommand(EnterAddExternalBoundaryMode);
        AddBoundaryModeCommand = new RelayCommand(EnterAddBoundaryMode);
        AddPolylineModeCommand = new RelayCommand(EnterAddPolylineMode);
        MoveVertexModeCommand = new RelayCommand(EnterMoveVertexMode);
        MoveBoundaryModeCommand = new RelayCommand(EnterMoveBoundaryMode);
        
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
    
    private void EnterAddExternalBoundaryMode()
    {
        if (_modeManager == null || _serviceProvider == null)
            return;
        
        var commandManager = _serviceProvider.GetService<ICommandManager>();
        var geometryModel = _serviceProvider.GetService<IGeometryModel>();
        var snapService = _serviceProvider.GetService<ISnapService>();
        
        if (commandManager != null && geometryModel != null && snapService != null)
        {
            // Create mode that adds ExternalBoundary instead of regular Boundary
            var mode = new AddExternalBoundaryMode(_modeManager, commandManager, geometryModel, snapService);
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
        if (_modeManager == null || _serviceProvider == null)
            return;
        
        var commandManager = _serviceProvider.GetService<ICommandManager>();
        var geometryModel = _serviceProvider.GetService<IGeometryModel>();
        var snapService = _serviceProvider.GetService<ISnapService>();
        
        if (commandManager != null && geometryModel != null && snapService != null)
        {
            var mode = new AddPolylineMode(_modeManager, commandManager, geometryModel, snapService);
            _modeManager.EnterMode(mode);
        }
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
    
    private void EnterMoveBoundaryMode()
    {
        if (_modeManager == null || _serviceProvider == null)
            return;
        
        var commandManager = _serviceProvider.GetService(typeof(ICommandManager)) as ICommandManager;
        var selectionService = _serviceProvider.GetService(typeof(ISelectionService)) as ISelectionService;
        var snapService = _serviceProvider.GetService(typeof(ISnapService)) as ISnapService;
        var geometryModel = _serviceProvider.GetService(typeof(IGeometryModel)) as IGeometryModel;
        
        if (commandManager != null && selectionService != null && snapService != null && geometryModel != null)
        {
            var mode = new MoveBoundaryMode(_modeManager, commandManager, selectionService, snapService, geometryModel);
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
                // Subscribe to selection changes for view updates and status bar
                selectionService.SelectionChanged += OnSelectionCountChanged;
                selectionService.VertexSelectionChanged += OnSelectionCountChanged;
                selectionService.SegmentSelectionChanged += OnSelectionCountChanged;
                selectionService.VertexSelectionChanged += (s, e) => CanvasControl.InvalidateVisual();
                selectionService.SegmentSelectionChanged += (s, e) => CanvasControl.InvalidateVisual();
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
                
                // Regenerate contours if they're visible
                var contourService = _serviceProvider.GetService<IContourService>();
                if (contourService != null && contourService.Settings.IsVisible)
                {
                    contourService.InvalidateContours();
                    RegenerateContours();
                }
            };
            
            // Subscribe to geometry changes for live preview updates
            geometryModel.GeometryChanged += (s, e) =>
            {
                var contourService = _serviceProvider.GetService<IContourService>();
                if (contourService != null && contourService.Settings.IsVisible)
                {
                    contourService.InvalidateContours();
                    RegenerateContours();
                }
            };
            
            // Initial sync
            SyncEntitiesToCanvas();
        }
        
        // Subscribe to command execution to regenerate contours on any geometry change
        var commandManager = _serviceProvider.GetService<ICommandManager>();
        if (commandManager != null)
        {
            commandManager.CommandExecuted += (s, e) =>
            {
                // Regenerate contours after any command that might affect geometry
                var contourService = _serviceProvider.GetService<IContourService>();
                if (contourService != null && contourService.Settings.IsVisible)
                {
                    contourService.InvalidateContours();
                    RegenerateContours();
                }
            };
        }
        
        // Setup contour service
        var contourService = _serviceProvider.GetService<IContourService>();
        if (contourService != null)
        {
            CanvasControl.ContourService = contourService;
            CanvasControl.GeometryModel = geometryModel; // Needed for excavation masking
            
            // Subscribe to contour updates to refresh legend
            contourService.ContoursUpdated += (s, e) =>
            {
                if (contourService.CurrentContourData != null)
                {
                    ContourLegend.UpdateLegend(contourService.CurrentContourData, contourService.Settings);
                }
            };
            
            // Load initial settings into properties panel
            PropertiesContourSettings.LoadSettings(contourService.Settings);
            
            // Subscribe to changes in the properties panel
            PropertiesContourSettings.VisibilityCheck.Checked += PropertiesContourSettings_Changed;
            PropertiesContourSettings.VisibilityCheck.Unchecked += PropertiesContourSettings_Changed;
            PropertiesContourSettings.ResultFieldCombo.SelectionChanged += PropertiesContourSettings_Changed;
            PropertiesContourSettings.ColorSchemeCombo.SelectionChanged += PropertiesContourSettings_Changed;
            PropertiesContourSettings.LevelsSlider.ValueChanged += PropertiesContourSettings_Changed;
            PropertiesContourSettings.ShowFilledCheck.Checked += PropertiesContourSettings_Changed;
            PropertiesContourSettings.ShowFilledCheck.Unchecked += PropertiesContourSettings_Changed;
            PropertiesContourSettings.ShowLinesCheck.Checked += PropertiesContourSettings_Changed;
            PropertiesContourSettings.ShowLinesCheck.Unchecked += PropertiesContourSettings_Changed;
            PropertiesContourSettings.OpacitySlider.ValueChanged += PropertiesContourSettings_Changed;
        }
        
        // Setup snap controls
        SetupSnapControls();
    }
    
    private void PropertiesContourSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null)
            return;
        
        var contourService = _serviceProvider.GetService<IContourService>();
        if (contourService == null)
            return;
        
        // Save settings from the properties panel
        PropertiesContourSettings.SaveSettings(contourService.Settings);
        
        // Regenerate contours if they're visible
        if (contourService.Settings.IsVisible)
        {
            contourService.InvalidateContours();
            RegenerateContours();
            ContourLegend.Visibility = Visibility.Visible;
        }
        else
        {
            ContourLegend.Visibility = Visibility.Collapsed;
        }
        
        CanvasControl.InvalidateVisual();
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
        
        // Update selection count display
        UpdateSelectionCountDisplay();
        
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
    
    private void OnSelectionCountChanged(object? sender, EventArgs e)
    {
        UpdateSelectionCountDisplay();
    }
    
    private void UpdateSelectionCountDisplay()
    {
        if (_serviceProvider == null)
            return;
        
        var selectionService = _serviceProvider.GetService<ISelectionService>();
        if (selectionService == null)
            return;
        
        int entityCount = selectionService.SelectedEntities.Count;
        int vertexCount = selectionService.SelectedVertices.Count;
        int segmentCount = selectionService.SelectedSegments.Count;
        
        if (entityCount == 0 && vertexCount == 0 && segmentCount == 0)
        {
            SelectionCountText.Text = "";
            SelectionCountSeparator.Visibility = System.Windows.Visibility.Collapsed;
        }
        else
        {
            var parts = new List<string>();
            
            if (entityCount > 0)
            {
                var boundaryCount = selectionService.SelectedEntities.OfType<Boundary>().Count();
                var polylineCount = selectionService.SelectedEntities.OfType<Polyline>().Count();
                
                if (boundaryCount > 0)
                    parts.Add($"{boundaryCount} {(boundaryCount == 1 ? "boundary" : "boundaries")}");
                if (polylineCount > 0)
                    parts.Add($"{polylineCount} {(polylineCount == 1 ? "polyline" : "polylines")}");
            }
            
            if (vertexCount > 0)
            {
                parts.Add($"{vertexCount} {(vertexCount == 1 ? "vertex" : "vertices")}");
            }
            
            if (segmentCount > 0)
            {
                parts.Add($"{segmentCount} {(segmentCount == 1 ? "segment" : "segments")}");
            }
            
            SelectionCountText.Text = $"Selected: {string.Join(", ", parts)}";
            SelectionCountSeparator.Visibility = System.Windows.Visibility.Visible;
        }
    }
    
    private void ContourSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceProvider == null)
            return;
        
        var contourService = _serviceProvider.GetService<IContourService>();
        if (contourService == null)
        {
            MessageBox.Show("Contour service is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        var dialog = new ContourSettingsDialog(contourService.Settings);
        
        // Subscribe to Apply button event
        dialog.SettingsApplied += (s, args) =>
        {
            // Regenerate contours when Apply is clicked
            if (contourService.Settings.IsVisible)
            {
                RegenerateContours();
            }
            else
            {
                ContourLegend.Visibility = Visibility.Collapsed;
            }
            CanvasControl.InvalidateVisual();
        };
        
        if (dialog.ShowDialog() == true)
        {
            // Settings were saved, regenerate contours if visible
            if (contourService.Settings.IsVisible)
            {
                RegenerateContours();
            }
            else
            {
                // Hide legend when contours are turned off
                ContourLegend.Visibility = Visibility.Collapsed;
            }
            
            CanvasControl.InvalidateVisual();
        }
    }
    
    private void RegenerateContours()
    {
        if (_serviceProvider == null)
            return;
        
        var contourService = _serviceProvider.GetService<IContourService>();
        var geometryModel = _serviceProvider.GetService<IGeometryModel>();
        
        if (contourService == null || geometryModel == null)
            return;
        
        // Find external boundary
        var externalBoundary = geometryModel.Entities
            .OfType<ExternalBoundary>()
            .FirstOrDefault();
        
        if (externalBoundary == null)
        {
            MessageBox.Show("Please create an External Boundary first.\n\n" +
                          "Use Model → Create External Boundary to define the analysis region.",
                          "External Boundary Required",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
            return;
        }
        
        // Get excavation boundaries (regular boundaries, not external)
        var excavations = geometryModel.Entities
            .OfType<Boundary>()
            .Where(b => b is not ExternalBoundary)
            .ToList();
        
        // Generate contours
        var contourData = contourService.GenerateContours(
            externalBoundary,
            excavations,
            contourService.Settings.Field);
        
        // Update legend
        ContourLegend.UpdateLegend(contourData, contourService.Settings);
    }
}

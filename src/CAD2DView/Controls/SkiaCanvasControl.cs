using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CAD2DModel.Camera;
using CAD2DModel.Geometry;
using CAD2DModel.Interaction;
using CAD2DModel.Services;
using CAD2DModel.Results;
using CAD2DModel.Rendering;
using CAD2DView.Rendering;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace CAD2DView.Controls;

/// <summary>
/// WPF control for rendering 2D CAD content using SkiaSharp with full interaction mode support
/// </summary>
public class SkiaCanvasControl : UserControl
{
    private readonly SKElement _skElement;
    private Camera2D? _camera;
    private Point2D _lastMousePosition;
    private bool _isPanning;
    private bool _isGridVisible = true;
    private IModeManager? _modeManager;
    private ISelectionService? _selectionService;
    private IContourService? _contourService;
    private IGeometryModel? _geometryModel;
    private ColorMapper _colorMapper = new ColorMapper();
    
    // Ruler rendering
    private readonly RulerRenderer _rulerRenderer = new();
    private readonly RulerConfiguration _rulerConfig = new();
    private SKPoint? _lastMouseScreenPos = null;
    
    // Annotation rendering
    private readonly AnnotationRenderer _annotationRenderer = new();
    
    // Cached contour rendering data (regenerated only when contours change)
    private SKColor[]? _cachedContourColors;
    private ContourData? _lastRenderedContourData;
    private double _lastMinValue;
    private double _lastMaxValue;
    private double _lastFillOpacity;
    private ColorScheme _lastColorScheme;
    
    // Entities to render
    public ObservableCollection<Polyline> Polylines { get; } = new();
    public ObservableCollection<Boundary> Boundaries { get; } = new();
    
    // Events for status updates
    public event EventHandler<string>? StatusTextChanged;
    public event EventHandler<Point2D>? MousePositionChanged;
    
    public SkiaCanvasControl()
    {
        _skElement = new SKElement();
        _skElement.PaintSurface += OnPaintSurface;
        Content = _skElement;
        
        // Initialize camera with default values
        _camera = new Camera2D
        {
            Center = Point2D.Zero,
            Scale = 1.0,
            ViewportSize = new CAD2DModel.Camera.Size(800, 600)
        };
        
        // Wire up mouse events
        MouseWheel += OnMouseWheel;
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        
        // Wire up keyboard events
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        
        // Subscribe to collection changes
        Polylines.CollectionChanged += (s, e) => InvalidateVisual();
        Boundaries.CollectionChanged += (s, e) => InvalidateVisual();
        
        Focusable = true;
        ClipToBounds = true;
    }
    
    /// <summary>
    /// Set the mode manager for interaction
    /// </summary>
    public IModeManager? ModeManager
    {
        get => _modeManager;
        set
        {
            if (_modeManager != null)
            {
                _modeManager.CurrentMode.StateChanged -= OnModeStateChanged;
                _modeManager.ModeChanged -= OnModeChanged;
            }
            
            _modeManager = value;
            
            if (_modeManager != null)
            {
                _modeManager.CurrentMode.StateChanged += OnModeStateChanged;
                _modeManager.ModeChanged += OnModeChanged;
                UpdateCursor();
                UpdateStatusText();
            }
        }
    }
    
    public ISelectionService? SelectionService
    {
        get => _selectionService;
        set
        {
            if (_selectionService != null)
            {
                _selectionService.SelectionChanged -= OnSelectionChanged;
            }
            
            _selectionService = value;
            
            if (_selectionService != null)
            {
                _selectionService.SelectionChanged += OnSelectionChanged;
            }
        }
    }
    
    public IContourService? ContourService
    {
        get => _contourService;
        set
        {
            if (_contourService != null)
            {
                _contourService.ContoursUpdated -= OnContoursUpdated;
            }
            
            _contourService = value;
            
            if (_contourService != null)
            {
                _contourService.ContoursUpdated += OnContoursUpdated;
            }
        }
    }
    
    public IGeometryModel? GeometryModel
    {
        get => _geometryModel;
        set
        {
            if (_geometryModel != null && _geometryModel.Annotations != null)
            {
                _geometryModel.Annotations.CollectionChanged -= OnAnnotationsChanged;
            }
            
            _geometryModel = value;
            
            if (_geometryModel != null && _geometryModel.Annotations != null)
            {
                _geometryModel.Annotations.CollectionChanged += OnAnnotationsChanged;
            }
        }
    }
    
    private void OnAnnotationsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual(); // Redraw when annotations change
    }
    
    private void OnContoursUpdated(object? sender, EventArgs e)
    {
        InvalidateVisual(); // Redraw to show new contours
    }
    
    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        InvalidateVisual(); // Redraw to show selection highlights
    }
    
    public Camera2D Camera
    {
        get => _camera ?? throw new InvalidOperationException("Camera not initialized");
        set
        {
            _camera = value;
            InvalidateVisual();
        }
    }
    
    public bool IsGridVisible
    {
        get => _isGridVisible;
        set
        {
            _isGridVisible = value;
            InvalidateVisual();
        }
    }
    
    public bool IsRulerVisible
    {
        get => _rulerConfig.IsVisible;
        set
        {
            _rulerConfig.IsVisible = value;
            InvalidateVisual();
        }
    }
    
    public new void InvalidateVisual()
    {
        _skElement.InvalidateVisual();
    }
    
    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);
        
        if (_camera == null)
            return;
        
        // Update camera viewport size
        _camera.ViewportSize = new CAD2DModel.Camera.Size(e.Info.Width, e.Info.Height);
        
        // Draw grid
        if (_isGridVisible)
        {
            DrawGrid(canvas, e.Info.Width, e.Info.Height);
        }
        
        // Draw contours (before geometry so boundaries are drawn on top)
        if (_contourService != null && _contourService.Settings.IsVisible)
        {
            DrawContours(canvas);
        }
        
        // Draw geometry entities
        DrawPolylines(canvas);
        DrawBoundaries(canvas);
        
        // Draw annotations (after geometry but before mode overlays)
        if (_geometryModel != null)
        {
            var renderContext = new SkiaRenderContext(canvas, _camera);
            _annotationRenderer.RenderAnnotations(_geometryModel.Annotations, renderContext);
        }
        
        // Draw mode overlays (selection box, temporary geometry, etc.)
        if (_modeManager != null)
        {
            // Create a simple render context
            var renderContext = new SkiaRenderContext(canvas, _camera);
            _modeManager.CurrentMode.Render(renderContext);
        }
        
        // Draw ruler LAST (on top of everything)
        if (_rulerConfig.IsVisible)
        {
            _rulerRenderer.Render(canvas, _camera, _rulerConfig, _lastMouseScreenPos);
        }
    }
    
    private void DrawGrid(SKCanvas canvas, int width, int height)
    {
        if (_camera == null)
            return;
        
        using var gridPaint = new SKPaint
        {
            Color = new SKColor(230, 230, 230),
            StrokeWidth = 1,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        
        // Calculate grid spacing in screen units
        double gridSpacing = 50; // pixels
        double worldGridSpacing = gridSpacing * _camera.Scale;
        
        // Calculate grid bounds
        var worldBounds = _camera.WorldBounds;
        
        // Vertical lines
        double startX = Math.Floor(worldBounds.X / worldGridSpacing) * worldGridSpacing;
        for (double x = startX; x <= worldBounds.Right; x += worldGridSpacing)
        {
            var screenPt1 = _camera.WorldToScreen(new Point2D(x, worldBounds.Top));
            var screenPt2 = _camera.WorldToScreen(new Point2D(x, worldBounds.Bottom));
            canvas.DrawLine((float)screenPt1.X, (float)screenPt1.Y, (float)screenPt2.X, (float)screenPt2.Y, gridPaint);
        }
        
        // Horizontal lines
        double startY = Math.Floor(worldBounds.Y / worldGridSpacing) * worldGridSpacing;
        for (double y = startY; y <= worldBounds.Bottom; y += worldGridSpacing)
        {
            var screenPt1 = _camera.WorldToScreen(new Point2D(worldBounds.Left, y));
            var screenPt2 = _camera.WorldToScreen(new Point2D(worldBounds.Right, y));
            canvas.DrawLine((float)screenPt1.X, (float)screenPt1.Y, (float)screenPt2.X, (float)screenPt2.Y, gridPaint);
        }
        
        // Draw origin axes
        using var axisPaint = new SKPaint
        {
            Color = new SKColor(200, 200, 200),
            StrokeWidth = 2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        
        var origin = _camera.WorldToScreen(Point2D.Origin);
        
        // X-axis (horizontal)
        canvas.DrawLine(0, (float)origin.Y, width, (float)origin.Y, axisPaint);
        
        // Y-axis (vertical)
        canvas.DrawLine((float)origin.X, 0, (float)origin.X, height, axisPaint);
    }
    
    private void DrawPolylines(SKCanvas canvas)
    {
        if (_camera == null)
            return;
        
        using var polylinePaint = new SKPaint
        {
            Color = SKColors.Blue,
            StrokeWidth = 2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        
        using var vertexPaint = new SKPaint
        {
            Color = SKColors.DarkBlue,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        
        foreach (var polyline in Polylines)
        {
            if (!polyline.IsVisible || polyline.Vertices.Count < 2)
                continue;
            
            // Draw line segments
            for (int i = 0; i < polyline.GetSegmentCount(); i++)
            {
                var segment = polyline.GetSegment(i);
                var screenStart = _camera.WorldToScreen(segment.Start);
                var screenEnd = _camera.WorldToScreen(segment.End);
                
                canvas.DrawLine(
                    (float)screenStart.X, (float)screenStart.Y,
                    (float)screenEnd.X, (float)screenEnd.Y,
                    polylinePaint);
            }
            
            // Draw vertices
            foreach (var vertex in polyline.Vertices)
            {
                var screenPos = _camera.WorldToScreen(vertex.Location);
                float radius = vertex.IsSelected ? 6f : 4f;
                
                if (vertex.IsSelected)
                {
                    vertexPaint.Color = SKColors.Red;
                }
                else
                {
                    vertexPaint.Color = SKColors.DarkBlue;
                }
                
                canvas.DrawCircle((float)screenPos.X, (float)screenPos.Y, radius, vertexPaint);
            }
        }
    }
    
    private void DrawBoundaries(SKCanvas canvas)
    {
        if (_camera == null)
            return;
        
        using var boundaryPaint = new SKPaint
        {
            Color = new SKColor(0, 150, 0), // Dark green
            StrokeWidth = 3,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        
        using var fillPaint = new SKPaint
        {
            Color = new SKColor(0, 200, 0, 30), // Light green with transparency
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        
        using var vertexPaint = new SKPaint
        {
            Color = new SKColor(0, 100, 0), // Darker green
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        
        foreach (var boundary in Boundaries)
        {
            if (!boundary.IsVisible || boundary.Vertices.Count < 3)
                continue;
            
            // Create path for boundary
            using var path = new SKPath();
            
            var firstVertex = boundary.Vertices[0];
            var screenPos = _camera.WorldToScreen(firstVertex.Location);
            path.MoveTo((float)screenPos.X, (float)screenPos.Y);
            
            for (int i = 1; i < boundary.Vertices.Count; i++)
            {
                screenPos = _camera.WorldToScreen(boundary.Vertices[i].Location);
                path.LineTo((float)screenPos.X, (float)screenPos.Y);
            }
            
            path.Close();
            
            // Fill boundary
            canvas.DrawPath(path, fillPaint);
            
            // Draw boundary outline
            canvas.DrawPath(path, boundaryPaint);
            
            // Draw vertices
            foreach (var vertex in boundary.Vertices)
            {
                screenPos = _camera.WorldToScreen(vertex.Location);
                float radius = vertex.IsSelected ? 7f : 5f;
                
                if (vertex.IsSelected)
                {
                    vertexPaint.Color = SKColors.Red;
                }
                else
                {
                    vertexPaint.Color = new SKColor(0, 100, 0);
                }
                
                canvas.DrawCircle((float)screenPos.X, (float)screenPos.Y, radius, vertexPaint);
            }
        }
    }
    
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_camera == null)
            return;
        
        // Zoom to cursor position
        var mousePos = e.GetPosition(this);
        var worldPos = _camera.ScreenToWorld(new CAD2DModel.Camera.Point(mousePos.X, mousePos.Y));
        
        // Zoom factor
        double zoomFactor = e.Delta > 0 ? 0.9 : 1.1;
        double newScale = _camera.Scale * zoomFactor;
        
        // Clamp scale
        newScale = Math.Clamp(newScale, 0.001, 1000.0);
        
        // Adjust center to keep world position under cursor
        _camera.Center = new Point2D(
            worldPos.X - (mousePos.X - _camera.ViewportSize.Width / 2) * newScale,
            worldPos.Y - (mousePos.Y - _camera.ViewportSize.Height / 2) * newScale
        );
        
        _camera.Scale = newScale;
        InvalidateVisual();
        
        e.Handled = true;
    }
    
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Give focus to handle keyboard events
        Focus();
        
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            _isPanning = true;
            var mousePos = e.GetPosition(this);
            _lastMousePosition = new Point2D(mousePos.X, mousePos.Y);
            CaptureMouse();
            e.Handled = true;
            return;
        }
        
        // Pass event to current mode
        if (_modeManager != null && _camera != null)
        {
            var screenPos = e.GetPosition(this);
            var worldPos = _camera.ScreenToWorld(new CAD2DModel.Camera.Point(screenPos.X, screenPos.Y));
            
            var button = ConvertMouseButton(e.ChangedButton);
            var modifiers = ConvertModifierKeys(Keyboard.Modifiers);
            
            // Handle right-click for context menu
            if (button == CAD2DModel.Interaction.MouseButton.Right)
            {
                ShowContextMenu(worldPos);
                e.Handled = true;
                return;
            }
            
            _modeManager.CurrentMode.OnMouseDown(worldPos, button, modifiers);
            InvalidateVisual();
            e.Handled = true;
        }
    }
    
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_camera == null)
            return;
        
        // Always update mouse position for status bar and ruler crosshair
        var screenPos = e.GetPosition(this);
        var worldPos = _camera.ScreenToWorld(new CAD2DModel.Camera.Point(screenPos.X, screenPos.Y));
        MousePositionChanged?.Invoke(this, worldPos);
        
        // Update mouse position for ruler crosshair
        _lastMouseScreenPos = new SKPoint((float)screenPos.X, (float)screenPos.Y);
        
        if (_isPanning)
        {
            var mousePos = e.GetPosition(this);
            var currentPos = new Point2D(mousePos.X, mousePos.Y);
            
            var delta = currentPos - _lastMousePosition;
            
            _camera.Center = new Point2D(
                _camera.Center.X - delta.X * _camera.Scale,
                _camera.Center.Y - delta.Y * _camera.Scale
            );
            
            _lastMousePosition = currentPos;
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        
        // Pass event to current mode
        if (_modeManager != null)
        {
            var modifiers = ConvertModifierKeys(Keyboard.Modifiers);
            
            _modeManager.CurrentMode.OnMouseMove(worldPos, modifiers);
            InvalidateVisual();
        }
    }
    
    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Released && _isPanning)
        {
            _isPanning = false;
            ReleaseMouseCapture();
            e.Handled = true;
            return;
        }
        
        // Pass event to current mode
        if (_modeManager != null && _camera != null)
        {
            var screenPos = e.GetPosition(this);
            var worldPos = _camera.ScreenToWorld(new CAD2DModel.Camera.Point(screenPos.X, screenPos.Y));
            
            var button = ConvertMouseButton(e.ChangedButton);
            
            _modeManager.CurrentMode.OnMouseUp(worldPos, button);
            InvalidateVisual();
            e.Handled = true;
        }
    }
    
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_modeManager != null)
        {
            var key = ConvertKey(e.Key);
            var modifiers = ConvertModifierKeys(Keyboard.Modifiers);
            
            _modeManager.CurrentMode.OnKeyDown(key, modifiers);
            InvalidateVisual();
            e.Handled = true;
        }
    }
    
    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (_modeManager != null)
        {
            var key = ConvertKey(e.Key);
            
            _modeManager.CurrentMode.OnKeyUp(key);
            InvalidateVisual();
            e.Handled = true;
        }
    }
    
    private void OnModeChanged(object? sender, ModeChangedEventArgs e)
    {
        // Unsubscribe from old mode
        if (e.OldMode != null)
        {
            e.OldMode.StateChanged -= OnModeStateChanged;
        }
        
        // Subscribe to new mode
        if (e.NewMode != null)
        {
            e.NewMode.StateChanged += OnModeStateChanged;
        }
        
        UpdateCursor();
        UpdateStatusText();
        InvalidateVisual();
    }
    
    private void OnModeStateChanged(object? sender, ModeStateChangedEventArgs e)
    {
        UpdateStatusText();
        UpdateCursor(); // Also update cursor when state/substate changes
        InvalidateVisual();
    }
    
    private void UpdateCursor()
    {
        if (_modeManager == null)
            return;
        
        var modeCursor = _modeManager.CurrentMode.Cursor;
        Cursor = ConvertCursor(modeCursor);
    }
    
    private void UpdateStatusText()
    {
        if (_modeManager == null)
            return;
        
        var statusText = _modeManager.CurrentMode.StatusPrompt;
        StatusTextChanged?.Invoke(this, statusText);
    }
    
    private void ShowContextMenu(Point2D worldPoint)
    {
        if (_modeManager == null)
            return;
        
        var menuItems = _modeManager.CurrentMode.GetContextMenuItems(worldPoint);
        if (!menuItems.Any())
            return;
        
        var contextMenu = new ContextMenu();
        
        foreach (var item in menuItems)
        {
            if (item.IsSeparator)
            {
                contextMenu.Items.Add(new Separator());
            }
            else
            {
                var menuItem = new MenuItem
                {
                    Header = item.Text,
                    IsEnabled = item.IsEnabled
                };
                
                // Add checkmark if IsChecked is true
                if (item.IsChecked)
                {
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = true;
                }
                
                // Handle Action property if it exists (check using reflection for any item with Action property)
                var actionProperty = item.GetType().GetProperty("Action");
                if (actionProperty != null)
                {
                    var action = actionProperty.GetValue(item) as Action;
                    if (action != null)
                    {
                        menuItem.Click += (s, e) =>
                        {
                            action.Invoke();
                            InvalidateVisual(); // Refresh view after action
                        };
                    }
                }
                else if (item.Command != null)
                {
                    menuItem.Command = item.Command;
                }
                
                contextMenu.Items.Add(menuItem);
            }
        }
        
        contextMenu.IsOpen = true;
        ContextMenu = contextMenu;
    }
    
    #region Type Conversion Helpers
    
    private static CAD2DModel.Interaction.MouseButton ConvertMouseButton(System.Windows.Input.MouseButton button)
    {
        return button switch
        {
            System.Windows.Input.MouseButton.Left => CAD2DModel.Interaction.MouseButton.Left,
            System.Windows.Input.MouseButton.Right => CAD2DModel.Interaction.MouseButton.Right,
            System.Windows.Input.MouseButton.Middle => CAD2DModel.Interaction.MouseButton.Middle,
            System.Windows.Input.MouseButton.XButton1 => CAD2DModel.Interaction.MouseButton.XButton1,
            System.Windows.Input.MouseButton.XButton2 => CAD2DModel.Interaction.MouseButton.XButton2,
            _ => CAD2DModel.Interaction.MouseButton.Left // Default
        };
    }
    
    private static CAD2DModel.Interaction.ModifierKeys ConvertModifierKeys(System.Windows.Input.ModifierKeys modifiers)
    {
        var result = CAD2DModel.Interaction.ModifierKeys.None;
        
        if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
            result |= CAD2DModel.Interaction.ModifierKeys.Control;
        if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
            result |= CAD2DModel.Interaction.ModifierKeys.Shift;
        if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
            result |= CAD2DModel.Interaction.ModifierKeys.Alt;
        
        return result;
    }
    
    private static CAD2DModel.Interaction.Key ConvertKey(System.Windows.Input.Key key)
    {
        return key switch
        {
            System.Windows.Input.Key.Enter => CAD2DModel.Interaction.Key.Enter,
            System.Windows.Input.Key.Escape => CAD2DModel.Interaction.Key.Escape,
            System.Windows.Input.Key.Delete => CAD2DModel.Interaction.Key.Delete,
            System.Windows.Input.Key.Back => CAD2DModel.Interaction.Key.Backspace,
            System.Windows.Input.Key.Space => CAD2DModel.Interaction.Key.Space,
            System.Windows.Input.Key.Tab => CAD2DModel.Interaction.Key.Tab,
            // Letter keys for mode shortcuts
            System.Windows.Input.Key.A => CAD2DModel.Interaction.Key.A,
            System.Windows.Input.Key.C => CAD2DModel.Interaction.Key.C,
            System.Windows.Input.Key.L => CAD2DModel.Interaction.Key.L,
            System.Windows.Input.Key.U => CAD2DModel.Interaction.Key.U,
            _ => CAD2DModel.Interaction.Key.None
        };
    }
    
    private static System.Windows.Input.Cursor ConvertCursor(CAD2DModel.Interaction.Cursor cursor)
    {
        return cursor switch
        {
            CAD2DModel.Interaction.Cursor.Arrow => Cursors.Arrow,
            CAD2DModel.Interaction.Cursor.Cross => Cursors.Cross,
            CAD2DModel.Interaction.Cursor.Hand => Cursors.Hand,
            CAD2DModel.Interaction.Cursor.Wait => Cursors.Wait,
            CAD2DModel.Interaction.Cursor.SizeAll => Cursors.SizeAll,
            CAD2DModel.Interaction.Cursor.PickBox => Cursors.Cross, // Use Cross cursor for pick box (closest to selection cursor)
            _ => Cursors.Arrow
        };
    }
    
    private void DrawContours(SKCanvas canvas)
    {
        if (_camera == null || _contourService == null)
            return;
        
        var contourData = _contourService.CurrentContourData;
        if (contourData == null || !contourData.IsValid)
            return;
        
        var settings = _contourService.Settings;
        
        // Determine value range
        double minValue = settings.MinValue ?? contourData.MinValue;
        double maxValue = settings.MaxValue ?? contourData.MaxValue;
        
        if (Math.Abs(maxValue - minValue) < 1e-10)
            return; // No variation in data
        
        // Draw filled contours (triangles colored by interpolated values)
        if (settings.ShowFilledContours)
        {
            // Update color mapper scheme
            _colorMapper.Scheme = settings.ColorScheme;
            
            // Check if we need to rebuild color cache
            bool needsRebuild = _cachedContourColors == null ||
                               _lastRenderedContourData != contourData ||
                               Math.Abs(_lastMinValue - minValue) > 1e-10 ||
                               Math.Abs(_lastMaxValue - maxValue) > 1e-10 ||
                               Math.Abs(_lastFillOpacity - settings.FillOpacity) > 1e-10 ||
                               _lastColorScheme != settings.ColorScheme;
            
            if (needsRebuild)
                {
                    // Build color array (only once per contour generation)
                    var allColors = new List<SKColor>(contourData.Triangles.Count);
                    
                    for (int i = 0; i < contourData.Triangles.Count; i += 3)
                    {
                        int idx0 = contourData.Triangles[i];
                        int idx1 = contourData.Triangles[i + 1];
                        int idx2 = contourData.Triangles[i + 2];
                        
                        // Check BOTH Values and MeshPoints to ensure consistency with vertex building
                        if (idx0 >= contourData.Values.Count || 
                            idx1 >= contourData.Values.Count || 
                            idx2 >= contourData.Values.Count ||
                            idx0 >= contourData.MeshPoints.Count || 
                            idx1 >= contourData.MeshPoints.Count || 
                            idx2 >= contourData.MeshPoints.Count)
                            continue;
                        
                        var v0 = contourData.Values[idx0];
                        var v1 = contourData.Values[idx1];
                        var v2 = contourData.Values[idx2];
                        
                        // Map values to colors
                        var c0 = _colorMapper.MapValue(v0, minValue, maxValue);
                        var c1 = _colorMapper.MapValue(v1, minValue, maxValue);
                        var c2 = _colorMapper.MapValue(v2, minValue, maxValue);
                        
                        // Add colors
                        allColors.Add(new SKColor(c0.R, c0.G, c0.B, (byte)(255 * settings.FillOpacity)));
                        allColors.Add(new SKColor(c1.R, c1.G, c1.B, (byte)(255 * settings.FillOpacity)));
                        allColors.Add(new SKColor(c2.R, c2.G, c2.B, (byte)(255 * settings.FillOpacity)));
                    }
                    
                    _cachedContourColors = allColors.ToArray();
                    _lastRenderedContourData = contourData;
                    _lastMinValue = minValue;
                    _lastMaxValue = maxValue;
                    _lastFillOpacity = settings.FillOpacity;
                    _lastColorScheme = settings.ColorScheme;
                    
                    System.Diagnostics.Debug.WriteLine($"Rebuilt contour colors: {allColors.Count / 3} triangles, value range: {minValue:F2} - {maxValue:F2}");
                }
                
                // Build vertex array (must do every frame due to camera transform)
                // IMPORTANT: Only build if we have matching cached colors for this exact contour data
                if (_cachedContourColors == null || _lastRenderedContourData != contourData)
                {
                    // Colors don't match this contour data, skip drawing this frame
                    return;
                }
                
                var allVertices = new List<SKPoint>(contourData.Triangles.Count);
                
                for (int i = 0; i < contourData.Triangles.Count; i += 3)
                {
                    int idx0 = contourData.Triangles[i];
                    int idx1 = contourData.Triangles[i + 1];
                    int idx2 = contourData.Triangles[i + 2];
                    
                    // Check BOTH Values and MeshPoints to ensure consistency with color building
                    if (idx0 >= contourData.Values.Count || 
                        idx1 >= contourData.Values.Count || 
                        idx2 >= contourData.Values.Count ||
                        idx0 >= contourData.MeshPoints.Count || 
                        idx1 >= contourData.MeshPoints.Count || 
                        idx2 >= contourData.MeshPoints.Count)
                        continue;
                    
                    var p0 = contourData.MeshPoints[idx0];
                    var p1 = contourData.MeshPoints[idx1];
                    var p2 = contourData.MeshPoints[idx2];
                    
                    // Convert to screen coordinates
                    var s0 = _camera.WorldToScreen(p0);
                    var s1 = _camera.WorldToScreen(p1);
                    var s2 = _camera.WorldToScreen(p2);
                    
                    // Add vertices
                    allVertices.Add(new SKPoint((float)s0.X, (float)s0.Y));
                    allVertices.Add(new SKPoint((float)s1.X, (float)s1.Y));
                    allVertices.Add(new SKPoint((float)s2.X, (float)s2.Y));
                }
                
                // Draw all triangles in one call with per-vertex color interpolation
                if (allVertices.Count > 0 && _cachedContourColors != null && allVertices.Count == _cachedContourColors.Length)
                {
                    using var paint = new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                        Color = SKColors.White,
                        BlendMode = SKBlendMode.Src // Disable blending to ensure full opacity
                    };
                    
                    canvas.DrawVertices(
                        SKVertexMode.Triangles, 
                        allVertices.ToArray(), 
                        texs: null,
                        _cachedContourColors, 
                        paint);
                }
        }
        
        // Draw contour lines
        if (settings.ShowContourLines)
        {
            var levels = _colorMapper.GetContourLevels(minValue, maxValue, settings.NumberOfLevels);
            
            using var linePaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 128), // Semi-transparent black
                StrokeWidth = 1,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };
            
            // Draw contour lines by finding edges that cross each level
            foreach (var level in levels)
            {
                for (int i = 0; i < contourData.Triangles.Count; i += 3)
                {
                    int idx0 = contourData.Triangles[i];
                    int idx1 = contourData.Triangles[i + 1];
                    int idx2 = contourData.Triangles[i + 2];
                    
                    if (idx0 >= contourData.MeshPoints.Count || 
                        idx1 >= contourData.MeshPoints.Count || 
                        idx2 >= contourData.MeshPoints.Count)
                        continue;
                    
                    var points = new[]
                    {
                        contourData.MeshPoints[idx0],
                        contourData.MeshPoints[idx1],
                        contourData.MeshPoints[idx2]
                    };
                    
                    var values = new[]
                    {
                        contourData.Values[idx0],
                        contourData.Values[idx1],
                        contourData.Values[idx2]
                    };
                    
                    // Find where contour line crosses triangle edges
                    var crossings = new List<Point2D>();
                    
                    for (int j = 0; j < 3; j++)
                    {
                        int next = (j + 1) % 3;
                        var crossing = GetContourCrossing(points[j], points[next], values[j], values[next], level);
                        if (crossing.HasValue)
                        {
                            crossings.Add(crossing.Value);
                        }
                    }
                    
                    // Draw line segment if we have exactly 2 crossings
                    if (crossings.Count == 2)
                    {
                        var s0 = _camera.WorldToScreen(crossings[0]);
                        var s1 = _camera.WorldToScreen(crossings[1]);
                        canvas.DrawLine((float)s0.X, (float)s0.Y, (float)s1.X, (float)s1.Y, linePaint);
                    }
                }
            }
        }
        
        // Mask excavations with white fill to hide contours inside them
        if (_geometryModel != null)
        {
            var excavations = _geometryModel.Entities
                .OfType<Boundary>()
                .Where(b => b is not ExternalBoundary);
            
            foreach (var excavation in excavations)
            {
                if (!excavation.IsVisible || excavation.Vertices.Count < 3)
                    continue;
                
                using var maskPath = new SKPath();
                var firstVertex = _camera.WorldToScreen(excavation.Vertices[0].Location);
                maskPath.MoveTo((float)firstVertex.X, (float)firstVertex.Y);
                
                for (int i = 1; i < excavation.Vertices.Count; i++)
                {
                    var screenPos = _camera.WorldToScreen(excavation.Vertices[i].Location);
                    maskPath.LineTo((float)screenPos.X, (float)screenPos.Y);
                }
                
                maskPath.Close();
                
                using var maskPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };
                
                canvas.DrawPath(maskPath, maskPaint);
            }
        }
    }
    
    private Point2D? GetContourCrossing(Point2D p1, Point2D p2, double v1, double v2, double level)
    {
        // Check if level is between v1 and v2
        if ((v1 <= level && v2 >= level) || (v1 >= level && v2 <= level))
        {
            if (Math.Abs(v2 - v1) < 1e-10)
                return null;
            
            // Linear interpolation
            double t = (level - v1) / (v2 - v1);
            return new Point2D(
                p1.X + t * (p2.X - p1.X),
                p1.Y + t * (p2.Y - p1.Y)
            );
        }
        
        return null;
    }
    
    #endregion
}

/// <summary>
/// Simple render context for SkiaSharp rendering
/// </summary>
internal class SkiaRenderContext : IRenderContext
{
    public SKCanvas Canvas { get; }
    public Camera2D Camera { get; }
    
    public SkiaRenderContext(SKCanvas canvas, Camera2D camera)
    {
        Canvas = canvas;
        Camera = camera;
    }
    
    public void DrawLine(Point2D worldStart, Point2D worldEnd, byte r, byte g, byte b, float strokeWidth = 1, bool dashed = false)
    {
        var screenStart = Camera.WorldToScreen(worldStart);
        var screenEnd = Camera.WorldToScreen(worldEnd);
        
        using var paint = new SKPaint
        {
            Color = new SKColor(r, g, b),
            StrokeWidth = strokeWidth,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        
        if (dashed)
        {
            paint.PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0);
        }
        
        Canvas.DrawLine(
            (float)screenStart.X, (float)screenStart.Y,
            (float)screenEnd.X, (float)screenEnd.Y,
            paint);
    }
    
    public void DrawSnapIndicator(Point2D worldPoint, CAD2DModel.Services.SnapMode snapType)
    {
        var screenPos = Camera.WorldToScreen(worldPoint);
        float x = (float)screenPos.X;
        float y = (float)screenPos.Y;
        float size = 8f; // indicator size in pixels
        
        using var paint = new SKPaint
        {
            Color = new SKColor(255, 165, 0), // Orange
            StrokeWidth = 2f,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        
        switch (snapType)
        {
            case CAD2DModel.Services.SnapMode.Vertex:
                // Circle
                Canvas.DrawCircle(x, y, size, paint);
                break;
                
            case CAD2DModel.Services.SnapMode.Midpoint:
                // Circle with cross in the middle
                Canvas.DrawCircle(x, y, size, paint);
                Canvas.DrawLine(x - size/2, y, x + size/2, y, paint);
                Canvas.DrawLine(x, y - size/2, x, y + size/2, paint);
                break;
                
            case CAD2DModel.Services.SnapMode.Grid:
                // Square
                Canvas.DrawRect(x - size, y - size, size * 2, size * 2, paint);
                break;
                
            case CAD2DModel.Services.SnapMode.Ortho:
                // X shape (diagonal cross)
                Canvas.DrawLine(x - size, y - size, x + size, y + size, paint);
                Canvas.DrawLine(x - size, y + size, x + size, y - size, paint);
                break;
                
            case CAD2DModel.Services.SnapMode.Nearest:
                // Diamond
                using (var path = new SKPath())
                {
                    path.MoveTo(x, y - size);
                    path.LineTo(x + size, y);
                    path.LineTo(x, y + size);
                    path.LineTo(x - size, y);
                    path.Close();
                    Canvas.DrawPath(path, paint);
                }
                break;
                
            case CAD2DModel.Services.SnapMode.Intersection:
                // X in a circle
                Canvas.DrawCircle(x, y, size, paint);
                Canvas.DrawLine(x - size/2, y - size/2, x + size/2, y + size/2, paint);
                Canvas.DrawLine(x - size/2, y + size/2, x + size/2, y - size/2, paint);
                break;
        }
    }
    
    public void DrawText(string text, Point2D worldPosition, float fontSize, string fontFamily, 
                         byte r, byte g, byte b, double rotationDegrees = 0, bool bold = false, 
                         bool italic = false, bool drawBackground = false, byte bgR = 255, byte bgG = 255, 
                         byte bgB = 255, byte bgA = 200)
    {
        var screenPos = Camera.WorldToScreen(worldPosition);
        
        var fontStyle = SKFontStyle.Normal;
        if (bold && italic)
            fontStyle = SKFontStyle.BoldItalic;
        else if (bold)
            fontStyle = SKFontStyle.Bold;
        else if (italic)
            fontStyle = SKFontStyle.Italic;
        
        using var typeface = SKTypeface.FromFamilyName(fontFamily, fontStyle);
        using var paint = new SKPaint
        {
            Typeface = typeface,
            TextSize = fontSize,
            Color = new SKColor(r, g, b),
            IsAntialias = true,
            TextAlign = SKTextAlign.Left
        };
        
        // Measure text for background
        var textBounds = new SKRect();
        paint.MeasureText(text, ref textBounds);
        
        Canvas.Save();
        
        // Apply rotation
        if (Math.Abs(rotationDegrees) > 0.001)
        {
            Canvas.RotateDegrees((float)rotationDegrees, (float)screenPos.X, (float)screenPos.Y);
        }
        
        // Draw background if requested
        if (drawBackground)
        {
            using var bgPaint = new SKPaint
            {
                Color = new SKColor(bgR, bgG, bgB, bgA),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            
            var bgRect = new SKRect(
                (float)screenPos.X + textBounds.Left - 2,
                (float)screenPos.Y + textBounds.Top - 2,
                (float)screenPos.X + textBounds.Right + 2,
                (float)screenPos.Y + textBounds.Bottom + 2
            );
            Canvas.DrawRect(bgRect, bgPaint);
        }
        
        // Draw text
        Canvas.DrawText(text, (float)screenPos.X, (float)screenPos.Y, paint);
        
        Canvas.Restore();
    }
    
    public void DrawRectangle(Point2D worldTopLeft, Point2D worldBottomRight, byte r, byte g, byte b, 
                             float strokeWidth = 1, bool filled = false, byte fillR = 128, byte fillG = 128, 
                             byte fillB = 128, byte fillA = 100)
    {
        var screenTopLeft = Camera.WorldToScreen(worldTopLeft);
        var screenBottomRight = Camera.WorldToScreen(worldBottomRight);
        
        var rect = SKRect.Create(
            (float)screenTopLeft.X,
            (float)screenTopLeft.Y,
            (float)(screenBottomRight.X - screenTopLeft.X),
            (float)(screenBottomRight.Y - screenTopLeft.Y)
        );
        
        if (filled)
        {
            using var fillPaint = new SKPaint
            {
                Color = new SKColor(fillR, fillG, fillB, fillA),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            Canvas.DrawRect(rect, fillPaint);
        }
        
        using var strokePaint = new SKPaint
        {
            Color = new SKColor(r, g, b),
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        Canvas.DrawRect(rect, strokePaint);
    }
    
    public void DrawRectangle(Point2D worldTopLeft, double width, double height, byte r, byte g, byte b, 
                             float strokeWidth = 1, bool filled = false, byte fillR = 128, byte fillG = 128, 
                             byte fillB = 128, byte fillA = 100)
    {
        var worldBottomRight = new Point2D(worldTopLeft.X + width, worldTopLeft.Y + height);
        DrawRectangle(worldTopLeft, worldBottomRight, r, g, b, strokeWidth, filled, fillR, fillG, fillB, fillA);
    }
    
    public void DrawCircle(Point2D worldCenter, double worldRadius, byte r, byte g, byte b, 
                          float strokeWidth = 1, bool filled = false, byte fillR = 128, byte fillG = 128, 
                          byte fillB = 128, byte fillA = 100)
    {
        var screenCenter = Camera.WorldToScreen(worldCenter);
        var screenRadiusPoint = Camera.WorldToScreen(new Point2D(worldCenter.X + worldRadius, worldCenter.Y));
        float screenRadius = (float)Math.Abs(screenRadiusPoint.X - screenCenter.X);
        
        if (filled)
        {
            using var fillPaint = new SKPaint
            {
                Color = new SKColor(fillR, fillG, fillB, fillA),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            Canvas.DrawCircle((float)screenCenter.X, (float)screenCenter.Y, screenRadius, fillPaint);
        }
        
        using var strokePaint = new SKPaint
        {
            Color = new SKColor(r, g, b),
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        Canvas.DrawCircle((float)screenCenter.X, (float)screenCenter.Y, screenRadius, strokePaint);
    }
    
    public void DrawArc(Point2D worldCenter, double worldRadius, double startAngleDegrees, 
                       double sweepAngleDegrees, byte r, byte g, byte b, float strokeWidth = 1)
    {
        var screenCenter = Camera.WorldToScreen(worldCenter);
        var screenRadiusPoint = Camera.WorldToScreen(new Point2D(worldCenter.X + worldRadius, worldCenter.Y));
        float screenRadius = (float)Math.Abs(screenRadiusPoint.X - screenCenter.X);
        
        var rect = new SKRect(
            (float)screenCenter.X - screenRadius,
            (float)screenCenter.Y - screenRadius,
            (float)screenCenter.X + screenRadius,
            (float)screenCenter.Y + screenRadius
        );
        
        using var paint = new SKPaint
        {
            Color = new SKColor(r, g, b),
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        
        using var path = new SKPath();
        path.AddArc(rect, (float)startAngleDegrees, (float)sweepAngleDegrees);
        Canvas.DrawPath(path, paint);
    }
    
    public void DrawArrowHead(Point2D worldLineStart, Point2D worldLineEnd, byte r, byte g, byte b, 
                             double arrowSize = 10.0, bool filled = true)
    {
        var screenStart = Camera.WorldToScreen(worldLineStart);
        var screenEnd = Camera.WorldToScreen(worldLineEnd);
        
        // Calculate arrow direction
        double dx = screenEnd.X - screenStart.X;
        double dy = screenEnd.Y - screenStart.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        
        if (length < 0.001) return; // Too short to draw arrow
        
        // Normalize direction
        dx /= length;
        dy /= length;
        
        // Arrow points (perpendicular to line direction)
        double perpX = -dy;
        double perpY = dx;
        
        // Calculate arrow head points
        float tipX = (float)screenEnd.X;
        float tipY = (float)screenEnd.Y;
        float baseX = (float)(screenEnd.X - dx * arrowSize);
        float baseY = (float)(screenEnd.Y - dy * arrowSize);
        float side1X = (float)(baseX + perpX * arrowSize * 0.5);
        float side1Y = (float)(baseY + perpY * arrowSize * 0.5);
        float side2X = (float)(baseX - perpX * arrowSize * 0.5);
        float side2Y = (float)(baseY - perpY * arrowSize * 0.5);
        
        using var path = new SKPath();
        path.MoveTo(tipX, tipY);
        path.LineTo(side1X, side1Y);
        path.LineTo(side2X, side2Y);
        path.Close();
        
        using var paint = new SKPaint
        {
            Color = new SKColor(r, g, b),
            Style = filled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };
        
        Canvas.DrawPath(path, paint);
    }
    
    public void DrawControlPoint(Point2D worldPosition, byte r = 0, byte g = 100, byte b = 255, 
                                bool highlighted = false)
    {
        var screenPos = Camera.WorldToScreen(worldPosition);
        float size = highlighted ? 6f : 4f;
        
        // Draw filled square for control point
        using var fillPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255), // White fill
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        
        var rect = new SKRect(
            (float)screenPos.X - size,
            (float)screenPos.Y - size,
            (float)screenPos.X + size,
            (float)screenPos.Y + size
        );
        Canvas.DrawRect(rect, fillPaint);
        
        // Draw border
        using var strokePaint = new SKPaint
        {
            Color = new SKColor(r, g, b),
            StrokeWidth = highlighted ? 2f : 1.5f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        Canvas.DrawRect(rect, strokePaint);
    }
}

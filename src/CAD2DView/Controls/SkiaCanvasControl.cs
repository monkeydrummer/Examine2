using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CAD2DModel.Camera;
using CAD2DModel.Geometry;
using CAD2DModel.Interaction;
using CAD2DModel.Services;
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
        
        // Draw geometry entities
        DrawPolylines(canvas);
        DrawBoundaries(canvas);
        
        // Draw mode overlays (selection box, temporary geometry, etc.)
        if (_modeManager != null)
        {
            // Create a simple render context
            var renderContext = new SkiaRenderContext(canvas, _camera);
            _modeManager.CurrentMode.Render(renderContext);
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
            
            _modeManager.CurrentMode.OnMouseDown(worldPos, button, modifiers);
            InvalidateVisual();
            e.Handled = true;
        }
    }
    
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_camera == null)
            return;
        
        // Always update mouse position for status bar
        var screenPos = e.GetPosition(this);
        var worldPos = _camera.ScreenToWorld(new CAD2DModel.Camera.Point(screenPos.X, screenPos.Y));
        MousePositionChanged?.Invoke(this, worldPos);
        
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
        UpdateCursor();
        UpdateStatusText();
        InvalidateVisual();
    }
    
    private void OnModeStateChanged(object? sender, ModeStateChangedEventArgs e)
    {
        UpdateStatusText();
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
}

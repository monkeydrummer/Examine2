using CAD2DModel.Geometry;
using System.Windows.Input;

namespace CAD2DModel.Interaction;

/// <summary>
/// State of an interaction mode
/// </summary>
public enum ModeState
{
    Idle,           // Mode just entered, waiting for first input
    Active,         // Mode is actively processing (e.g., dragging)
    WaitingForInput, // Mode needs more input (e.g., "Pick second point")
    Completed,      // Mode finished successfully
    Cancelled       // Mode was cancelled
}

/// <summary>
/// Context menu item interface
/// </summary>
public interface IContextMenuItem
{
    string Text { get; }
    ICommand? Command { get; }
    bool IsEnabled { get; }
    bool IsSeparator { get; }
    bool IsChecked { get; }
    IEnumerable<IContextMenuItem>? SubItems { get; }
}

/// <summary>
/// Interface for modal interaction modes
/// </summary>
public interface IInteractionMode
{
    /// <summary>
    /// Name of the mode
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Prompt to display in status bar
    /// </summary>
    string StatusPrompt { get; }
    
    /// <summary>
    /// Cursor to display for this mode
    /// </summary>
    Cursor Cursor { get; }
    
    /// <summary>
    /// Current state of the mode
    /// </summary>
    ModeState CurrentState { get; }
    
    /// <summary>
    /// Current substate of the mode (for fine-grained state management)
    /// </summary>
    ModeSubState CurrentSubState { get; }
    
    /// <summary>
    /// Called when entering this mode
    /// </summary>
    void OnEnter(ModeContext context);
    
    /// <summary>
    /// Called when exiting this mode
    /// </summary>
    void OnExit();
    
    /// <summary>
    /// Check if the mode can be exited
    /// </summary>
    bool CanExit();
    
    /// <summary>
    /// Handle mouse down event
    /// </summary>
    void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers);
    
    /// <summary>
    /// Handle mouse move event
    /// </summary>
    void OnMouseMove(Point2D worldPoint, ModifierKeys modifiers);
    
    /// <summary>
    /// Handle mouse up event
    /// </summary>
    void OnMouseUp(Point2D worldPoint, MouseButton button);
    
    /// <summary>
    /// Handle key down event
    /// </summary>
    void OnKeyDown(Key key, ModifierKeys modifiers);
    
    /// <summary>
    /// Handle key up event
    /// </summary>
    void OnKeyUp(Key key);
    
    /// <summary>
    /// Get context menu items for right-click at point
    /// </summary>
    IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint);
    
    /// <summary>
    /// Render mode-specific overlays
    /// </summary>
    void Render(IRenderContext context);
    
    /// <summary>
    /// Event raised when mode state changes
    /// </summary>
    event EventHandler<ModeStateChangedEventArgs>? StateChanged;
    
    /// <summary>
    /// Event raised when mode completes
    /// </summary>
    event EventHandler? ModeCompleted;
}

/// <summary>
/// Event args for mode state changes
/// </summary>
public class ModeStateChangedEventArgs : EventArgs
{
    public ModeState OldState { get; }
    public ModeState NewState { get; }
    public ModeSubState OldSubState { get; }
    public ModeSubState NewSubState { get; }
    
    public ModeStateChangedEventArgs(ModeState oldState, ModeState newState, ModeSubState oldSubState, ModeSubState newSubState)
    {
        OldState = oldState;
        NewState = newState;
        OldSubState = oldSubState;
        NewSubState = newSubState;
    }
}

/// <summary>
/// Interface for rendering context
/// </summary>
public interface IRenderContext
{
    /// <summary>
    /// Gets the camera for coordinate transformations
    /// </summary>
    CAD2DModel.Camera.Camera2D Camera { get; }
    
    /// <summary>
    /// Draw a line in screen coordinates
    /// </summary>
    void DrawLine(Point2D worldStart, Point2D worldEnd, byte r, byte g, byte b, float strokeWidth = 1, bool dashed = false);
    
    /// <summary>
    /// Draw a snap indicator at the specified world point
    /// </summary>
    void DrawSnapIndicator(Point2D worldPoint, CAD2DModel.Services.SnapMode snapType);
    
    /// <summary>
    /// Draw text at world coordinates with optional rotation
    /// </summary>
    void DrawText(string text, Point2D worldPosition, float fontSize, string fontFamily, 
                  byte r, byte g, byte b, double rotationDegrees = 0, bool bold = false, 
                  bool italic = false, bool drawBackground = false, byte bgR = 255, byte bgG = 255, 
                  byte bgB = 255, byte bgA = 200);
    
    /// <summary>
    /// Draw a rectangle in world coordinates
    /// </summary>
    void DrawRectangle(Point2D worldTopLeft, Point2D worldBottomRight, byte r, byte g, byte b, 
                       float strokeWidth = 1, bool filled = false, byte fillR = 128, byte fillG = 128, 
                       byte fillB = 128, byte fillA = 100);
    
    /// <summary>
    /// Draw a rectangle in world coordinates (position and size overload)
    /// </summary>
    void DrawRectangle(Point2D worldTopLeft, double width, double height, byte r, byte g, byte b, 
                       float strokeWidth = 1, bool filled = false, byte fillR = 128, byte fillG = 128, 
                       byte fillB = 128, byte fillA = 100);
    
    /// <summary>
    /// Draw a circle in world coordinates
    /// </summary>
    void DrawCircle(Point2D worldCenter, double worldRadius, byte r, byte g, byte b, 
                    float strokeWidth = 1, bool filled = false, byte fillR = 128, byte fillG = 128, 
                    byte fillB = 128, byte fillA = 100);
    
    /// <summary>
    /// Draw an arc in world coordinates
    /// </summary>
    void DrawArc(Point2D worldCenter, double worldRadius, double startAngleDegrees, 
                 double sweepAngleDegrees, byte r, byte g, byte b, float strokeWidth = 1);
    
    /// <summary>
    /// Draw an arrow head at the end of a line
    /// </summary>
    void DrawArrowHead(Point2D worldLineStart, Point2D worldLineEnd, byte r, byte g, byte b, 
                       double arrowSize = 10.0, bool filled = true);
    
    /// <summary>
    /// Draw a control point handle at world coordinates (rendered in screen space with fixed size)
    /// </summary>
    void DrawControlPoint(Point2D worldPosition, byte r = 0, byte g = 100, byte b = 255, 
                         bool highlighted = false);
}

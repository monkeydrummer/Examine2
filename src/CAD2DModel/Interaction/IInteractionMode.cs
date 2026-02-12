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
    
    public ModeStateChangedEventArgs(ModeState oldState, ModeState newState)
    {
        OldState = oldState;
        NewState = newState;
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
}

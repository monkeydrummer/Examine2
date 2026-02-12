using CAD2DModel.Commands;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction;

/// <summary>
/// Context passed to interaction modes
/// </summary>
public class ModeContext
{
    public IGeometryModel Model { get; init; }
    public ISelectionService Selection { get; init; }
    public ISnapService SnapService { get; init; }
    public IGeometryEngine GeometryEngine { get; init; }
    public ICommandManager CommandManager { get; init; }
    public CAD2DModel.Camera.Camera2D? Camera { get; init; }
    public Dictionary<string, object> Parameters { get; init; }
    
    public ModeContext(
        ICommandManager commandManager,
        ISelectionService selection,
        IGeometryModel model)
    {
        CommandManager = commandManager;
        Selection = selection;
        Model = model;
        SnapService = null!; // To be set
        GeometryEngine = null!; // To be set
        Parameters = new Dictionary<string, object>();
    }
    
    public ModeContext(
        IGeometryModel model,
        ISelectionService selection,
        ISnapService snapService,
        IGeometryEngine geometryEngine,
        ICommandManager commandManager)
    {
        Model = model;
        Selection = selection;
        SnapService = snapService;
        GeometryEngine = geometryEngine;
        CommandManager = commandManager;
        Parameters = new Dictionary<string, object>();
    }
}

/// <summary>
/// Interface for managing interaction modes
/// </summary>
public interface IModeManager
{
    /// <summary>
    /// Current active mode
    /// </summary>
    IInteractionMode CurrentMode { get; }
    
    /// <summary>
    /// Default idle mode
    /// </summary>
    IInteractionMode IdleMode { get; }
    
    /// <summary>
    /// Camera for viewport transformations (set by view layer)
    /// </summary>
    CAD2DModel.Camera.Camera2D? Camera { get; set; }
    
    /// <summary>
    /// Enter a new mode
    /// </summary>
    void EnterMode(IInteractionMode mode);
    
    /// <summary>
    /// Exit current mode and return to idle
    /// </summary>
    void ExitCurrentMode();
    
    /// <summary>
    /// Return to idle mode
    /// </summary>
    void ReturnToIdle();
    
    /// <summary>
    /// Push a temporary mode onto the stack
    /// </summary>
    void PushMode(IInteractionMode temporaryMode);
    
    /// <summary>
    /// Pop the temporary mode and return to previous
    /// </summary>
    void PopMode();
    
    /// <summary>
    /// Event raised when mode changes
    /// </summary>
    event EventHandler<ModeChangedEventArgs>? ModeChanged;
}

/// <summary>
/// Event args for mode changes
/// </summary>
public class ModeChangedEventArgs : EventArgs
{
    public IInteractionMode? OldMode { get; }
    public IInteractionMode NewMode { get; }
    
    public ModeChangedEventArgs(IInteractionMode? oldMode, IInteractionMode newMode)
    {
        OldMode = oldMode;
        NewMode = newMode;
    }
}

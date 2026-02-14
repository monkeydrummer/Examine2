using CAD2DModel.Annotations;
using CAD2DModel.Commands;
using CAD2DModel.Services;

namespace CAD2DModel.Interaction.Implementations;

/// <summary>
/// Manages interaction modes and mode transitions
/// </summary>
public class ModeManager : IModeManager
{
    private readonly Stack<IInteractionMode> _modeStack = new();
    private readonly IInteractionMode _idleMode;
    private readonly IGeometryModel _geometryModel;
    private readonly ISelectionService _selectionService;
    private readonly ISnapService _snapService;
    private readonly IGeometryEngine _geometryEngine;
    private readonly ICommandManager _commandManager;
    private IInteractionMode _currentMode;
    
    /// <summary>
    /// Camera for viewport transformations (set by view layer)
    /// </summary>
    public Camera.Camera2D? Camera { get; set; }
    
    public ModeManager(
        ICommandManager commandManager, 
        ISelectionService selectionService, 
        ISnapService snapService,
        IGeometryEngine geometryEngine,
        IGeometryModel geometryModel)
    {
        _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        _snapService = snapService ?? throw new ArgumentNullException(nameof(snapService));
        _geometryEngine = geometryEngine ?? throw new ArgumentNullException(nameof(geometryEngine));
        _geometryModel = geometryModel ?? throw new ArgumentNullException(nameof(geometryModel));
        
        // Create idle mode
        _idleMode = new Modes.IdleMode(this, commandManager, selectionService, geometryModel, snapService);
        _currentMode = _idleMode;
        
        // Enter idle mode
        _currentMode.OnEnter(CreateModeContext());
    }
    
    public IInteractionMode CurrentMode => _currentMode;
    public IInteractionMode IdleMode => _idleMode;
    
    public event EventHandler<ModeChangedEventArgs>? ModeChanged;
    
    public void EnterMode(IInteractionMode mode)
    {
        if (mode == null)
            throw new ArgumentNullException(nameof(mode));
        
        // Don't allow entering the same mode twice
        if (_currentMode == mode)
            return;
        
        // Check if current mode can exit
        if (!_currentMode.CanExit())
            return;
        
        var previousMode = _currentMode;
        
        // Exit current mode
        _currentMode.OnExit();
        
        // Clear mode stack when entering a new mode (not pushing)
        _modeStack.Clear();
        
        // Set and enter new mode
        _currentMode = mode;
        var context = CreateModeContext();
        _currentMode.OnEnter(context);
        
        // Raise event
        OnModeChanged(previousMode, _currentMode);
    }
    
    public void ExitCurrentMode()
    {
        if (_currentMode == _idleMode)
            return; // Can't exit idle mode
        
        if (!_currentMode.CanExit())
            return;
        
        ReturnToIdle();
    }
    
    public void ReturnToIdle()
    {
        if (_currentMode == _idleMode)
            return;
        
        var previousMode = _currentMode;
        
        // Exit current mode
        _currentMode.OnExit();
        
        // Clear mode stack
        _modeStack.Clear();
        
        // Return to idle
        _currentMode = _idleMode;
        var context = CreateModeContext();
        _currentMode.OnEnter(context);
        
        // Raise event
        OnModeChanged(previousMode, _currentMode);
    }
    
    public void PushMode(IInteractionMode temporaryMode)
    {
        if (temporaryMode == null)
            throw new ArgumentNullException(nameof(temporaryMode));
        
        // Check if current mode can exit
        if (!_currentMode.CanExit())
            return;
        
        var previousMode = _currentMode;
        
        // Push current mode onto stack
        _modeStack.Push(_currentMode);
        
        // Exit current mode (but don't fully exit, just suspend)
        _currentMode.OnExit();
        
        // Enter temporary mode
        _currentMode = temporaryMode;
        var context = CreateModeContext();
        _currentMode.OnEnter(context);
        
        // Raise event
        OnModeChanged(previousMode, _currentMode);
    }
    
    public void PopMode()
    {
        if (_modeStack.Count == 0)
        {
            // No mode to pop, return to idle
            ReturnToIdle();
            return;
        }
        
        if (!_currentMode.CanExit())
            return;
        
        var previousMode = _currentMode;
        
        // Exit current mode
        _currentMode.OnExit();
        
        // Pop previous mode from stack
        _currentMode = _modeStack.Pop();
        var context = CreateModeContext();
        _currentMode.OnEnter(context);
        
        // Raise event
        OnModeChanged(previousMode, _currentMode);
    }
    
    private ModeContext CreateModeContext()
    {
        return new ModeContext(
            _geometryModel,
            _selectionService,
            _snapService,
            _geometryEngine,
            _commandManager)
        {
            Camera = Camera
        };
    }
    
    private void OnModeChanged(IInteractionMode oldMode, IInteractionMode newMode)
    {
        ModeChanged?.Invoke(this, new ModeChangedEventArgs(oldMode, newMode));
    }
    
    // Convenience methods for entering specific modes
    
    public void EnterIdleMode()
    {
        ReturnToIdle();
    }
    
    public void EnterAddRulerMode()
    {
        var mode = new Modes.AddRulerMode(this, _commandManager, _geometryModel, _snapService);
        EnterMode(mode);
    }
    
    public void EnterAddArrowMode()
    {
        var mode = new Modes.AddArrowMode(this, _commandManager, _geometryModel, _snapService);
        EnterMode(mode);
    }
    
    public void EnterAddLineMode()
    {
        var mode = new Modes.AddLineMode(this, _commandManager, _geometryModel, _snapService);
        EnterMode(mode);
    }
    
    public void EnterAddDimensionMode()
    {
        var mode = new Modes.AddDimensionMode(this, _commandManager, _geometryModel, _snapService);
        EnterMode(mode);
    }
    
    public void EnterAddAngularDimensionMode()
    {
        var mode = new Modes.AddAngularDimensionMode(this, _commandManager, _geometryModel, _snapService);
        EnterMode(mode);
    }
    
    public void EnterAddRectangleMode()
    {
        var mode = new Modes.AddRectangleMode(this, _commandManager, _geometryModel, _snapService);
        EnterMode(mode);
    }
    
    public void EnterAddCircleMode()
    {
        var mode = new Modes.AddCircleMode(this, _commandManager, _geometryModel, _snapService);
        EnterMode(mode);
    }
    
    public void EnterAddEllipseMode()
    {
        var mode = new Modes.AddEllipseMode(this, _commandManager, _geometryModel, _snapService);
        EnterMode(mode);
    }
    
    public void EnterAddTextMode(string text = "Sample Text")
    {
        var mode = new Modes.AddTextMode(this, _commandManager, _geometryModel, _snapService);
        mode.SetText(text);
        EnterMode(mode);
    }
}

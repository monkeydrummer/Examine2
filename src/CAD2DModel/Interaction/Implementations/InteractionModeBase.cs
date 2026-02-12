using CAD2DModel.Geometry;

namespace CAD2DModel.Interaction.Implementations;

/// <summary>
/// Base class for interaction modes providing common functionality
/// </summary>
public abstract class InteractionModeBase : IInteractionMode
{
    protected ModeContext? Context;
    protected ModeState State;
    protected ModeSubState SubState;
    
    public abstract string Name { get; }
    public abstract string StatusPrompt { get; }
    public abstract Cursor Cursor { get; }
    
    public ModeState CurrentState => State;
    public ModeSubState CurrentSubState => SubState;
    
    public event EventHandler<ModeStateChangedEventArgs>? StateChanged;
    public event EventHandler? ModeCompleted;
    
    protected InteractionModeBase()
    {
        State = ModeState.WaitingForInput;
        SubState = DefaultSubState.Instance;
    }
    
    public virtual void OnEnter(ModeContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        State = ModeState.Active;
        OnStateChanged(ModeState.Idle, ModeState.Active);
    }
    
    public virtual void OnExit()
    {
        var previousState = State;
        State = ModeState.Idle;
        OnStateChanged(previousState, ModeState.Idle);
        Context = null;
    }
    
    public virtual bool CanExit()
    {
        return true;
    }
    
    public virtual void OnMouseDown(Point2D worldPoint, MouseButton button, ModifierKeys modifiers)
    {
    }
    
    public virtual void OnMouseMove(Point2D worldPoint, ModifierKeys modifiers)
    {
    }
    
    public virtual void OnMouseUp(Point2D worldPoint, MouseButton button)
    {
    }
    
    public virtual void OnKeyDown(Key key, ModifierKeys modifiers)
    {
    }
    
    public virtual void OnKeyUp(Key key)
    {
    }
    
    public virtual IEnumerable<IContextMenuItem> GetContextMenuItems(Point2D worldPoint)
    {
        return Enumerable.Empty<IContextMenuItem>();
    }
    
    public virtual void Render(IRenderContext context)
    {
        // Override in derived classes to render mode-specific overlays
    }
    
    protected virtual void OnStateChanged(ModeState oldState, ModeState newState)
    {
        StateChanged?.Invoke(this, new ModeStateChangedEventArgs(oldState, newState, SubState, SubState));
    }
    
    protected virtual void OnSubStateChanged(ModeSubState oldSubState, ModeSubState newSubState)
    {
        StateChanged?.Invoke(this, new ModeStateChangedEventArgs(State, State, oldSubState, newSubState));
    }
    
    protected virtual void OnStateAndSubStateChanged(ModeState oldState, ModeState newState, ModeSubState oldSubState, ModeSubState newSubState)
    {
        StateChanged?.Invoke(this, new ModeStateChangedEventArgs(oldState, newState, oldSubState, newSubState));
    }
    
    protected virtual void CompleteMod()
    {
        ModeCompleted?.Invoke(this, EventArgs.Empty);
    }
}

namespace CAD2DModel.Interaction;

/// <summary>
/// Base class for mode substates - allows modes to have fine-grained state management
/// Each mode can define its own substate class to represent different phases of operation
/// </summary>
public abstract class ModeSubState
{
    /// <summary>
    /// Gets the name of this substate
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// Gets a description of this substate for display purposes
    /// </summary>
    public virtual string Description => Name;
    
    public override string ToString() => Name;
    
    public override bool Equals(object? obj)
    {
        if (obj is ModeSubState other)
        {
            return GetType() == other.GetType() && Name == other.Name;
        }
        return false;
    }
    
    public override int GetHashCode() => HashCode.Combine(GetType(), Name);
    
    public static bool operator ==(ModeSubState? left, ModeSubState? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }
    
    public static bool operator !=(ModeSubState? left, ModeSubState? right) => !(left == right);
}

/// <summary>
/// Default substate for modes that don't need fine-grained state management
/// </summary>
public sealed class DefaultSubState : ModeSubState
{
    public static readonly DefaultSubState Instance = new DefaultSubState();
    
    private DefaultSubState() { }
    
    public override string Name => "Default";
}

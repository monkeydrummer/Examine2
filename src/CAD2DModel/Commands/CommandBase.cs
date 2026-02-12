using CAD2DModel.Commands;

namespace CAD2DModel.Commands;

/// <summary>
/// Base abstract command class
/// </summary>
public abstract class CommandBase : ICommand
{
    public string Description { get; protected set; }
    
    protected CommandBase(string description)
    {
        Description = description;
    }
    
    public abstract void Execute();
    public abstract void Undo();
    
    public virtual bool CanExecute()
    {
        return true;
    }
}

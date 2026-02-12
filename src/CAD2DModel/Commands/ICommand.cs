namespace CAD2DModel.Commands;

/// <summary>
/// Interface for undoable commands
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Description of the command for display in undo/redo UI
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Execute the command
    /// </summary>
    void Execute();
    
    /// <summary>
    /// Undo the command
    /// </summary>
    void Undo();
    
    /// <summary>
    /// Check if the command can be executed
    /// </summary>
    bool CanExecute();
}

/// <summary>
/// Interface for managing command execution and undo/redo
/// </summary>
public interface ICommandManager
{
    /// <summary>
    /// Execute a command and add it to undo stack
    /// </summary>
    void Execute(ICommand command);
    
    /// <summary>
    /// Undo the last command
    /// </summary>
    void Undo();
    
    /// <summary>
    /// Redo the last undone command
    /// </summary>
    void Redo();
    
    /// <summary>
    /// Check if undo is available
    /// </summary>
    bool CanUndo { get; }
    
    /// <summary>
    /// Check if redo is available
    /// </summary>
    bool CanRedo { get; }
    
    /// <summary>
    /// Get the description of the next undo command
    /// </summary>
    string? UndoDescription { get; }
    
    /// <summary>
    /// Get the description of the next redo command
    /// </summary>
    string? RedoDescription { get; }
    
    /// <summary>
    /// Clear all undo/redo history
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Event raised when a command is executed
    /// </summary>
    event EventHandler<CommandEventArgs>? CommandExecuted;
    
    /// <summary>
    /// Event raised when undo/redo state changes
    /// </summary>
    event EventHandler? StateChanged;
}

/// <summary>
/// Event args for command execution
/// </summary>
public class CommandEventArgs : EventArgs
{
    public ICommand Command { get; }
    
    public CommandEventArgs(ICommand command)
    {
        Command = command;
    }
}

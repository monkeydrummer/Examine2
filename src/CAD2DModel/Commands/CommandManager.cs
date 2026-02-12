using System.Collections.Generic;

namespace CAD2DModel.Commands;

/// <summary>
/// Command manager implementation for undo/redo functionality
/// </summary>
public class CommandManager : ICommandManager
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();
    private readonly int _maxUndoLevels;
    
    public CommandManager(int maxUndoLevels = 100)
    {
        _maxUndoLevels = maxUndoLevels;
    }
    
    public bool CanUndo => _undoStack.Count > 0;
    
    public bool CanRedo => _redoStack.Count > 0;
    
    public string? UndoDescription => CanUndo ? _undoStack.Peek().Description : null;
    
    public string? RedoDescription => CanRedo ? _redoStack.Peek().Description : null;
    
    public event EventHandler<CommandEventArgs>? CommandExecuted;
    public event EventHandler? StateChanged;
    
    public void Execute(ICommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));
        
        if (!command.CanExecute())
            throw new InvalidOperationException($"Command '{command.Description}' cannot be executed");
        
        command.Execute();
        
        _undoStack.Push(command);
        _redoStack.Clear();
        
        // Trim undo stack if it exceeds max levels
        if (_undoStack.Count > _maxUndoLevels)
        {
            var temp = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < _maxUndoLevels; i++)
            {
                _undoStack.Push(temp[i]);
            }
        }
        
        OnCommandExecuted(new CommandEventArgs(command));
        OnStateChanged();
    }
    
    public void Undo()
    {
        if (!CanUndo)
            throw new InvalidOperationException("No commands to undo");
        
        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        
        OnStateChanged();
    }
    
    public void Redo()
    {
        if (!CanRedo)
            throw new InvalidOperationException("No commands to redo");
        
        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        
        OnCommandExecuted(new CommandEventArgs(command));
        OnStateChanged();
    }
    
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        OnStateChanged();
    }
    
    protected virtual void OnCommandExecuted(CommandEventArgs e)
    {
        CommandExecuted?.Invoke(this, e);
    }
    
    protected virtual void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}

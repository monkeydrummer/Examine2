using CAD2DModel.Geometry;

namespace CAD2DModel.Commands.Implementations;

/// <summary>
/// Command to add a vertex to a polyline
/// </summary>
public class AddVertexCommand : CommandBase
{
    private readonly Polyline _polyline;
    private readonly Point2D _location;
    private readonly int _index;
    private Vertex? _addedVertex;
    
    public AddVertexCommand(Polyline polyline, Point2D location, int index = -1)
        : base($"Add vertex at {location}")
    {
        _polyline = polyline ?? throw new ArgumentNullException(nameof(polyline));
        _location = location;
        _index = index < 0 ? polyline.VertexCount : index;
    }
    
    public override void Execute()
    {
        _addedVertex = new Vertex(_location);
        
        if (_index >= _polyline.VertexCount)
        {
            _polyline.Vertices.Add(_addedVertex);
        }
        else
        {
            _polyline.Vertices.Insert(_index, _addedVertex);
        }
    }
    
    public override void Undo()
    {
        if (_addedVertex != null)
        {
            _polyline.Vertices.Remove(_addedVertex);
        }
    }
}

/// <summary>
/// Command to remove a vertex from a polyline
/// </summary>
public class RemoveVertexCommand : CommandBase
{
    private readonly Polyline _polyline;
    private readonly Vertex _vertex;
    private int _originalIndex;
    
    public RemoveVertexCommand(Polyline polyline, Vertex vertex)
        : base($"Remove vertex at {vertex.Location}")
    {
        _polyline = polyline ?? throw new ArgumentNullException(nameof(polyline));
        _vertex = vertex ?? throw new ArgumentNullException(nameof(vertex));
    }
    
    public override void Execute()
    {
        _originalIndex = _polyline.Vertices.IndexOf(_vertex);
        _polyline.Vertices.Remove(_vertex);
    }
    
    public override void Undo()
    {
        _polyline.Vertices.Insert(_originalIndex, _vertex);
    }
    
    public override bool CanExecute()
    {
        return _polyline.Vertices.Contains(_vertex);
    }
}

/// <summary>
/// Command to move a vertex
/// </summary>
public class MoveVertexCommand : CommandBase
{
    private readonly Vertex _vertex;
    private readonly Point2D _newLocation;
    private readonly Point2D _oldLocation;
    
    public MoveVertexCommand(Vertex vertex, Point2D newLocation)
        : base($"Move vertex to {newLocation}")
    {
        _vertex = vertex ?? throw new ArgumentNullException(nameof(vertex));
        _oldLocation = vertex.Location;
        _newLocation = newLocation;
    }
    
    public override void Execute()
    {
        _vertex.Location = _newLocation;
    }
    
    public override void Undo()
    {
        _vertex.Location = _oldLocation;
    }
}

/// <summary>
/// Command to add a polyline to the model
/// </summary>
public class AddPolylineCommand : CommandBase
{
    private readonly Services.IGeometryModel _model;
    private readonly Polyline _polyline;
    
    public AddPolylineCommand(Services.IGeometryModel model, Polyline polyline)
        : base($"Add polyline '{polyline.Name}'")
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _polyline = polyline ?? throw new ArgumentNullException(nameof(polyline));
    }
    
    public override void Execute()
    {
        _model.AddEntity(_polyline);
    }
    
    public override void Undo()
    {
        _model.RemoveEntity(_polyline);
    }
}

/// <summary>
/// Command to remove a polyline from the model
/// </summary>
public class RemovePolylineCommand : CommandBase
{
    private readonly Services.IGeometryModel _model;
    private readonly Polyline _polyline;
    
    public RemovePolylineCommand(Services.IGeometryModel model, Polyline polyline)
        : base($"Remove polyline '{polyline.Name}'")
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _polyline = polyline ?? throw new ArgumentNullException(nameof(polyline));
    }
    
    public override void Execute()
    {
        _model.RemoveEntity(_polyline);
    }
    
    public override void Undo()
    {
        _model.AddEntity(_polyline);
    }
}

/// <summary>
/// Composite command that executes multiple commands as one unit
/// </summary>
public class CompositeCommand : CommandBase
{
    private readonly List<ICommand> _commands = new();
    
    public CompositeCommand(string description) : base(description)
    {
    }
    
    public void AddCommand(ICommand command)
    {
        _commands.Add(command);
    }
    
    public override void Execute()
    {
        foreach (var command in _commands)
        {
            command.Execute();
        }
    }
    
    public override void Undo()
    {
        // Undo in reverse order
        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
    
    public override bool CanExecute()
    {
        return _commands.All(c => c.CanExecute());
    }
}

/// <summary>
/// Command to modify a property value
/// </summary>
public class PropertyChangeCommand<T> : CommandBase
{
    private readonly Action<T> _setter;
    private readonly T _newValue;
    private readonly T _oldValue;
    
    public PropertyChangeCommand(string description, Action<T> setter, T oldValue, T newValue)
        : base(description)
    {
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        _oldValue = oldValue;
        _newValue = newValue;
    }
    
    public override void Execute()
    {
        _setter(_newValue);
    }
    
    public override void Undo()
    {
        _setter(_oldValue);
    }
}

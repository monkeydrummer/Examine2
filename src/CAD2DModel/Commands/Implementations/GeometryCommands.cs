using CAD2DModel.Geometry;
using CAD2DModel.Services;
using System.Collections.Generic;

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
    private readonly IEntity? _parentEntity;
    private readonly IGeometryModel? _model;
    
    public MoveVertexCommand(Vertex vertex, Point2D newLocation, IEntity? parentEntity = null, IGeometryModel? model = null)
        : base($"Move vertex to {newLocation}")
    {
        _vertex = vertex ?? throw new ArgumentNullException(nameof(vertex));
        _oldLocation = vertex.Location;
        _newLocation = newLocation;
        _parentEntity = parentEntity;
        _model = model;
    }
    
    public override void Execute()
    {
        _vertex.Location = _newLocation;
        
        // Apply rules to the parent entity if available
        if (_parentEntity != null && _model != null)
        {
            _model.ApplyRulesToEntity(_parentEntity);
        }
    }
    
    public override void Undo()
    {
        _vertex.Location = _oldLocation;
        
        // Apply rules to the parent entity if available
        if (_parentEntity != null && _model != null)
        {
            _model.ApplyRulesToEntity(_parentEntity);
        }
    }
}

/// <summary>
/// Command to add any entity to the model
/// </summary>
public class AddEntityCommand : CommandBase
{
    private readonly IGeometryModel _model;
    private readonly IEntity _entity;
    
    public AddEntityCommand(IGeometryModel model, IEntity entity)
        : base($"Add {GetEntityTypeName(entity)} '{entity.Name}'")
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }
    
    public override void Execute()
    {
        _model.AddEntity(_entity);
    }
    
    public override void Undo()
    {
        _model.RemoveEntity(_entity);
    }
    
    private static string GetEntityTypeName(IEntity entity)
    {
        return entity switch
        {
            Boundary => "boundary",
            Polyline => "polyline",
            _ => "entity"
        };
    }
}

/// <summary>
/// Command to add a polyline to the model
/// </summary>
public class AddPolylineCommand : CommandBase
{
    private readonly IGeometryModel _model;
    private readonly Polyline _polyline;
    
    public AddPolylineCommand(IGeometryModel model, Polyline polyline)
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
/// Command to add a boundary to the model
/// </summary>
public class AddBoundaryCommand : CommandBase
{
    private readonly IGeometryModel _model;
    private readonly Boundary _boundary;
    
    public AddBoundaryCommand(IGeometryModel model, Boundary boundary)
        : base($"Add boundary '{boundary.Name}'")
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
    }
    
    public override void Execute()
    {
        _model.AddEntity(_boundary);
    }
    
    public override void Undo()
    {
        _model.RemoveEntity(_boundary);
    }
}

/// <summary>
/// Command to delete any entity from the model
/// </summary>
public class DeleteEntityCommand : CommandBase
{
    private readonly IGeometryModel _model;
    private readonly IEntity _entity;
    
    public DeleteEntityCommand(IGeometryModel model, IEntity entity)
        : base($"Delete {GetEntityTypeName(entity)} '{entity.Name}'")
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }
    
    public override void Execute()
    {
        _model.RemoveEntity(_entity);
    }
    
    public override void Undo()
    {
        _model.AddEntity(_entity);
    }
    
    private static string GetEntityTypeName(IEntity entity)
    {
        return entity switch
        {
            Boundary => "boundary",
            Polyline => "polyline",
            _ => "entity"
        };
    }
}

/// <summary>
/// Command to delete multiple entities from the model
/// </summary>
public class DeleteMultipleEntitiesCommand : CommandBase
{
    private readonly IGeometryModel _model;
    private readonly List<IEntity> _entities;
    
    public DeleteMultipleEntitiesCommand(IGeometryModel model, IEnumerable<IEntity> entities)
        : base($"Delete {entities.Count()} entities")
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _entities = entities?.ToList() ?? throw new ArgumentNullException(nameof(entities));
        
        if (_entities.Count == 0)
            throw new ArgumentException("Must provide at least one entity to delete", nameof(entities));
    }
    
    public override void Execute()
    {
        foreach (var entity in _entities)
        {
            _model.RemoveEntity(entity);
        }
    }
    
    public override void Undo()
    {
        // Add back in reverse order to maintain original order
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            _model.AddEntity(_entities[i]);
        }
    }
}

/// <summary>
/// Command to remove a polyline from the model
/// </summary>
public class RemovePolylineCommand : CommandBase
{
    private readonly IGeometryModel _model;
    private readonly Polyline _polyline;
    
    public RemovePolylineCommand(IGeometryModel model, Polyline polyline)
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
/// Command to delete a vertex from a polyline or boundary
/// </summary>
public class DeleteVertexCommand : CommandBase
{
    private readonly IEntity _entity;
    private readonly int _vertexIndex;
    private readonly Vertex _deletedVertex;
    
    public DeleteVertexCommand(IEntity entity, int vertexIndex)
        : base($"Delete vertex at index {vertexIndex}")
    {
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        _vertexIndex = vertexIndex;
        
        // Store the vertex before deletion
        if (entity is Polyline polyline)
        {
            _deletedVertex = polyline.Vertices[vertexIndex];
        }
        else if (entity is Boundary boundary)
        {
            _deletedVertex = boundary.Vertices[vertexIndex];
        }
        else
        {
            throw new ArgumentException("Entity must be a Polyline or Boundary", nameof(entity));
        }
    }
    
    public override void Execute()
    {
        if (_entity is Polyline polyline)
        {
            polyline.RemoveVertexAt(_vertexIndex);
        }
        else if (_entity is Boundary boundary)
        {
            boundary.RemoveVertexAt(_vertexIndex);
        }
    }
    
    public override void Undo()
    {
        if (_entity is Polyline polyline)
        {
            polyline.Vertices.Insert(_vertexIndex, _deletedVertex);
        }
        else if (_entity is Boundary boundary)
        {
            boundary.Vertices.Insert(_vertexIndex, _deletedVertex);
        }
    }
    
    public override bool CanExecute()
    {
        if (_entity is Polyline polyline)
        {
            return polyline.Vertices.Count > 2; // Minimum 2 vertices for polyline
        }
        else if (_entity is Boundary boundary)
        {
            return boundary.Vertices.Count > 3; // Minimum 3 vertices for boundary
        }
        return false;
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

/// <summary>
/// Command to trim a polyline or boundary at intersection points
/// </summary>
public class TrimCommand : CommandBase
{
    private readonly IGeometryModel _model;
    private readonly IEntity _entityToTrim;
    private readonly IEntity _originalEntity;
    private readonly List<Point2D> _trimPoints;
    private readonly IEntity? _resultEntity;
    private readonly bool _wasRemoved;

    public TrimCommand(
        IGeometryModel model,
        IEntity entityToTrim,
        List<Point2D> trimPoints,
        IEntity? resultEntity = null)
        : base($"Trim {GetEntityTypeName(entityToTrim)}")
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _entityToTrim = entityToTrim ?? throw new ArgumentNullException(nameof(entityToTrim));
        _trimPoints = new List<Point2D>(trimPoints);
        _resultEntity = resultEntity;
        
        // Store original entity for undo
        if (entityToTrim is Polyline p)
        {
            _originalEntity = new Polyline(p.Vertices.Select(v => v.Location));
        }
        else if (entityToTrim is Boundary b)
        {
            _originalEntity = new Boundary(b.Vertices.Select(v => v.Location));
        }
        else
        {
            throw new ArgumentException("Unsupported entity type");
        }
        
        _wasRemoved = resultEntity == null;
    }

    public override void Execute()
    {
        if (_wasRemoved)
        {
            // Entity was completely trimmed away
            _model.Entities.Remove(_entityToTrim);
        }
        else if (_resultEntity != null)
        {
            // Replace with trimmed entity
            int index = _model.Entities.IndexOf(_entityToTrim);
            if (index >= 0)
            {
                _model.Entities.RemoveAt(index);
                _model.Entities.Insert(index, _resultEntity);
            }
        }
    }

    public override void Undo()
    {
        if (_wasRemoved)
        {
            // Restore removed entity
            _model.Entities.Add(_originalEntity);
        }
        else if (_resultEntity != null)
        {
            // Restore original entity
            int index = _model.Entities.IndexOf(_resultEntity);
            if (index >= 0)
            {
                _model.Entities.RemoveAt(index);
                _model.Entities.Insert(index, _originalEntity);
            }
        }
    }

    private static string GetEntityTypeName(IEntity entity)
    {
        return entity switch
        {
            Boundary => "boundary",
            Polyline => "polyline",
            _ => "entity"
        };
    }
}

/// <summary>
/// Command to extend a polyline to meet a boundary entity
/// </summary>
public class ExtendCommand : CommandBase
{
    private readonly IGeometryModel _model;
    private readonly IEntity _entityToExtend;
    private readonly IEntity _originalEntity;
    private readonly Point2D _extensionPoint;
    private readonly IEntity _extendedEntity;

    public ExtendCommand(
        IGeometryModel model,
        IEntity entityToExtend,
        Point2D extensionPoint,
        IEntity extendedEntity)
        : base($"Extend {GetEntityTypeName(entityToExtend)}")
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _entityToExtend = entityToExtend ?? throw new ArgumentNullException(nameof(entityToExtend));
        _extensionPoint = extensionPoint;
        _extendedEntity = extendedEntity ?? throw new ArgumentNullException(nameof(extendedEntity));
        
        // Store original entity for undo
        if (entityToExtend is Polyline p)
        {
            _originalEntity = new Polyline(p.Vertices.Select(v => v.Location));
        }
        else if (entityToExtend is Boundary b)
        {
            _originalEntity = new Boundary(b.Vertices.Select(v => v.Location));
        }
        else
        {
            throw new ArgumentException("Unsupported entity type");
        }
    }

    public override void Execute()
    {
        // Replace with extended entity
        int index = _model.Entities.IndexOf(_entityToExtend);
        if (index >= 0)
        {
            _model.Entities.RemoveAt(index);
            _model.Entities.Insert(index, _extendedEntity);
        }
    }

    public override void Undo()
    {
        // Restore original entity
        int index = _model.Entities.IndexOf(_extendedEntity);
        if (index >= 0)
        {
            _model.Entities.RemoveAt(index);
            _model.Entities.Insert(index, _originalEntity);
        }
    }

    private static string GetEntityTypeName(IEntity entity)
    {
        return entity switch
        {
            Boundary => "boundary",
            Polyline => "polyline",
            _ => "entity"
        };
    }
}

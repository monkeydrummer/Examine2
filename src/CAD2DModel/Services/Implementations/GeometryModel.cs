using CAD2DModel.Geometry;
using CAD2DModel.Services;
using System.Collections.ObjectModel;

namespace CAD2DModel.Services.Implementations;

/// <summary>
/// Simple geometry model implementation
/// </summary>
public class GeometryModel : IGeometryModel
{
    private readonly ObservableCollection<IEntity> _entities = new();
    
    public ObservableCollection<IEntity> Entities => _entities;
    
    public void AddEntity(IEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        
        if (!_entities.Contains(entity))
        {
            _entities.Add(entity);
        }
    }
    
    public void RemoveEntity(IEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        
        _entities.Remove(entity);
    }
    
    public IEntity? FindEntity(Guid id)
    {
        return _entities.FirstOrDefault(e => e.Id == id);
    }
}

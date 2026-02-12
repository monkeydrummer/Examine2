using CAD2DModel.Geometry;
using CAD2DModel.Services;
using System.Collections.ObjectModel;

namespace CAD2DModel.Services.Implementations;

/// <summary>
/// Simple geometry model implementation with automatic rule application
/// </summary>
public class GeometryModel : IGeometryModel
{
    private readonly ObservableCollection<IEntity> _entities = new();
    private IGeometryRuleEngine? _ruleEngine;
    
    public ObservableCollection<IEntity> Entities => _entities;
    
    /// <summary>
    /// Set the rule engine for automatic rule application
    /// This is set by DI after construction
    /// </summary>
    public void SetRuleEngine(IGeometryRuleEngine ruleEngine)
    {
        _ruleEngine = ruleEngine;
    }
    
    public void AddEntity(IEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        
        if (!_entities.Contains(entity))
        {
            _entities.Add(entity);
            
            // Rules are NOT automatically applied here.
            // Call ApplyRulesToEntity() or ApplyAllRules() explicitly when needed.
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
    
    /// <summary>
    /// Apply all geometry rules to all entities in the model
    /// Call this after batch operations or loading from file
    /// </summary>
    public void ApplyAllRules()
    {
        _ruleEngine?.ApplyAllRules(this);
    }
    
    /// <summary>
    /// Apply rules to a specific entity
    /// Call this after modifying an entity (e.g., moving vertices)
    /// </summary>
    public void ApplyRulesToEntity(IEntity entity)
    {
        _ruleEngine?.ApplyRules(entity, this);
    }
}

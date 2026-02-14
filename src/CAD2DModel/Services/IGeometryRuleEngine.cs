using CAD2DModel.Geometry;
using CAD2DModel.Annotations;

namespace CAD2DModel.Services;

/// <summary>
/// Interface for a single geometry validation/correction rule
/// </summary>
public interface IGeometryRule
{
    /// <summary>
    /// Name of the rule
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Priority for rule execution (lower executes first)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Check if the rule applies to the given entity
    /// </summary>
    bool AppliesTo(IEntity entity);
    
    /// <summary>
    /// Apply the rule to the entity and model
    /// </summary>
    void Apply(IEntity entity, IGeometryModel model);
}

/// <summary>
/// Engine that applies geometry rules when geometry changes
/// </summary>
public interface IGeometryRuleEngine
{
    /// <summary>
    /// Register a rule with the engine
    /// </summary>
    void RegisterRule(IGeometryRule rule);
    
    /// <summary>
    /// Unregister a rule
    /// </summary>
    void UnregisterRule(IGeometryRule rule);
    
    /// <summary>
    /// Apply all applicable rules to an entity
    /// </summary>
    void ApplyRules(IEntity entity, IGeometryModel model);
    
    /// <summary>
    /// Apply all rules to all entities in the model
    /// </summary>
    void ApplyAllRules(IGeometryModel model);
    
    /// <summary>
    /// Enable or disable the rule engine
    /// </summary>
    bool Enabled { get; set; }
}

/// <summary>
/// Interface for the geometry model
/// </summary>
public interface IGeometryModel
{
    System.Collections.ObjectModel.ObservableCollection<IEntity> Entities { get; }
    System.Collections.ObjectModel.ObservableCollection<IAnnotation> Annotations { get; }
    void AddEntity(IEntity entity);
    void RemoveEntity(IEntity entity);
    IEntity? FindEntity(Guid id);
    
    /// <summary>
    /// Set the rule engine (called by DI container)
    /// </summary>
    void SetRuleEngine(IGeometryRuleEngine ruleEngine);
    
    /// <summary>
    /// Apply all geometry rules to all entities
    /// </summary>
    void ApplyAllRules();
    
    /// <summary>
    /// Apply rules to a specific entity
    /// </summary>
    void ApplyRulesToEntity(IEntity entity);
    
    /// <summary>
    /// Event raised when geometry changes (but collection doesn't change)
    /// Used for live preview updates during moves
    /// </summary>
    event EventHandler? GeometryChanged;
    
    /// <summary>
    /// Notify that geometry has changed (for live preview updates)
    /// </summary>
    void NotifyGeometryChanged();
}

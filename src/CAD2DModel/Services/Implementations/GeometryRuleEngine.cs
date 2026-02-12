using CAD2DModel.Geometry;

namespace CAD2DModel.Services.Implementations;

/// <summary>
/// Engine that manages and applies geometry validation/correction rules
/// </summary>
public class GeometryRuleEngine : IGeometryRuleEngine
{
    private readonly List<IGeometryRule> _rules;
    private bool _enabled;
    
    public GeometryRuleEngine()
    {
        _rules = new List<IGeometryRule>();
        _enabled = true;
    }
    
    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }
    
    public void RegisterRule(IGeometryRule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));
        
        if (_rules.Contains(rule))
            return;
        
        _rules.Add(rule);
        
        // Sort by priority after adding
        _rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }
    
    public void UnregisterRule(IGeometryRule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));
        
        _rules.Remove(rule);
    }
    
    public void ApplyRules(IEntity entity, IGeometryModel model)
    {
        if (!_enabled || entity == null || model == null)
            return;
        
        // Apply all applicable rules to the entity
        foreach (var rule in _rules)
        {
            if (rule.AppliesTo(entity))
            {
                rule.Apply(entity, model);
            }
        }
    }
    
    public void ApplyAllRules(IGeometryModel model)
    {
        if (!_enabled || model == null)
            return;
        
        // Apply rules to all entities
        foreach (var entity in model.Entities.ToList()) // ToList to avoid modification during iteration
        {
            ApplyRules(entity, model);
        }
    }
    
    public IReadOnlyList<IGeometryRule> GetRegisteredRules()
    {
        return _rules.AsReadOnly();
    }
}

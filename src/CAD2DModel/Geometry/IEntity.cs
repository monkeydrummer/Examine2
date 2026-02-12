namespace CAD2DModel.Geometry;

/// <summary>
/// Base interface for all geometric entities in the model
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    Guid Id { get; set; }
    
    /// <summary>
    /// Display name of the entity
    /// </summary>
    string Name { get; set; }
    
    /// <summary>
    /// Visibility state of the entity
    /// </summary>
    bool IsVisible { get; set; }
}

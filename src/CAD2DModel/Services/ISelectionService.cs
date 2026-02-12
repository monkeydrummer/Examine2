using CAD2DModel.Geometry;

namespace CAD2DModel.Services;

/// <summary>
/// Interface for entity selection operations
/// </summary>
public interface ISelectionService
{
    /// <summary>
    /// Currently selected entities
    /// </summary>
    IReadOnlyCollection<IEntity> SelectedEntities { get; }
    
    /// <summary>
    /// Hit test to find entity at point
    /// </summary>
    IEntity? HitTest(Point2D point, double tolerance, IEnumerable<IEntity> entities);
    
    /// <summary>
    /// Select entities within a rectangular region
    /// </summary>
    IEnumerable<IEntity> SelectInBox(Rect2D box, IEnumerable<IEntity> entities, bool crossing = false);
    
    /// <summary>
    /// Select a single entity
    /// </summary>
    void Select(IEntity entity, bool addToSelection = false);
    
    /// <summary>
    /// Select multiple entities
    /// </summary>
    void Select(IEnumerable<IEntity> entities, bool addToSelection = false);
    
    /// <summary>
    /// Deselect an entity
    /// </summary>
    void Deselect(IEntity entity);
    
    /// <summary>
    /// Clear all selections
    /// </summary>
    void ClearSelection();
    
    /// <summary>
    /// Toggle selection state of an entity
    /// </summary>
    void ToggleSelection(IEntity entity);
    
    /// <summary>
    /// Event raised when selection changes
    /// </summary>
    event EventHandler SelectionChanged;
}

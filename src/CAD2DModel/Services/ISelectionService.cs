using CAD2DModel.Geometry;
using CAD2DModel.Selection;

namespace CAD2DModel.Services;

/// <summary>
/// Interface for entity and vertex selection operations
/// </summary>
public interface ISelectionService
{
    /// <summary>
    /// Currently selected entities
    /// </summary>
    IReadOnlyCollection<IEntity> SelectedEntities { get; }
    
    /// <summary>
    /// Currently selected vertices
    /// </summary>
    IReadOnlyCollection<VertexHandle> SelectedVertices { get; }
    
    /// <summary>
    /// Hit test to find entity at point
    /// </summary>
    IEntity? HitTest(Point2D point, double tolerance, IEnumerable<IEntity> entities);
    
    /// <summary>
    /// Hit test to find vertex at point
    /// </summary>
    VertexHandle? HitTestVertex(Point2D point, double tolerance, IEnumerable<IEntity> entities);
    
    /// <summary>
    /// Select entities within a rectangular region
    /// </summary>
    IEnumerable<IEntity> SelectInBox(Rect2D box, IEnumerable<IEntity> entities, bool crossing = false);
    
    /// <summary>
    /// Select vertices within a rectangular region
    /// </summary>
    IEnumerable<VertexHandle> SelectVerticesInBox(Rect2D box, IEnumerable<IEntity> entities);
    
    /// <summary>
    /// Select a single entity
    /// </summary>
    void Select(IEntity entity, bool addToSelection = false);
    
    /// <summary>
    /// Select multiple entities
    /// </summary>
    void Select(IEnumerable<IEntity> entities, bool addToSelection = false);
    
    /// <summary>
    /// Select a single vertex
    /// </summary>
    void SelectVertex(VertexHandle vertex, bool addToSelection = false);
    
    /// <summary>
    /// Select multiple vertices
    /// </summary>
    void SelectVertices(IEnumerable<VertexHandle> vertices, bool addToSelection = false);
    
    /// <summary>
    /// Deselect an entity
    /// </summary>
    void Deselect(IEntity entity);
    
    /// <summary>
    /// Deselect a vertex
    /// </summary>
    void DeselectVertex(VertexHandle vertex);
    
    /// <summary>
    /// Clear all entity selections
    /// </summary>
    void ClearSelection();
    
    /// <summary>
    /// Clear all vertex selections
    /// </summary>
    void ClearVertexSelection();
    
    /// <summary>
    /// Clear both entity and vertex selections
    /// </summary>
    void ClearAllSelections();
    
    /// <summary>
    /// Toggle selection state of an entity
    /// </summary>
    void ToggleSelection(IEntity entity);
    
    /// <summary>
    /// Toggle selection state of a vertex
    /// </summary>
    void ToggleVertexSelection(VertexHandle vertex);
    
    /// <summary>
    /// Event raised when entity selection changes
    /// </summary>
    event EventHandler SelectionChanged;
    
    /// <summary>
    /// Event raised when vertex selection changes
    /// </summary>
    event EventHandler VertexSelectionChanged;
}

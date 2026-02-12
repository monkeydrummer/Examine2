using CAD2DModel.Geometry;

namespace CAD2DModel.Services.Implementations.Rules;

/// <summary>
/// Rule that ensures polylines have at least 2 vertices and boundaries have at least 3 vertices
/// Removes invalid entities from the model
/// </summary>
public class MinimumVertexCountRule : IGeometryRule
{
    public string Name => "Minimum Vertex Count";
    public int Priority => 10; // Run very early
    
    public bool AppliesTo(IEntity entity)
    {
        return entity is Polyline || entity is Boundary;
    }
    
    public void Apply(IEntity entity, IGeometryModel model)
    {
        bool shouldRemove = false;
        
        if (entity is Polyline polyline && polyline.Vertices.Count < 2)
        {
            shouldRemove = true;
        }
        else if (entity is Boundary boundary && boundary.Vertices.Count < 3)
        {
            shouldRemove = true;
        }
        
        if (shouldRemove)
        {
            model.RemoveEntity(entity);
        }
    }
}

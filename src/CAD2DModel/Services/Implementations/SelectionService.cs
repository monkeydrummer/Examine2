using System.Collections.ObjectModel;
using CAD2DModel.Geometry;

namespace CAD2DModel.Services.Implementations;

/// <summary>
/// Manages entity selection with hit testing and selection boxes
/// </summary>
public class SelectionService : ISelectionService
{
    private readonly ObservableCollection<IEntity> _selectedEntities = new();
    private readonly IGeometryEngine _geometryEngine;
    
    public SelectionService(IGeometryEngine geometryEngine)
    {
        _geometryEngine = geometryEngine ?? throw new ArgumentNullException(nameof(geometryEngine));
    }
    
    public IReadOnlyCollection<IEntity> SelectedEntities => _selectedEntities;
    
    public event EventHandler? SelectionChanged;
    
    public IEntity? HitTest(Point2D point, double tolerance, IEnumerable<IEntity> entities)
    {
        var toleranceSquared = tolerance * tolerance;
        
        foreach (var entity in entities)
        {
            if (!entity.IsVisible)
                continue;
            
            if (entity is Polyline polyline)
            {
                // Check if point is near any vertex
                foreach (var vertex in polyline.Vertices)
                {
                    var distSquared = point.DistanceSquaredTo(vertex.Location);
                    if (distSquared <= toleranceSquared)
                        return polyline;
                }
                
                // Check if point is near any segment
                for (int i = 0; i < polyline.GetSegmentCount(); i++)
                {
                    var segment = polyline.GetSegment(i);
                    var distance = _geometryEngine.DistanceToLineSegment(point, segment);
                    if (distance <= tolerance)
                        return polyline;
                }
            }
            else if (entity is Boundary boundary)
            {
                // Check if point is near any vertex
                foreach (var vertex in boundary.Vertices)
                {
                    var distSquared = point.DistanceSquaredTo(vertex.Location);
                    if (distSquared <= toleranceSquared)
                        return boundary;
                }
                
                // Check if point is near any segment
                for (int i = 0; i < boundary.GetSegmentCount(); i++)
                {
                    var segment = boundary.GetSegment(i);
                    var distance = _geometryEngine.DistanceToLineSegment(point, segment);
                    if (distance <= tolerance)
                        return boundary;
                }
                
                // Check if point is inside the boundary
                if (_geometryEngine.IsPointInside(point, boundary))
                    return boundary;
            }
        }
        
        return null;
    }
    
    public IEnumerable<IEntity> SelectInBox(Rect2D box, IEnumerable<IEntity> entities, bool entirelyInside = false)
    {
        var selected = new List<IEntity>();
        
        foreach (var entity in entities)
        {
            if (!entity.IsVisible)
                continue;
            
            if (entity is Polyline polyline)
            {
                bool intersects = false;
                bool allInside = true;
                
                foreach (var vertex in polyline.Vertices)
                {
                    bool vertexInside = box.Contains(vertex.Location);
                    if (vertexInside)
                        intersects = true;
                    if (!vertexInside)
                        allInside = false;
                }
                
                // Check if any segment intersects the box
                if (!intersects)
                {
                    for (int i = 0; i < polyline.GetSegmentCount(); i++)
                    {
                        var segment = polyline.GetSegment(i);
                        if (SegmentIntersectsBox(segment, box))
                        {
                            intersects = true;
                            allInside = false;
                            break;
                        }
                    }
                }
                
                if (entirelyInside)
                {
                    if (allInside && polyline.Vertices.Count > 0)
                        selected.Add(polyline);
                }
                else
                {
                    if (intersects)
                        selected.Add(polyline);
                }
            }
        }
        
        return selected;
    }
    
    public void Select(IEntity entity, bool addToSelection = false)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        
        if (!addToSelection)
        {
            _selectedEntities.Clear();
        }
        
        if (!_selectedEntities.Contains(entity))
        {
            _selectedEntities.Add(entity);
        }
        
        OnSelectionChanged();
    }
    
    public void Select(IEnumerable<IEntity> entities, bool addToSelection = false)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));
        
        if (!addToSelection)
        {
            _selectedEntities.Clear();
        }
        
        foreach (var entity in entities)
        {
            if (!_selectedEntities.Contains(entity))
            {
                _selectedEntities.Add(entity);
            }
        }
        
        OnSelectionChanged();
    }
    
    public void Deselect(IEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        
        if (_selectedEntities.Remove(entity))
        {
            OnSelectionChanged();
        }
    }
    
    public void ClearSelection()
    {
        if (_selectedEntities.Count == 0)
            return;
        
        _selectedEntities.Clear();
        OnSelectionChanged();
    }
    
    public void ToggleSelection(IEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        
        if (_selectedEntities.Contains(entity))
        {
            Deselect(entity);
        }
        else
        {
            Select(entity, addToSelection: true);
        }
    }
    
    private bool SegmentIntersectsBox(LineSegment segment, Rect2D box)
    {
        // Check if either endpoint is inside the box
        if (box.Contains(segment.Start) || box.Contains(segment.End))
            return true;
        
        // Check if segment intersects any of the box edges
        var boxCorners = new[]
        {
            new Point2D(box.X, box.Y),
            new Point2D(box.Right, box.Y),
            new Point2D(box.Right, box.Bottom),
            new Point2D(box.X, box.Bottom)
        };
        
        for (int i = 0; i < 4; i++)
        {
            var edgeStart = boxCorners[i];
            var edgeEnd = boxCorners[(i + 1) % 4];
            var edgeSegment = new LineSegment(edgeStart, edgeEnd);
            
            if (_geometryEngine.DoSegmentsIntersect(segment, edgeSegment))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}

using System.Collections.ObjectModel;
using CAD2DModel.Geometry;
using CAD2DModel.Selection;
using System.Linq;

namespace CAD2DModel.Services.Implementations;

/// <summary>
/// Manages entity and vertex selection with hit testing and selection boxes
/// </summary>
public class SelectionService : ISelectionService
{
    private readonly ObservableCollection<IEntity> _selectedEntities = new();
    private readonly ObservableCollection<VertexHandle> _selectedVertices = new();
    private readonly ObservableCollection<SegmentHandle> _selectedSegments = new();
    private readonly IGeometryEngine _geometryEngine;
    
    public SelectionService(IGeometryEngine geometryEngine)
    {
        _geometryEngine = geometryEngine ?? throw new ArgumentNullException(nameof(geometryEngine));
    }
    
    public IReadOnlyCollection<IEntity> SelectedEntities => _selectedEntities;
    public IReadOnlyCollection<VertexHandle> SelectedVertices => _selectedVertices;
    public IReadOnlyCollection<SegmentHandle> SelectedSegments => _selectedSegments;
    
    public event EventHandler? SelectionChanged;
    public event EventHandler? VertexSelectionChanged;
    public event EventHandler? SegmentSelectionChanged;
    
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
    
    public VertexHandle? HitTestVertex(Point2D point, double tolerance, IEnumerable<IEntity> entities)
    {
        var toleranceSquared = tolerance * tolerance;
        
        foreach (var entity in entities)
        {
            if (!entity.IsVisible)
                continue;
            
            if (entity is Polyline polyline)
            {
                for (int i = 0; i < polyline.Vertices.Count; i++)
                {
                    var vertex = polyline.Vertices[i];
                    var distSquared = point.DistanceSquaredTo(vertex.Location);
                    if (distSquared <= toleranceSquared)
                    {
                        return new VertexHandle(polyline, i);
                    }
                }
            }
            else if (entity is Boundary boundary)
            {
                for (int i = 0; i < boundary.Vertices.Count; i++)
                {
                    var vertex = boundary.Vertices[i];
                    var distSquared = point.DistanceSquaredTo(vertex.Location);
                    if (distSquared <= toleranceSquared)
                    {
                        return new VertexHandle(boundary, i);
                    }
                }
            }
        }
        
        return null;
    }
    
    public List<VertexHandle> HitTestAllVertices(Point2D point, double tolerance, IEnumerable<IEntity> entities)
    {
        var results = new List<VertexHandle>();
        var toleranceSquared = tolerance * tolerance;
        
        foreach (var entity in entities)
        {
            if (!entity.IsVisible)
                continue;
            
            if (entity is Polyline polyline)
            {
                for (int i = 0; i < polyline.Vertices.Count; i++)
                {
                    var vertex = polyline.Vertices[i];
                    var distSquared = point.DistanceSquaredTo(vertex.Location);
                    if (distSquared <= toleranceSquared)
                    {
                        results.Add(new VertexHandle(polyline, i));
                    }
                }
            }
            else if (entity is Boundary boundary)
            {
                for (int i = 0; i < boundary.Vertices.Count; i++)
                {
                    var vertex = boundary.Vertices[i];
                    var distSquared = point.DistanceSquaredTo(vertex.Location);
                    if (distSquared <= toleranceSquared)
                    {
                        results.Add(new VertexHandle(boundary, i));
                    }
                }
            }
        }
        
        return results;
    }
    
    public IEnumerable<VertexHandle> SelectVerticesInBox(Rect2D box, IEnumerable<IEntity> entities)
    {
        var vertices = new List<VertexHandle>();
        
        foreach (var entity in entities)
        {
            if (!entity.IsVisible)
                continue;
            
            if (entity is Polyline polyline)
            {
                for (int i = 0; i < polyline.Vertices.Count; i++)
                {
                    var vertex = polyline.Vertices[i];
                    if (box.Contains(vertex.Location))
                    {
                        vertices.Add(new VertexHandle(polyline, i));
                    }
                }
            }
            else if (entity is Boundary boundary)
            {
                for (int i = 0; i < boundary.Vertices.Count; i++)
                {
                    var vertex = boundary.Vertices[i];
                    if (box.Contains(vertex.Location))
                    {
                        vertices.Add(new VertexHandle(boundary, i));
                    }
                }
            }
        }
        
        return vertices;
    }
    
    public void SelectVertex(VertexHandle vertex, bool addToSelection = false)
    {
        if (vertex == null)
            throw new ArgumentNullException(nameof(vertex));
        
        if (!addToSelection)
        {
            _selectedVertices.Clear();
        }
        
        if (!_selectedVertices.Contains(vertex))
        {
            _selectedVertices.Add(vertex);
        }
        
        OnVertexSelectionChanged();
    }
    
    public void SelectVertices(IEnumerable<VertexHandle> vertices, bool addToSelection = false)
    {
        if (vertices == null)
            throw new ArgumentNullException(nameof(vertices));
        
        if (!addToSelection)
        {
            _selectedVertices.Clear();
        }
        
        foreach (var vertex in vertices)
        {
            if (!_selectedVertices.Contains(vertex))
            {
                _selectedVertices.Add(vertex);
            }
        }
        
        OnVertexSelectionChanged();
    }
    
    public void DeselectVertex(VertexHandle vertex)
    {
        if (vertex == null)
            throw new ArgumentNullException(nameof(vertex));
        
        if (_selectedVertices.Remove(vertex))
        {
            OnVertexSelectionChanged();
        }
    }
    
    public void ClearVertexSelection()
    {
        if (_selectedVertices.Count == 0)
            return;
        
        _selectedVertices.Clear();
        OnVertexSelectionChanged();
    }
    
    public void ClearAllSelections()
    {
        bool changed = false;
        
        if (_selectedEntities.Count > 0)
        {
            _selectedEntities.Clear();
            changed = true;
        }
        
        if (_selectedVertices.Count > 0)
        {
            _selectedVertices.Clear();
            changed = true;
        }
        
        if (_selectedSegments.Count > 0)
        {
            _selectedSegments.Clear();
            changed = true;
        }
        
        if (changed)
        {
            OnSelectionChanged();
            OnVertexSelectionChanged();
            OnSegmentSelectionChanged();
        }
    }
    
    public void ToggleVertexSelection(VertexHandle vertex)
    {
        if (vertex == null)
            throw new ArgumentNullException(nameof(vertex));
        
        if (_selectedVertices.Contains(vertex))
        {
            DeselectVertex(vertex);
        }
        else
        {
            SelectVertex(vertex, addToSelection: true);
        }
    }
    
    public SegmentHandle? HitTestSegment(Point2D point, double tolerance, IEnumerable<IEntity> entities)
    {
        foreach (var entity in entities)
        {
            if (!entity.IsVisible)
                continue;
            
            if (entity is Polyline polyline)
            {
                var vertices = polyline.Vertices.ToList();
                for (int i = 0; i < vertices.Count - 1; i++)
                {
                    var distance = IntersectionCalculator.DistanceToSegment(
                        point, vertices[i].Location, vertices[i + 1].Location, out _);
                    
                    if (distance <= tolerance)
                    {
                        return new SegmentHandle(entity, i);
                    }
                }
            }
            else if (entity is Boundary boundary)
            {
                var vertices = boundary.Vertices.ToList();
                for (int i = 0; i < vertices.Count; i++)
                {
                    int nextI = (i + 1) % vertices.Count;
                    var distance = IntersectionCalculator.DistanceToSegment(
                        point, vertices[i].Location, vertices[nextI].Location, out _);
                    
                    if (distance <= tolerance)
                    {
                        return new SegmentHandle(entity, i);
                    }
                }
            }
        }
        
        return null;
    }
    
    public IEnumerable<SegmentHandle> SelectSegmentsInBox(Rect2D box, IEnumerable<IEntity> entities)
    {
        var segments = new List<SegmentHandle>();
        
        foreach (var entity in entities)
        {
            if (!entity.IsVisible)
                continue;
            
            if (entity is Polyline polyline)
            {
                var vertices = polyline.Vertices.ToList();
                for (int i = 0; i < vertices.Count - 1; i++)
                {
                    var segment = new LineSegment(vertices[i].Location, vertices[i + 1].Location);
                    if (SegmentIntersectsBox(segment, box))
                    {
                        segments.Add(new SegmentHandle(entity, i));
                    }
                }
            }
            else if (entity is Boundary boundary)
            {
                var vertices = boundary.Vertices.ToList();
                for (int i = 0; i < vertices.Count; i++)
                {
                    int nextI = (i + 1) % vertices.Count;
                    var segment = new LineSegment(vertices[i].Location, vertices[nextI].Location);
                    if (SegmentIntersectsBox(segment, box))
                    {
                        segments.Add(new SegmentHandle(entity, i));
                    }
                }
            }
        }
        
        return segments;
    }
    
    public void SelectSegment(SegmentHandle segment, bool addToSelection = false)
    {
        if (segment == null)
            throw new ArgumentNullException(nameof(segment));
        
        if (!addToSelection)
        {
            _selectedSegments.Clear();
        }
        
        if (!_selectedSegments.Contains(segment))
        {
            _selectedSegments.Add(segment);
            OnSegmentSelectionChanged();
        }
    }
    
    public void SelectSegments(IEnumerable<SegmentHandle> segments, bool addToSelection = false)
    {
        if (segments == null)
            throw new ArgumentNullException(nameof(segments));
        
        if (!addToSelection)
        {
            _selectedSegments.Clear();
        }
        
        bool changed = false;
        foreach (var segment in segments)
        {
            if (!_selectedSegments.Contains(segment))
            {
                _selectedSegments.Add(segment);
                changed = true;
            }
        }
        
        if (changed)
        {
            OnSegmentSelectionChanged();
        }
    }
    
    public void DeselectSegment(SegmentHandle segment)
    {
        if (segment == null)
            throw new ArgumentNullException(nameof(segment));
        
        if (_selectedSegments.Remove(segment))
        {
            OnSegmentSelectionChanged();
        }
    }
    
    public void ClearSegmentSelection()
    {
        if (_selectedSegments.Count == 0)
            return;
        
        _selectedSegments.Clear();
        OnSegmentSelectionChanged();
    }
    
    public void ToggleSegmentSelection(SegmentHandle segment)
    {
        if (segment == null)
            throw new ArgumentNullException(nameof(segment));
        
        if (_selectedSegments.Contains(segment))
        {
            DeselectSegment(segment);
        }
        else
        {
            SelectSegment(segment, addToSelection: true);
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
    
    private void OnVertexSelectionChanged()
    {
        VertexSelectionChanged?.Invoke(this, EventArgs.Empty);
    }
    
    private void OnSegmentSelectionChanged()
    {
        SegmentSelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}

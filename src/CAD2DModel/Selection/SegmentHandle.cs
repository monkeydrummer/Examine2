using CAD2DModel.Geometry;

namespace CAD2DModel.Selection;

/// <summary>
/// Represents a handle to a specific segment (edge) of an entity
/// </summary>
public class SegmentHandle
{
    public IEntity Entity { get; }
    public int SegmentIndex { get; }
    public Point2D StartPoint { get; }
    public Point2D EndPoint { get; }

    public SegmentHandle(IEntity entity, int segmentIndex)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        SegmentIndex = segmentIndex;

        // Get start and end points based on entity type
        if (entity is Polyline polyline)
        {
            var vertices = polyline.Vertices.ToList();
            if (segmentIndex < 0 || segmentIndex >= vertices.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(segmentIndex));
            
            StartPoint = vertices[segmentIndex].Location;
            EndPoint = vertices[segmentIndex + 1].Location;
        }
        else if (entity is Boundary boundary)
        {
            var vertices = boundary.Vertices.ToList();
            if (segmentIndex < 0 || segmentIndex >= vertices.Count)
                throw new ArgumentOutOfRangeException(nameof(segmentIndex));
            
            int nextIndex = (segmentIndex + 1) % vertices.Count;
            StartPoint = vertices[segmentIndex].Location;
            EndPoint = vertices[nextIndex].Location;
        }
        else
        {
            throw new ArgumentException("Entity type does not support segments");
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SegmentHandle other)
            return false;

        return Entity == other.Entity && SegmentIndex == other.SegmentIndex;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Entity, SegmentIndex);
    }

    public static bool operator ==(SegmentHandle? left, SegmentHandle? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(SegmentHandle? left, SegmentHandle? right)
    {
        return !(left == right);
    }
}

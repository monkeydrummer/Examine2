using CAD2DModel.Geometry;
using CAD2DModel.Interaction;

namespace CAD2DModel.Annotations;

/// <summary>
/// Represents a control point on an annotation
/// </summary>
public class ControlPoint : IControlPoint
{
    public IAnnotation Annotation { get; }
    public ControlPointType Type { get; }
    public Point2D Location { get; set; }
    public int Index { get; }
    public bool IsMovable { get; init; } = true;
    
    public Cursor CursorType
    {
        get
        {
            return Type switch
            {
                ControlPointType.TopLeft or ControlPointType.BottomRight => Cursor.SizeNWSE,
                ControlPointType.TopRight or ControlPointType.BottomLeft => Cursor.SizeNESW,
                ControlPointType.TopEdge or ControlPointType.BottomEdge => Cursor.SizeNS,
                ControlPointType.LeftEdge or ControlPointType.RightEdge => Cursor.SizeWE,
                ControlPointType.Rotation => Cursor.Hand,
                _ => Cursor.SizeAll
            };
        }
    }
    
    public ControlPoint(IAnnotation annotation, ControlPointType type, Point2D location, int index = 0)
    {
        Annotation = annotation ?? throw new ArgumentNullException(nameof(annotation));
        Type = type;
        Location = location;
        Index = index;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is ControlPoint other)
        {
            return Annotation == other.Annotation && Type == other.Type && Index == other.Index;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Annotation, Type, Index);
    }
}

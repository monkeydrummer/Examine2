using CAD2DModel.Geometry;

namespace CAD2DModel.Annotations;

/// <summary>
/// Arrow annotation with configurable arrow heads
/// </summary>
public class ArrowAnnotation : LinearAnnotation
{
    public ArrowAnnotation() : base()
    {
        // Default arrow appearance - arrow at head only
        ArrowAtHead = true;
        ArrowAtTail = false;
        ArrowHeadStyle = ArrowStyle.FilledTriangle;
        ArrowTailStyle = ArrowStyle.FilledTriangle;
        ArrowSize = 12.0;
        
        Color = Color.Black;
        LineWeight = 2.0f;
    }
    
    public ArrowAnnotation(Point2D start, Point2D end) : base(start, end)
    {
        // Default arrow appearance - arrow at head only
        ArrowAtHead = true;
        ArrowAtTail = false;
        ArrowHeadStyle = ArrowStyle.FilledTriangle;
        ArrowTailStyle = ArrowStyle.FilledTriangle;
        ArrowSize = 12.0;
        
        Color = Color.Black;
        LineWeight = 2.0f;
    }
}

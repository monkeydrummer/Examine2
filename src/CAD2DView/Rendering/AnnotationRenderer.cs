using CAD2DModel.Annotations;
using CAD2DModel.Interaction;
using SkiaSharp;

namespace CAD2DView.Rendering;

/// <summary>
/// Renders annotations using SkiaSharp
/// </summary>
public class AnnotationRenderer
{
    /// <summary>
    /// Convert model Color to SKColor
    /// </summary>
    private static SKColor ToSKColor(CAD2DModel.Annotations.Color color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }
    /// <summary>
    /// Render a collection of annotations
    /// </summary>
    public void RenderAnnotations(IEnumerable<IAnnotation> annotations, IRenderContext context)
    {
        foreach (var annotation in annotations)
        {
            if (!annotation.IsVisible)
                continue;
            
            RenderAnnotation(annotation, context);
            
            // Draw control points if annotation is in edit mode
            if (annotation.IsEditing)
            {
                RenderControlPoints(annotation, context);
            }
            
            // Draw selection highlight if selected
            if (annotation.IsSelected && !annotation.IsEditing)
            {
                RenderSelectionHighlight(annotation, context);
            }
        }
    }
    
    /// <summary>
    /// Render a single annotation
    /// </summary>
    public void RenderAnnotation(IAnnotation annotation, IRenderContext context)
    {
        switch (annotation)
        {
            case PolygonAnnotation polygon:
                RenderPolygonAnnotation(polygon, context);
                break;
            case PolylineAnnotation polyline:
                RenderPolylineAnnotation(polyline, context);
                break;
            case EllipseAnnotation ellipse:
                RenderEllipseAnnotation(ellipse, context);
                break;
            case AngularDimensionAnnotation angularDim:
                RenderAngularDimensionAnnotation(angularDim, context);
                break;
            case DimensionAnnotation dimension:
                RenderDimensionAnnotation(dimension, context);
                break;
            case RulerAnnotation ruler:
                RenderRulerAnnotation(ruler, context);
                break;
            case ArrowAnnotation arrow:
                RenderArrowAnnotation(arrow, context);
                break;
            case LinearAnnotation linear:
                RenderLinearAnnotation(linear, context);
                break;
            case TextAnnotation text:
                RenderTextAnnotation(text, context);
                break;
            case RectangleAnnotation rectangle:
                RenderRectangleAnnotation(rectangle, context);
                break;
            default:
                // Fallback: just draw bounding box
                RenderDefaultAnnotation(annotation, context);
                break;
        }
    }
    
    private void RenderEllipseAnnotation(EllipseAnnotation annotation, IRenderContext context)
    {
        var color = ToSKColor(annotation.Color);
        
        // Draw filled ellipse if enabled
        if (annotation.IsFilled)
        {
            var fillColor = ToSKColor(annotation.FillColor);
            context.DrawCircle(
                annotation.Center,
                annotation.RadiusX, // TODO: proper ellipse rendering
                color.Red, color.Green, color.Blue,
                annotation.LineWeight,
                true,
                fillColor.Red, fillColor.Green, fillColor.Blue, fillColor.Alpha
            );
        }
        else
        {
            // Just draw the outline
            context.DrawCircle(
                annotation.Center,
                annotation.RadiusX, // TODO: proper ellipse rendering
                color.Red, color.Green, color.Blue,
                annotation.LineWeight,
                false
            );
        }
    }
    
    private void RenderPolylineAnnotation(PolylineAnnotation annotation, IRenderContext context)
    {
        if (annotation.Vertices.Count < 2)
            return;
        
        var color = ToSKColor(annotation.Color);
        
        // Draw segments
        for (int i = 0; i < annotation.Vertices.Count - 1; i++)
        {
            context.DrawLine(
                annotation.Vertices[i],
                annotation.Vertices[i + 1],
                color.Red, color.Green, color.Blue,
                annotation.LineWeight,
                annotation.LineStyle != LineStyle.Solid
            );
        }
        
        // Draw arrows if enabled
        if (annotation.ArrowAtStart && annotation.Vertices.Count >= 2)
        {
            context.DrawArrowHead(
                annotation.Vertices[1],
                annotation.Vertices[0],
                color.Red, color.Green, color.Blue,
                annotation.ArrowSize,
                annotation.ArrowStyle == ArrowStyle.FilledTriangle
            );
        }
        
        if (annotation.ArrowAtEnd && annotation.Vertices.Count >= 2)
        {
            context.DrawArrowHead(
                annotation.Vertices[^2],
                annotation.Vertices[^1],
                color.Red, color.Green, color.Blue,
                annotation.ArrowSize,
                annotation.ArrowStyle == ArrowStyle.FilledTriangle
            );
        }
    }
    
    private void RenderPolygonAnnotation(PolygonAnnotation annotation, IRenderContext context)
    {
        if (annotation.Vertices.Count < 3)
        {
            // Render as polyline if insufficient vertices
            RenderPolylineAnnotation(annotation, context);
            return;
        }
        
        var color = ToSKColor(annotation.Color);
        
        // Draw all segments including closing segment
        for (int i = 0; i < annotation.Vertices.Count - 1; i++)
        {
            context.DrawLine(
                annotation.Vertices[i],
                annotation.Vertices[i + 1],
                color.Red, color.Green, color.Blue,
                annotation.LineWeight,
                annotation.LineStyle != LineStyle.Solid
            );
        }
        
        // Draw closing segment
        context.DrawLine(
            annotation.Vertices[^1],
            annotation.Vertices[0],
            color.Red, color.Green, color.Blue,
            annotation.LineWeight,
            annotation.LineStyle != LineStyle.Solid
        );
        
        // TODO: Add fill rendering
        // TODO: Add hatch rendering
    }
    
    private void RenderAngularDimensionAnnotation(AngularDimensionAnnotation annotation, IRenderContext context)
    {
        var color = ToSKColor(annotation.Color);
        var angles = annotation.GetArcAngles();
        
        // Draw the arc
        context.DrawArc(
            annotation.CenterPoint,
            annotation.ArcRadius,
            angles.StartAngle,
            angles.SweepAngle,
            color.Red, color.Green, color.Blue,
            annotation.LineWeight
        );
        
        // Draw extension lines from center to arms
        context.DrawLine(
            annotation.CenterPoint,
            annotation.FirstArmPoint,
            color.Red, color.Green, color.Blue,
            annotation.LineWeight * 0.7f
        );
        
        context.DrawLine(
            annotation.CenterPoint,
            annotation.SecondArmPoint,
            color.Red, color.Green, color.Blue,
            annotation.LineWeight * 0.7f
        );
        
        // Calculate arc endpoints for arrow placement
        double startAngleRad = angles.StartAngle * Math.PI / 180.0;
        double endAngleRad = (angles.StartAngle + angles.SweepAngle) * Math.PI / 180.0;
        
        var arcStart = new CAD2DModel.Geometry.Point2D(
            annotation.CenterPoint.X + annotation.ArcRadius * Math.Cos(startAngleRad),
            annotation.CenterPoint.Y + annotation.ArcRadius * Math.Sin(startAngleRad)
        );
        
        var arcEnd = new CAD2DModel.Geometry.Point2D(
            annotation.CenterPoint.X + annotation.ArcRadius * Math.Cos(endAngleRad),
            annotation.CenterPoint.Y + annotation.ArcRadius * Math.Sin(endAngleRad)
        );
        
        // Draw arrows at arc ends (tangent to the arc)
        if (annotation.ArrowAtStart)
        {
            // Tangent direction at start
            var tangentStart = new CAD2DModel.Geometry.Point2D(
                arcStart.X - annotation.ArcRadius * Math.Sin(startAngleRad) * 0.1,
                arcStart.Y + annotation.ArcRadius * Math.Cos(startAngleRad) * 0.1
            );
            context.DrawArrowHead(
                tangentStart,
                arcStart,
                color.Red, color.Green, color.Blue,
                annotation.ArrowSize,
                annotation.ArrowStyle == ArrowStyle.FilledTriangle
            );
        }
        
        if (annotation.ArrowAtEnd)
        {
            // Tangent direction at end
            var tangentEnd = new CAD2DModel.Geometry.Point2D(
                arcEnd.X + annotation.ArcRadius * Math.Sin(endAngleRad) * 0.1,
                arcEnd.Y - annotation.ArcRadius * Math.Cos(endAngleRad) * 0.1
            );
            context.DrawArrowHead(
                tangentEnd,
                arcEnd,
                color.Red, color.Green, color.Blue,
                annotation.ArrowSize,
                annotation.ArrowStyle == ArrowStyle.FilledTriangle
            );
        }
        
        // Draw angle text at midpoint of arc
        double midAngleRad = (angles.StartAngle + angles.SweepAngle / 2.0) * Math.PI / 180.0;
        var textPosition = new CAD2DModel.Geometry.Point2D(
            annotation.CenterPoint.X + annotation.ArcRadius * 1.3 * Math.Cos(midAngleRad),
            annotation.CenterPoint.Y + annotation.ArcRadius * 1.3 * Math.Sin(midAngleRad)
        );
        
        var textColor = ToSKColor(annotation.TextColor);
        var bgColor = ToSKColor(annotation.TextBackgroundColor);
        context.DrawText(
            annotation.GetAngleText(),
            textPosition,
            annotation.FontSize,
            annotation.FontFamily,
            textColor.Red, textColor.Green, textColor.Blue,
            0, // No rotation for angle text
            annotation.FontBold,
            annotation.FontItalic,
            annotation.DrawTextBackground,
            bgColor.Red, bgColor.Green, bgColor.Blue, bgColor.Alpha
        );
    }
    
    private void RenderDimensionAnnotation(DimensionAnnotation annotation, IRenderContext context)
    {
        var color = ToSKColor(annotation.Color);
        var points = annotation.GetDimensionPoints();
        
        // Draw extension lines
        context.DrawLine(
            points.ExtLine1Start,
            points.ExtLine1End,
            color.Red, color.Green, color.Blue,
            annotation.LineWeight * 0.7f
        );
        
        context.DrawLine(
            points.ExtLine2Start,
            points.ExtLine2End,
            color.Red, color.Green, color.Blue,
            annotation.LineWeight * 0.7f
        );
        
        // Draw dimension line
        context.DrawLine(
            points.DimLineStart,
            points.DimLineEnd,
            color.Red, color.Green, color.Blue,
            annotation.LineWeight
        );
        
        // Draw arrows at dimension line ends
        if (annotation.ArrowAtTail)
        {
            context.DrawArrowHead(
                points.DimLineEnd,
                points.DimLineStart,
                color.Red, color.Green, color.Blue,
                annotation.ArrowSize,
                annotation.ArrowTailStyle == ArrowStyle.FilledTriangle
            );
        }
        
        if (annotation.ArrowAtHead)
        {
            context.DrawArrowHead(
                points.DimLineStart,
                points.DimLineEnd,
                color.Red, color.Green, color.Blue,
                annotation.ArrowSize,
                annotation.ArrowHeadStyle == ArrowStyle.FilledTriangle
            );
        }
        
        // Draw dimension text at midpoint of dimension line
        var midpoint = new CAD2DModel.Geometry.Point2D(
            (points.DimLineStart.X + points.DimLineEnd.X) / 2.0,
            (points.DimLineStart.Y + points.DimLineEnd.Y) / 2.0
        );
        
        var textColor = ToSKColor(annotation.TextColor);
        var bgColor = ToSKColor(annotation.TextBackgroundColor);
        context.DrawText(
            annotation.GetDimensionText(),
            midpoint,
            annotation.FontSize,
            annotation.FontFamily,
            textColor.Red, textColor.Green, textColor.Blue,
            annotation.AngleDegrees,
            annotation.FontBold,
            annotation.FontItalic,
            annotation.DrawTextBackground,
            bgColor.Red, bgColor.Green, bgColor.Blue, bgColor.Alpha
        );
    }
    
    private void RenderArrowAnnotation(ArrowAnnotation annotation, IRenderContext context)
    {
        // Render as a linear annotation (already handles arrows)
        RenderLinearAnnotation(annotation, context);
    }
    
    private void RenderRulerAnnotation(RulerAnnotation annotation, IRenderContext context)
    {
        // Render as a linear annotation (line + optional arrows)
        RenderLinearAnnotation(annotation, context);
        
        // The text is automatically set by the ruler, so it will be rendered by the base method
    }
    
    private void RenderLinearAnnotation(LinearAnnotation annotation, IRenderContext context)
    {
        var color = ToSKColor(annotation.Color);
        
        // Draw the main line
        bool dashed = annotation.LineStyle != LineStyle.Solid;
        context.DrawLine(
            annotation.StartPoint,
            annotation.EndPoint,
            color.Red, color.Green, color.Blue,
            annotation.LineWeight,
            dashed
        );
        
        // Draw arrow heads if enabled
        if (annotation.ArrowAtHead)
        {
            context.DrawArrowHead(
                annotation.StartPoint,
                annotation.EndPoint,
                color.Red, color.Green, color.Blue,
                annotation.ArrowSize,
                annotation.ArrowHeadStyle == ArrowStyle.FilledTriangle
            );
        }
        
        if (annotation.ArrowAtTail)
        {
            context.DrawArrowHead(
                annotation.EndPoint,
                annotation.StartPoint,
                color.Red, color.Green, color.Blue,
                annotation.ArrowSize,
                annotation.ArrowTailStyle == ArrowStyle.FilledTriangle
            );
        }
        
        // Draw text if present
        if (!string.IsNullOrEmpty(annotation.Text))
        {
            // Position text at midpoint of line
            var midpoint = new CAD2DModel.Geometry.Point2D(
                (annotation.StartPoint.X + annotation.EndPoint.X) / 2.0,
                (annotation.StartPoint.Y + annotation.EndPoint.Y) / 2.0
            );
            
            var textColor = ToSKColor(annotation.TextColor);
            var bgColor = ToSKColor(annotation.TextBackgroundColor);
            context.DrawText(
                annotation.Text,
                midpoint,
                annotation.FontSize,
                annotation.FontFamily,
                textColor.Red, textColor.Green, textColor.Blue,
                annotation.AngleDegrees,
                annotation.FontBold,
                annotation.FontItalic,
                annotation.DrawTextBackground,
                bgColor.Red, bgColor.Green, bgColor.Blue, bgColor.Alpha
            );
        }
    }
    
    private void RenderTextAnnotation(TextAnnotation annotation, IRenderContext context)
    {
        // Draw leader line if present
        if (annotation.HasLeader && annotation.LeaderEndPoint.HasValue)
        {
            var color = ToSKColor(annotation.Color);
            context.DrawLine(
                annotation.Position,
                annotation.LeaderEndPoint.Value,
                color.Red, color.Green, color.Blue,
                annotation.LineWeight
            );
            
            // Draw arrow at end of leader
            context.DrawArrowHead(
                annotation.Position,
                annotation.LeaderEndPoint.Value,
                color.Red, color.Green, color.Blue,
                annotation.ArrowSize,
                annotation.LeaderArrowStyle == ArrowStyle.FilledTriangle
            );
        }
        
        // Draw text
        var textColor = ToSKColor(annotation.TextColor);
        var bgColor = ToSKColor(annotation.BackgroundColor);
        context.DrawText(
            annotation.Text,
            annotation.Position,
            annotation.FontSize,
            annotation.FontFamily,
            textColor.Red, textColor.Green, textColor.Blue,
            annotation.RotationDegrees,
            annotation.FontBold,
            annotation.FontItalic,
            annotation.DrawBackground,
            bgColor.Red, bgColor.Green, bgColor.Blue, bgColor.Alpha
        );
    }
    
    private void RenderRectangleAnnotation(RectangleAnnotation annotation, IRenderContext context)
    {
        var color = ToSKColor(annotation.Color);
        
        // Draw filled rectangle if enabled
        if (annotation.IsFilled)
        {
            var fillColor = ToSKColor(annotation.FillColor);
            context.DrawRectangle(
                annotation.TopLeft,
                annotation.BottomRight,
                color.Red, color.Green, color.Blue,
                annotation.LineWeight,
                true,
                fillColor.Red, fillColor.Green, fillColor.Blue, fillColor.Alpha
            );
        }
        else
        {
            // Just draw the outline
            context.DrawRectangle(
                annotation.TopLeft,
                annotation.BottomRight,
                color.Red, color.Green, color.Blue,
                annotation.LineWeight,
                false
            );
        }
        
        // TODO: Add hatching support if IsHatched is true
    }
    
    private void RenderDefaultAnnotation(IAnnotation annotation, IRenderContext context)
    {
        // Fallback rendering: just draw bounding box
        var bounds = annotation.GetBounds();
        var topLeft = new CAD2DModel.Geometry.Point2D(bounds.X, bounds.Y);
        var bottomRight = new CAD2DModel.Geometry.Point2D(bounds.X + bounds.Width, bounds.Y + bounds.Height);
        
        var color = annotation.Color;
        context.DrawRectangle(
            topLeft,
            bottomRight,
            color.R, color.G, color.B,
            1f,
            false
        );
    }
    
    private void RenderControlPoints(IAnnotation annotation, IRenderContext context)
    {
        var controlPoints = annotation.GetControlPoints();
        foreach (var cp in controlPoints)
        {
            context.DrawControlPoint(cp.Location);
        }
    }
    
    private void RenderSelectionHighlight(IAnnotation annotation, IRenderContext context)
    {
        var bounds = annotation.GetBounds();
        var topLeft = new CAD2DModel.Geometry.Point2D(bounds.X, bounds.Y);
        var bottomRight = new CAD2DModel.Geometry.Point2D(bounds.X + bounds.Width, bounds.Y + bounds.Height);
        
        // Draw dashed selection box
        context.DrawLine(topLeft, new CAD2DModel.Geometry.Point2D(bottomRight.X, topLeft.Y), 0, 100, 255, 1.5f, true);
        context.DrawLine(new CAD2DModel.Geometry.Point2D(bottomRight.X, topLeft.Y), bottomRight, 0, 100, 255, 1.5f, true);
        context.DrawLine(bottomRight, new CAD2DModel.Geometry.Point2D(topLeft.X, bottomRight.Y), 0, 100, 255, 1.5f, true);
        context.DrawLine(new CAD2DModel.Geometry.Point2D(topLeft.X, bottomRight.Y), topLeft, 0, 100, 255, 1.5f, true);
    }
}

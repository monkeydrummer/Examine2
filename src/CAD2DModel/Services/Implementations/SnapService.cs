using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Camera;

namespace CAD2DModel.Services.Implementations;

/// <summary>
/// Snap service implementation for various snap modes
/// </summary>
public class SnapService : ISnapService
{
    public SnapMode ActiveSnapModes { get; set; } = SnapMode.Vertex | SnapMode.Midpoint | SnapMode.Ortho;
    public double SnapTolerancePixels { get; set; } = 10.0; // screen pixels
    public double VertexSnapTolerancePixels { get; set; } = 8.0; // screen pixels - less aggressive
    public double GridSpacing { get; set; } = 1.0; // world units
    public double OrthoAngleToleranceDegrees { get; set; } = 5.0; // only snap if within 5 degrees of ortho/diagonal
    
    public SnapResult Snap(Point2D point, IEnumerable<IEntity> entities, Camera2D camera)
    {
        var polylines = entities.OfType<Polyline>().ToList();
        
        // Try snap modes in priority order
        if ((ActiveSnapModes & SnapMode.Vertex) != 0)
        {
            var vertexSnap = SnapToVertex(point, polylines, camera);
            if (vertexSnap?.IsSnapped == true)
                return vertexSnap;
        }
        
        if ((ActiveSnapModes & SnapMode.Midpoint) != 0)
        {
            var midpointSnap = SnapToMidpoint(point, polylines, camera);
            if (midpointSnap?.IsSnapped == true)
                return midpointSnap;
        }
        
        if ((ActiveSnapModes & SnapMode.Nearest) != 0)
        {
            var nearestSnap = SnapToNearest(point, polylines, camera);
            if (nearestSnap?.IsSnapped == true)
                return nearestSnap;
        }
        
        if ((ActiveSnapModes & SnapMode.Grid) != 0)
        {
            return SnapToGrid(point, camera);
        }
        
        return new SnapResult(point);
    }
    
    public SnapResult? SnapToVertex(Point2D point, IEnumerable<Polyline> polylines, Camera2D camera)
    {
        // Convert pixel tolerance to world units based on current zoom
        // Scale is "world units per screen pixel", so multiply to get world units
        double worldTolerance = VertexSnapTolerancePixels * camera.Scale;
        
        Point2D? closestVertex = null;
        double closestDistance = double.MaxValue;
        Polyline? closestPolyline = null;
        
        foreach (var polyline in polylines)
        {
            if (!polyline.IsVisible)
                continue;
            
            foreach (var vertex in polyline.Vertices)
            {
                double distance = point.DistanceTo(vertex.Location);
                
                if (distance < worldTolerance && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestVertex = vertex.Location;
                    closestPolyline = polyline;
                }
            }
        }
        
        if (closestVertex.HasValue)
        {
            return new SnapResult(closestVertex.Value, true, SnapMode.Vertex, closestPolyline);
        }
        
        return null;
    }
    
    public SnapResult? SnapToMidpoint(Point2D point, IEnumerable<Polyline> polylines, Camera2D camera)
    {
        // Convert pixel tolerance to world units based on current zoom
        // Scale is "world units per screen pixel", so multiply to get world units
        double worldTolerance = SnapTolerancePixels * camera.Scale;
        
        Point2D? closestMidpoint = null;
        double closestDistance = double.MaxValue;
        Polyline? closestPolyline = null;
        
        foreach (var polyline in polylines)
        {
            if (!polyline.IsVisible)
                continue;
            
            int segmentCount = polyline.GetSegmentCount();
            for (int i = 0; i < segmentCount; i++)
            {
                var segment = polyline.GetSegment(i);
                var midpoint = segment.Midpoint;
                
                double distance = point.DistanceTo(midpoint);
                
                if (distance < worldTolerance && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestMidpoint = midpoint;
                    closestPolyline = polyline;
                }
            }
        }
        
        if (closestMidpoint.HasValue)
        {
            return new SnapResult(closestMidpoint.Value, true, SnapMode.Midpoint, closestPolyline);
        }
        
        return null;
    }
    
    public SnapResult SnapToGrid(Point2D point, Camera2D camera)
    {
        double x = Math.Round(point.X / GridSpacing) * GridSpacing;
        double y = Math.Round(point.Y / GridSpacing) * GridSpacing;
        
        var snappedPoint = new Point2D(x, y);
        
        // Convert pixel tolerance to world units based on current zoom
        // Scale is "world units per screen pixel", so multiply to get world units
        double worldTolerance = SnapTolerancePixels * camera.Scale;
        bool isSnapped = point.DistanceTo(snappedPoint) < worldTolerance;
        
        return new SnapResult(snappedPoint, isSnapped, SnapMode.Grid);
    }
    
    public SnapResult SnapToOrtho(Point2D point, Point2D referencePoint)
    {
        var delta = point - referencePoint;
        var distance = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        
        // If too close to reference point, no meaningful angle to snap
        if (distance < 1e-6)
        {
            return new SnapResult(point, false, SnapMode.None);
        }
        
        // Calculate angle in radians from horizontal (0 to 2π)
        var angleRadians = Math.Atan2(delta.Y, delta.X);
        
        // Convert to degrees (0 to 360)
        var angleDegrees = angleRadians * 180.0 / Math.PI;
        if (angleDegrees < 0)
            angleDegrees += 360.0;
        
        // Check all 8 snap directions: 0°, 45°, 90°, 135°, 180°, 225°, 270°, 315°
        var snapAngles = new[] { 0.0, 45.0, 90.0, 135.0, 180.0, 225.0, 270.0, 315.0 };
        
        double closestSnapAngle = 0.0;
        double closestDifference = double.MaxValue;
        
        foreach (var snapAngle in snapAngles)
        {
            // Calculate difference, accounting for wrap-around at 360°
            var diff1 = Math.Abs(angleDegrees - snapAngle);
            var diff2 = Math.Abs(angleDegrees - (snapAngle + 360.0));
            var diff3 = Math.Abs(angleDegrees - (snapAngle - 360.0));
            var minDiff = Math.Min(diff1, Math.Min(diff2, diff3));
            
            if (minDiff < closestDifference)
            {
                closestDifference = minDiff;
                closestSnapAngle = snapAngle;
            }
        }
        
        // If not within tolerance of any snap angle, don't snap
        if (closestDifference > OrthoAngleToleranceDegrees)
        {
            return new SnapResult(point, false, SnapMode.None);
        }
        
        // Snap to the closest angle
        var snappedAngleRadians = closestSnapAngle * Math.PI / 180.0;
        var snappedPoint = new Point2D(
            referencePoint.X + distance * Math.Cos(snappedAngleRadians),
            referencePoint.Y + distance * Math.Sin(snappedAngleRadians)
        );
        
        return new SnapResult(snappedPoint, true, SnapMode.Ortho);
    }
    
    public SnapResult? SnapToNearest(Point2D point, IEnumerable<Polyline> polylines, Camera2D camera)
    {
        // Convert pixel tolerance to world units based on current zoom
        // Scale is "world units per screen pixel", so multiply to get world units
        double worldTolerance = SnapTolerancePixels * camera.Scale;
        
        Point2D? closestPoint = null;
        double closestDistance = double.MaxValue;
        Polyline? closestPolyline = null;
        
        foreach (var polyline in polylines)
        {
            if (!polyline.IsVisible)
                continue;
            
            int segmentCount = polyline.GetSegmentCount();
            for (int i = 0; i < segmentCount; i++)
            {
                var segment = polyline.GetSegment(i);
                var nearestPoint = segment.ClosestPoint(point);
                double distance = point.DistanceTo(nearestPoint);
                
                if (distance < worldTolerance && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = nearestPoint;
                    closestPolyline = polyline;
                }
            }
        }
        
        if (closestPoint.HasValue)
        {
            return new SnapResult(closestPoint.Value, true, SnapMode.Nearest, closestPolyline);
        }
        
        return null;
    }
}

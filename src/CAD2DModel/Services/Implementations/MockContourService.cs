using CAD2DModel.Geometry;
using CAD2DModel.Results;

namespace CAD2DModel.Services.Implementations;

/// <summary>
/// Mock implementation of contour service that generates fake data
/// Used for testing visualization before solver is implemented
/// </summary>
public class MockContourService : IContourService
{
    private ContourData? _cachedData;
    
    public ContourSettings Settings { get; } = new ContourSettings();
    
    public ContourData? CurrentContourData => _cachedData;
    
    public event EventHandler? ContoursUpdated;
    
    public ContourData GenerateContours(ExternalBoundary externalBoundary, IEnumerable<Boundary> excavations, ResultField field)
    {
        // Create a regular mesh within the external boundary
        var bounds = CalculateBounds(externalBoundary);
        double meshSpacing = externalBoundary.MeshResolution;
        
        var contourData = new ContourData
        {
            Field = field
        };
        
        // Generate a regular grid of points
        var pointMap = new Dictionary<(int, int), int>(); // (i,j) -> index
        int pointIndex = 0;
        
        int nx = (int)Math.Ceiling((bounds.maxX - bounds.minX) / meshSpacing) + 1;
        int ny = (int)Math.Ceiling((bounds.maxY - bounds.minY) / meshSpacing) + 1;
        
        for (int j = 0; j < ny; j++)
        {
            for (int i = 0; i < nx; i++)
            {
                double x = bounds.minX + i * meshSpacing;
                double y = bounds.minY + j * meshSpacing;
                var point = new Point2D(x, y);
                
                // Include all points inside external boundary (including inside excavations)
                // We'll mask excavations with white fill during rendering
                if (IsPointInside(point, externalBoundary))
                {
                    contourData.MeshPoints.Add(point);
                    
                    // Generate mock result value based on position and field type
                    double value = GenerateMockValue(point, externalBoundary, excavations, field);
                    contourData.Values.Add(value);
                    
                    pointMap[(i, j)] = pointIndex;
                    pointIndex++;
                }
            }
        }
        
        // Generate triangular mesh connectivity
        for (int j = 0; j < ny - 1; j++)
        {
            for (int i = 0; i < nx - 1; i++)
            {
                // Try to create two triangles for this grid cell
                bool hasP00 = pointMap.TryGetValue((i, j), out int p00);
                bool hasP10 = pointMap.TryGetValue((i + 1, j), out int p10);
                bool hasP01 = pointMap.TryGetValue((i, j + 1), out int p01);
                bool hasP11 = pointMap.TryGetValue((i + 1, j + 1), out int p11);
                
                // Lower triangle
                if (hasP00 && hasP10 && hasP01)
                {
                    contourData.Triangles.Add(p00);
                    contourData.Triangles.Add(p10);
                    contourData.Triangles.Add(p01);
                }
                
                // Upper triangle
                if (hasP10 && hasP11 && hasP01)
                {
                    contourData.Triangles.Add(p10);
                    contourData.Triangles.Add(p11);
                    contourData.Triangles.Add(p01);
                }
            }
        }
        
        // Calculate min/max values
        if (contourData.Values.Count > 0)
        {
            contourData.MinValue = contourData.Values.Min();
            contourData.MaxValue = contourData.Values.Max();
        }
        
        _cachedData = contourData;
        ContoursUpdated?.Invoke(this, EventArgs.Empty);
        
        return contourData;
    }
    
    public void InvalidateContours()
    {
        _cachedData = null;
    }
    
    private (double minX, double maxX, double minY, double maxY) CalculateBounds(ExternalBoundary boundary)
    {
        if (boundary.Vertices.Count == 0)
            return (0, 10, 0, 10);
        
        double minX = boundary.Vertices.Min(v => v.Location.X);
        double maxX = boundary.Vertices.Max(v => v.Location.X);
        double minY = boundary.Vertices.Min(v => v.Location.Y);
        double maxY = boundary.Vertices.Max(v => v.Location.Y);
        
        return (minX, maxX, minY, maxY);
    }
    
    private bool IsPointInsideExternalAndOutsideExcavations(Point2D point, ExternalBoundary external, IEnumerable<Boundary> excavations)
    {
        // Check if inside external boundary
        if (!IsPointInside(point, external))
            return false;
        
        // Check if outside all excavations
        foreach (var excavation in excavations)
        {
            if (IsPointInside(point, excavation))
                return false;
        }
        
        return true;
    }
    
    private bool IsPointInside(Point2D point, Boundary boundary)
    {
        // Ray casting algorithm
        int intersections = 0;
        int n = boundary.Vertices.Count;
        
        for (int i = 0; i < n; i++)
        {
            var v1 = boundary.Vertices[i].Location;
            var v2 = boundary.Vertices[(i + 1) % n].Location;
            
            if ((v1.Y > point.Y) != (v2.Y > point.Y))
            {
                double xIntersection = (v2.X - v1.X) * (point.Y - v1.Y) / (v2.Y - v1.Y) + v1.X;
                if (point.X < xIntersection)
                    intersections++;
            }
        }
        
        return (intersections % 2) == 1;
    }
    
    private double GenerateMockValue(Point2D point, ExternalBoundary external, IEnumerable<Boundary> excavations, ResultField field)
    {
        // Generate fake values that create interesting patterns
        // Use distance from nearest excavation boundary to simulate stress concentration
        
        double minDistToExcavation = double.MaxValue;
        Point2D? nearestExcavationCenter = null;
        
        foreach (var excavation in excavations)
        {
            // Get excavation center
            var center = new Point2D(
                excavation.Vertices.Average(v => v.Location.X),
                excavation.Vertices.Average(v => v.Location.Y)
            );
            
            // Distance from point to each edge of excavation
            for (int i = 0; i < excavation.Vertices.Count; i++)
            {
                var v1 = excavation.Vertices[i].Location;
                var v2 = excavation.Vertices[(i + 1) % excavation.Vertices.Count].Location;
                
                double dist = DistanceToSegment(point, v1, v2);
                if (dist < minDistToExcavation)
                {
                    minDistToExcavation = dist;
                    nearestExcavationCenter = center;
                }
            }
        }
        
        // If no excavations, use distance from center of external boundary
        if (nearestExcavationCenter == null)
        {
            nearestExcavationCenter = new Point2D(
                external.Vertices.Average(v => v.Location.X),
                external.Vertices.Average(v => v.Location.Y)
            );
            minDistToExcavation = point.DistanceTo(nearestExcavationCenter.Value);
        }
        
        // Generate different patterns based on field type
        double baseValue = 0;
        
        switch (field)
        {
            case ResultField.VonMisesStress:
            case ResultField.PrincipalStress1:
                // Stress concentration near excavations (decreases with distance)
                if (minDistToExcavation < 5.0)
                {
                    baseValue = 50.0 - 8.0 * minDistToExcavation; // High stress near boundary
                }
                else
                {
                    baseValue = 10.0; // Far field stress
                }
                break;
                
            case ResultField.StressX:
                // Horizontal stress varies with x position
                baseValue = 10.0 + 5.0 * Math.Sin(point.X * 0.5);
                if (minDistToExcavation < 3.0)
                    baseValue += 20.0 / (minDistToExcavation + 0.5);
                break;
                
            case ResultField.StressY:
                // Vertical stress varies with depth (y position)
                baseValue = 15.0 + 0.5 * point.Y;
                if (minDistToExcavation < 3.0)
                    baseValue += 15.0 / (minDistToExcavation + 0.5);
                break;
                
            case ResultField.StressXY:
                // Shear stress
                baseValue = 5.0 * Math.Sin(point.X * 0.3) * Math.Cos(point.Y * 0.3);
                if (minDistToExcavation < 2.0)
                    baseValue += 10.0 / (minDistToExcavation + 0.5);
                break;
                
            case ResultField.DisplacementMagnitude:
                // Displacement decreases away from excavation
                if (minDistToExcavation < 10.0)
                {
                    baseValue = 0.5 / (minDistToExcavation + 1.0);
                }
                break;
                
            case ResultField.DisplacementX:
            case ResultField.DisplacementY:
                // Directional displacement
                baseValue = 0.2 / (minDistToExcavation + 2.0);
                break;
        }
        
        // Add some noise for realism
        baseValue += (new Random(point.GetHashCode()).NextDouble() - 0.5) * baseValue * 0.1;
        
        return baseValue;
    }
    
    private double DistanceToSegment(Point2D point, Point2D segStart, Point2D segEnd)
    {
        var v = segEnd - segStart;
        var w = point - segStart;
        
        double c1 = w.Dot(v);
        if (c1 <= 0)
            return point.DistanceTo(segStart);
        
        double c2 = v.Dot(v);
        if (c1 >= c2)
            return point.DistanceTo(segEnd);
        
        double b = c1 / c2;
        var pb = segStart + v * b;
        return point.DistanceTo(pb);
    }
}

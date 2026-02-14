using CAD2DModel.Geometry;
using Examine2DModel.Analysis;

namespace Examine2DModel.BEM;

/// <summary>
/// Discretizes boundaries into boundary elements for BEM analysis
/// Ports logic from bcompute2d.cpp lines 6973-7069
/// </summary>
public class BoundaryElementDiscretizer
{
    private readonly BEMConfiguration _configuration;
    
    public BoundaryElementDiscretizer(BEMConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    /// <summary>
    /// Discretize a collection of boundaries into boundary elements
    /// </summary>
    /// <param name="boundaries">Boundaries to discretize (excavations, surfaces, etc.)</param>
    /// <returns>List of boundary elements ready for BEM analysis</returns>
    public List<BoundaryElement> DiscretizeBoundaries(IEnumerable<Boundary> boundaries)
    {
        var boundaryList = boundaries.ToList();
        if (boundaryList.Count == 0)
        {
            return new List<BoundaryElement>();
        }
        
        // Calculate total perimeter of all boundaries
        double totalPerimeter = CalculateTotalPerimeter(boundaryList);
        
        // Calculate target element size based on total perimeter and target element count
        // This matches C++ logic: total_length / (discfactor * default_elements)
        // where discfactor is the number of boundary types (in our case, we use boundary count)
        double targetElementSize = totalPerimeter / _configuration.TargetElementCount;
        
        // Discretize each boundary
        var elements = new List<BoundaryElement>();
        int boundaryId = 0;
        
        foreach (var boundary in boundaryList)
        {
            var boundaryElements = DiscretizeBoundary(boundary, targetElementSize, boundaryId++);
            elements.AddRange(boundaryElements);
        }
        
        return elements;
    }
    
    /// <summary>
    /// Discretize a single boundary into elements
    /// </summary>
    private List<BoundaryElement> DiscretizeBoundary(Boundary boundary, double targetElementSize, int boundaryId)
    {
        var elements = new List<BoundaryElement>();
        int segmentCount = boundary.GetSegmentCount();
        
        for (int i = 0; i < segmentCount; i++)
        {
            var segment = boundary.GetSegment(i);
            double segmentLength = segment.Length;
            
            // Calculate number of elements for this segment
            // Port of C++ logic: num = (int)(0.49999999 + (length/length_element))
            int numElements = (int)(0.49999999 + (segmentLength / targetElementSize));
            if (numElements < 1)
                numElements = 1;
            
            // Apply adaptive sizing if enabled
            if (_configuration.UseAdaptiveElementSizing)
            {
                numElements = ApplyAdaptiveRefinement(boundary, i, numElements);
            }
            
            // Create elements for this segment
            var segmentElements = CreateElementsForSegment(
                segment.Start,
                segment.End,
                numElements,
                ConvertToElementType(_configuration.ElementType),
                boundaryId,
                isGroundSurface: IsGroundSurface(boundary)
            );
            
            elements.AddRange(segmentElements);
        }
        
        return elements;
    }
    
    /// <summary>
    /// Apply adaptive refinement to increase element density at corners and high curvature
    /// </summary>
    private int ApplyAdaptiveRefinement(Boundary boundary, int segmentIndex, int baseElementCount)
    {
        // Calculate angle at the vertex between this segment and the next
        double angle = CalculateInteriorAngle(boundary, segmentIndex);
        
        // Refinement factor based on angle deviation from straight line (180 degrees)
        // Sharp corners (small angles) get more refinement
        double angleDeviation = Math.Abs(180.0 - angle);
        double refinementFactor = 1.0 + (angleDeviation / 180.0) * (_configuration.MaxRefinementFactor - 1.0);
        
        // Apply refinement factor
        int refinedCount = (int)(baseElementCount * refinementFactor + 0.5);
        
        // Ensure minimum of 1 element
        return Math.Max(1, refinedCount);
    }
    
    /// <summary>
    /// Calculate interior angle at a vertex (in degrees)
    /// </summary>
    private double CalculateInteriorAngle(Boundary boundary, int segmentIndex)
    {
        int vertexCount = boundary.VertexCount;
        if (vertexCount < 3)
            return 180.0; // No angle for line segments
        
        // Get three consecutive points
        var p1 = boundary.Vertices[segmentIndex].Location;
        var p2 = boundary.Vertices[(segmentIndex + 1) % vertexCount].Location;
        var p3 = boundary.Vertices[(segmentIndex + 2) % vertexCount].Location;
        
        // Calculate vectors
        var v1 = new Vector2D(p1.X - p2.X, p1.Y - p2.Y);
        var v2 = new Vector2D(p3.X - p2.X, p3.Y - p2.Y);
        
        // Calculate angle using dot product
        double dotProduct = v1.X * v2.X + v1.Y * v2.Y;
        double mag1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
        double mag2 = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);
        
        if (mag1 < 1e-10 || mag2 < 1e-10)
            return 180.0;
        
        double cosAngle = dotProduct / (mag1 * mag2);
        
        // Clamp to valid range for acos
        cosAngle = Math.Max(-1.0, Math.Min(1.0, cosAngle));
        
        double angleRadians = Math.Acos(cosAngle);
        return angleRadians * 180.0 / Math.PI;
    }
    
    /// <summary>
    /// Create boundary elements for a segment by subdividing it
    /// Ports logic from AddBoundaryElements in C++
    /// </summary>
    private List<BoundaryElement> CreateElementsForSegment(
        Point2D start,
        Point2D end,
        int numElements,
        int elementType,
        int boundaryId,
        bool isGroundSurface)
    {
        var elements = new List<BoundaryElement>(numElements);
        
        // Calculate incremental step along the segment
        double dx = (end.X - start.X) / numElements;
        double dy = (end.Y - start.Y) / numElements;
        
        for (int i = 0; i < numElements; i++)
        {
            // Calculate start and end points for this element
            var elementStart = new Point2D(
                start.X + i * dx,
                start.Y + i * dy
            );
            
            var elementEnd = new Point2D(
                start.X + (i + 1) * dx,
                start.Y + (i + 1) * dy
            );
            
            // Create the boundary element
            var element = BoundaryElement.Create(elementStart, elementEnd, elementType, boundaryId);
            element.IsGroundSurface = isGroundSurface;
            
            // Default boundary conditions (will be set later based on analysis type)
            // Excavation boundaries typically have zero traction (free surface)
            element.BoundaryConditionType = 1; // Traction specified
            element.NormalBoundaryCondition = 0.0;
            element.ShearBoundaryCondition = 0.0;
            
            elements.Add(element);
        }
        
        return elements;
    }
    
    /// <summary>
    /// Calculate total perimeter of all boundaries
    /// </summary>
    private double CalculateTotalPerimeter(List<Boundary> boundaries)
    {
        double totalPerimeter = 0.0;
        
        foreach (var boundary in boundaries)
        {
            totalPerimeter += boundary.Length;
        }
        
        return totalPerimeter;
    }
    
    /// <summary>
    /// Check if boundary is a ground surface (external boundary)
    /// </summary>
    private bool IsGroundSurface(Boundary boundary)
    {
        // External boundaries are ground surfaces
        return boundary is ExternalBoundary;
    }
    
    /// <summary>
    /// Convert from Analysis.ElementType enum to integer element type
    /// </summary>
    private int ConvertToElementType(ElementType elementType)
    {
        return elementType switch
        {
            ElementType.Constant => 1,
            ElementType.Linear => 2,
            ElementType.Quadratic => 3,
            _ => 2 // Default to linear
        };
    }
    
    /// <summary>
    /// Get statistics about the discretization
    /// </summary>
    public DiscretizationStatistics GetStatistics(List<BoundaryElement> elements)
    {
        if (elements.Count == 0)
        {
            return new DiscretizationStatistics();
        }
        
        var stats = new DiscretizationStatistics
        {
            TotalElementCount = elements.Count,
            MinElementLength = elements.Min(e => e.Length),
            MaxElementLength = elements.Max(e => e.Length),
            AverageElementLength = elements.Average(e => e.Length)
        };
        
        // Count elements by boundary
        stats.ElementsByBoundary = elements
            .GroupBy(e => e.BoundaryId)
            .ToDictionary(g => g.Key, g => g.Count());
        
        return stats;
    }
}

/// <summary>
/// Statistics about boundary element discretization
/// </summary>
public class DiscretizationStatistics
{
    public int TotalElementCount { get; set; }
    public double MinElementLength { get; set; }
    public double MaxElementLength { get; set; }
    public double AverageElementLength { get; set; }
    public Dictionary<int, int> ElementsByBoundary { get; set; } = new();
    
    public override string ToString()
    {
        return $"Elements: {TotalElementCount}, " +
               $"Length: {MinElementLength:F2}-{MaxElementLength:F2} (avg {AverageElementLength:F2}), " +
               $"Boundaries: {ElementsByBoundary.Count}";
    }
}

using CAD2DModel.Geometry;
using CAD2DModel.Results;
using CAD2DModel.Services;
using Examine2DModel.Analysis;
using Examine2DModel.BEM;
using Examine2DModel.Materials;
using Examine2DModel.Stress;

namespace Examine2DModel.Services;

/// <summary>
/// Real implementation of contour service using the BEM solver.
/// Replaces MockContourService to generate actual stress/displacement contours.
/// </summary>
public class ContourService : IContourService
{
    private readonly IIsotropicMaterial _material;
    private readonly BEMConfiguration _bemConfig;
    private ContourData? _cachedData;
    
    public ContourSettings Settings { get; }
    
    public ContourData? CurrentContourData => _cachedData;
    
    public event EventHandler? ContoursUpdated;
    
    /// <summary>
    /// Create a new contour service with a default material
    /// </summary>
    public ContourService() : this(CreateDefaultMaterial())
    {
    }
    
    /// <summary>
    /// Create a new contour service with specified material properties
    /// </summary>
    public ContourService(IIsotropicMaterial material)
    {
        _material = material ?? throw new ArgumentNullException(nameof(material));
        _bemConfig = new BEMConfiguration();
        Settings = new ContourSettings();
    }
    
    /// <summary>
    /// Generate contour data for a specific result field within the external boundary
    /// </summary>
    public ContourData GenerateContours(ExternalBoundary externalBoundary, IEnumerable<Boundary> excavations, ResultField field)
    {
        // Step 1: Set up BEM configuration for the problem
        var boundaries = new List<Boundary>();
        
        // Add external boundary (if needed - some BEM formulations use only excavations)
        // For now, we'll use excavations only as they are the primary boundaries of interest
        var excavationsList = excavations.ToList();
        boundaries.AddRange(excavationsList);
        
        if (excavationsList.Count == 0)
        {
            // No excavations - return empty contour data
            return new ContourData { Field = field };
        }
        
        // Step 2: Create BEM solver with current material properties
        var matrixSolver = new MatrixSolverService();
        var bemSolver = new BoundaryElementSolver(_material, _bemConfig, matrixSolver);
        
        // Step 4: Calculate stress grid bounds from external boundary
        var bounds = CalculateBounds(externalBoundary);
        
        // Create a stress grid for the field point evaluation
        // The BEM solver will use its adaptive grid generator internally,
        // but we need to specify the region bounds
        int gridResolution = CalculateGridResolution(externalBoundary.MeshResolution, bounds);
        var stressGrid = StressGrid.CreateUniform(bounds, gridResolution, gridResolution);
        
        // Step 3: Set up boundary configuration
        var boundaryConfig = new BoundaryConfiguration
        {
            Boundaries = boundaries,
            StressGrid = stressGrid
        };
        
        // Step 5: Set up solver options
        var solverOptions = new SolverOptions
        {
            PlaneStrainType = PlaneStrainType.PlaneStrain,
            ElementType = ElementType.Constant,  // IMPORTANT: Must match C++ code (order=1)
            NumberOfElements = _bemConfig.TargetElementCount,
            Tolerance = _bemConfig.Tolerance,
            MaxIterations = _bemConfig.MaxIterations
        };
        
        // Step 6: Solve the BEM system to get stress field
        StressField stressField;
        try
        {
            stressField = bemSolver.Solve(boundaryConfig, solverOptions);
        }
        catch (Exception ex)
        {
            // If BEM solve fails, return empty contour data
            System.Diagnostics.Debug.WriteLine($"BEM solve failed: {ex.Message}");
            return new ContourData { Field = field };
        }
        
        // Step 7: Convert StressField to ContourData for visualization
        var contourData = ConvertStressFieldToContourData(stressField, field, externalBoundary, excavations);
        
        // Cache the results
        _cachedData = contourData;
        ContoursUpdated?.Invoke(this, EventArgs.Empty);
        
        return contourData;
    }
    
    /// <summary>
    /// Invalidate cached contours (call when geometry changes)
    /// </summary>
    public void InvalidateContours()
    {
        _cachedData = null;
    }
    
    /// <summary>
    /// Convert StressField from BEM solver to ContourData for visualization
    /// </summary>
    private ContourData ConvertStressFieldToContourData(
        StressField stressField, 
        ResultField field,
        ExternalBoundary externalBoundary,
        IEnumerable<Boundary> excavations)
    {
        var contourData = new ContourData
        {
            Field = field
        };
        
        var grid = stressField.Grid;
        int xPoints = grid.XPoints;
        int yPoints = grid.YPoints;
        
        // Build a map from grid coordinates to point indices
        var pointMap = new Dictionary<(int, int), int>();
        int pointIndex = 0;
        
        // Step 1: Create mesh points and extract field values
        // NOTE: We include ALL points inside the external boundary, even those inside excavations.
        // This prevents jagged edges. Excavations will be masked during rendering by drawing
        // white polygons on top.
        int pointsAdded = 0;
        int pointsSkippedOutside = 0;
        int pointsInsideExcavation = 0;
        
        for (int j = 0; j < yPoints; j++)
        {
            for (int i = 0; i < xPoints; i++)
            {
                int gridIndex = j * xPoints + i;
                var point = grid.GetPoint(gridIndex);
                
                // Only skip points outside the external boundary
                if (!IsPointInside(point, externalBoundary))
                {
                    pointsSkippedOutside++;
                    continue;
                }
                
                // Check if inside excavation (for tracking, but DON'T skip)
                foreach (var excavation in excavations)
                {
                    if (IsPointInside(point, excavation))
                    {
                        pointsInsideExcavation++;
                        break;
                    }
                }
                
                // Add point and extract field value (even if inside excavation)
                contourData.MeshPoints.Add(point);
                pointMap[(i, j)] = pointIndex;
                pointIndex++;
                
                // Extract the requested field value
                double value = ExtractFieldValue(stressField, gridIndex, field);
                contourData.Values.Add(value);
                pointsAdded++;
            }
        }
        
        // Step 2: Generate triangular mesh connectivity
        for (int j = 0; j < yPoints - 1; j++)
        {
            for (int i = 0; i < xPoints - 1; i++)
            {
                // Try to create two triangles for this grid cell
                bool hasP00 = pointMap.TryGetValue((i, j), out int p00);
                bool hasP10 = pointMap.TryGetValue((i + 1, j), out int p10);
                bool hasP01 = pointMap.TryGetValue((i, j + 1), out int p01);
                bool hasP11 = pointMap.TryGetValue((i + 1, j + 1), out int p11);
                
                // Lower triangle (p00, p10, p01)
                if (hasP00 && hasP10 && hasP01)
                {
                    contourData.Triangles.Add(p00);
                    contourData.Triangles.Add(p10);
                    contourData.Triangles.Add(p01);
                }
                
                // Upper triangle (p10, p11, p01)
                if (hasP10 && hasP11 && hasP01)
                {
                    contourData.Triangles.Add(p10);
                    contourData.Triangles.Add(p11);
                    contourData.Triangles.Add(p01);
                }
            }
        }
        
        // Step 3: Calculate min/max values for color scale
        if (contourData.Values.Count > 0)
        {
            contourData.MinValue = contourData.Values.Min();
            contourData.MaxValue = contourData.Values.Max();
        }
        
        // Step 4: Store excavations to be masked during rendering
        contourData.ExcavationsToMask.AddRange(excavations);
        
        return contourData;
    }
    
    /// <summary>
    /// Extract the requested field value from the stress field at a given grid index
    /// </summary>
    private double ExtractFieldValue(StressField stressField, int index, ResultField field)
    {
        // Ensure index is within bounds
        if (index < 0 || index >= stressField.Grid.PointCount)
            return 0.0;
        
        return field switch
        {
            ResultField.PrincipalStress1 => stressField.Sigma1[index],
            ResultField.PrincipalStress3 => stressField.Sigma3[index],
            
            // For StressX, StressY, StressXY, VonMisesStress, we need to compute from principal stresses
            // The BEM solver currently only populates Sigma1, Sigma3, and Theta
            // These would need to be back-calculated or the StressField class needs additional fields
            ResultField.StressX => CalculateStressXFromPrincipal(stressField, index),
            ResultField.StressY => CalculateStressYFromPrincipal(stressField, index),
            ResultField.StressXY => CalculateStressXYFromPrincipal(stressField, index),
            ResultField.VonMisesStress => CalculateVonMisesStress(stressField, index),
            
            ResultField.DisplacementX => stressField.Displacements[index].X,
            ResultField.DisplacementY => stressField.Displacements[index].Y,
            ResultField.DisplacementMagnitude => CalculateDisplacementMagnitude(stressField.Displacements[index]),
            
            _ => 0.0
        };
    }
    
    /// <summary>
    /// Calculate stress X component from principal stresses and angle
    /// σx = σ1*cos²θ + σ3*sin²θ
    /// </summary>
    private double CalculateStressXFromPrincipal(StressField stressField, int index)
    {
        double sigma1 = stressField.Sigma1[index];
        double sigma3 = stressField.Sigma3[index];
        double theta = stressField.Theta[index]; // In radians
        
        double cos2 = Math.Cos(theta) * Math.Cos(theta);
        double sin2 = Math.Sin(theta) * Math.Sin(theta);
        
        return sigma1 * cos2 + sigma3 * sin2;
    }
    
    /// <summary>
    /// Calculate stress Y component from principal stresses and angle
    /// σy = σ1*sin²θ + σ3*cos²θ
    /// </summary>
    private double CalculateStressYFromPrincipal(StressField stressField, int index)
    {
        double sigma1 = stressField.Sigma1[index];
        double sigma3 = stressField.Sigma3[index];
        double theta = stressField.Theta[index]; // In radians
        
        double cos2 = Math.Cos(theta) * Math.Cos(theta);
        double sin2 = Math.Sin(theta) * Math.Sin(theta);
        
        return sigma1 * sin2 + sigma3 * cos2;
    }
    
    /// <summary>
    /// Calculate shear stress XY from principal stresses and angle
    /// τxy = (σ1 - σ3)*sinθ*cosθ
    /// </summary>
    private double CalculateStressXYFromPrincipal(StressField stressField, int index)
    {
        double sigma1 = stressField.Sigma1[index];
        double sigma3 = stressField.Sigma3[index];
        double theta = stressField.Theta[index]; // In radians
        
        return (sigma1 - sigma3) * Math.Sin(theta) * Math.Cos(theta);
    }
    
    /// <summary>
    /// Calculate von Mises stress from principal stresses
    /// For 2D plane strain: σvm = sqrt(σ1² - σ1*σ3 + σ3²)
    /// </summary>
    private double CalculateVonMisesStress(StressField stressField, int index)
    {
        double sigma1 = stressField.Sigma1[index];
        double sigma3 = stressField.Sigma3[index];
        
        // 2D von Mises formula
        return Math.Sqrt(sigma1 * sigma1 - sigma1 * sigma3 + sigma3 * sigma3);
    }
    
    /// <summary>
    /// Calculate displacement magnitude
    /// </summary>
    private double CalculateDisplacementMagnitude(Vector2D displacement)
    {
        return Math.Sqrt(displacement.X * displacement.X + displacement.Y * displacement.Y);
    }
    
    /// <summary>
    /// Calculate bounds from external boundary
    /// </summary>
    private Rect2D CalculateBounds(ExternalBoundary boundary)
    {
        if (boundary.Vertices.Count == 0)
            return new Rect2D(0, 0, 10, 10);
        
        double minX = boundary.Vertices.Min(v => v.Location.X);
        double maxX = boundary.Vertices.Max(v => v.Location.X);
        double minY = boundary.Vertices.Min(v => v.Location.Y);
        double maxY = boundary.Vertices.Max(v => v.Location.Y);
        
        return new Rect2D(minX, minY, maxX - minX, maxY - minY);
    }
    
    /// <summary>
    /// Calculate appropriate grid resolution based on mesh resolution
    /// </summary>
    private int CalculateGridResolution(double meshResolution, Rect2D bounds)
    {
        // Calculate number of grid points based on mesh resolution
        // Aim for approximately meshResolution spacing between grid points
        double dimension = Math.Max(bounds.Width, bounds.Height);
        int resolution = (int)Math.Ceiling(dimension / meshResolution);
        
        // Clamp to reasonable range (10-200)
        return Math.Clamp(resolution, 10, 200);
    }
    
    /// <summary>
    /// Check if a point is inside a boundary using ray casting algorithm
    /// </summary>
    private bool IsPointInside(Point2D point, Boundary boundary)
    {
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
    
    /// <summary>
    /// Create a default rock material for testing/demo purposes
    /// In production, this would be provided by the application or user settings
    /// </summary>
    private static IIsotropicMaterial CreateDefaultMaterial()
    {
        return new DefaultRockMaterial
        {
            Name = "Default Rock",
            Density = 2700.0, // kg/m³
            YoungModulus = 50000.0, // MPa
            PoissonRatio = 0.25
        };
    }
    
    /// <summary>
    /// Simple implementation of IIsotropicMaterial for default material
    /// </summary>
    private class DefaultRockMaterial : IIsotropicMaterial
    {
        public string Name { get; set; } = "Default Rock";
        public double Density { get; set; }
        public double YoungModulus { get; set; }
        public double PoissonRatio { get; set; }
        
        public double ShearModulus => YoungModulus / (2.0 * (1.0 + PoissonRatio));
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Examine2DModel.BEM;
using Examine2DModel.Materials;
using Examine2DModel.Analysis;
using CAD2DModel.Geometry;
using MathNet.Numerics.LinearAlgebra;

namespace Examine2DModel.Tests.BEM;

/// <summary>
/// Tests to verify that the BEM influence matrix has reasonable conditioning
/// and that the matrix assembly produces correct results for simple geometries
/// </summary>
[TestClass]
public class MatrixConditioningTests
{
    [TestMethod]
    public void CircularExcavation_MatrixIsWellConditioned()
    {
        // Arrange: Create a circular excavation (5m radius, 32 elements)
        var material = new TestMaterial
        {
            Name = "Rock",
            Density = 2500.0,
            YoungModulus = 10000.0, // MPa
            PoissonRatio = 0.25
        };
        
        var config = new BEMConfiguration
        {
            ElementType = ElementType.Constant,
            EnableCaching = false
        };
        
        var builder = new InfluenceMatrixBuilder(material, config);
        
        // Create circular boundary with 32 elements
        var elements = CreateCircularElements(
            centerX: 0.0,
            centerY: 0.0,
            radius: 5.0,
            numElements: 32);
        
        // Act: Build influence matrix (use HALF-SPACE with ground surface above excavation)
        var matrix = builder.BuildMatrix(elements, groundSurfaceY: 10.0, isHalfSpace: true);
        
        // Assert: Check matrix properties
        Assert.AreEqual(64, matrix.RowCount, "Matrix should be 64x64 for 32 constant elements");
        Assert.AreEqual(64, matrix.ColumnCount);
        
        // Check for NaN or Inf
        for (int i = 0; i < matrix.RowCount; i++)
        {
            for (int j = 0; j < matrix.ColumnCount; j++)
            {
                Assert.IsFalse(double.IsNaN(matrix[i, j]), 
                    $"Matrix contains NaN at [{i},{j}]");
                Assert.IsFalse(double.IsInfinity(matrix[i, j]), 
                    $"Matrix contains Infinity at [{i},{j}]");
            }
        }
        
        // Check condition number
        double conditionNumber = matrix.ConditionNumber();
        System.Diagnostics.Debug.WriteLine($"Condition number: {conditionNumber:E3}");
        
        // For a well-discretized circular excavation, condition number should be reasonable
        Assert.IsTrue(conditionNumber < 1e12, 
            $"Condition number {conditionNumber:E3} is too large (indicates ill-conditioning)");
        
        // Check that matrix has reasonable magnitudes
        double maxAbs = 0;
        double minNonZeroAbs = double.MaxValue;
        for (int i = 0; i < matrix.RowCount; i++)
        {
            for (int j = 0; j < matrix.ColumnCount; j++)
            {
                double abs = Math.Abs(matrix[i, j]);
                if (abs > maxAbs) maxAbs = abs;
                if (abs > 0 && abs < minNonZeroAbs) minNonZeroAbs = abs;
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"Matrix element range: [{minNonZeroAbs:E3}, {maxAbs:E3}]");
        Assert.IsTrue(maxAbs < 1e10, 
            $"Matrix contains unreasonably large values (max={maxAbs:E3})");
    }
    
    [TestMethod]
    public void CircularExcavation_SolutionHasReasonableMagnitude()
    {
        // Arrange: Create a circular excavation with known far-field stress
        var material = new TestMaterial
        {
            Name = "Rock",
            Density = 2500.0,
            YoungModulus = 10000.0, // MPa
            PoissonRatio = 0.25
        };
        
        var config = new BEMConfiguration
        {
            ElementType = ElementType.Constant,
            EnableCaching = false,
            DirectSolverThreshold = 10000 // Use direct solver
        };
        
        var builder = new InfluenceMatrixBuilder(material, config);
        var solver = new MatrixSolverService(config);
        
        // Create circular boundary
        var elements = CreateCircularElements(
            centerX: 0.0,
            centerY: 0.0,
            radius: 5.0,
            numElements: 32);
        
        // Build matrix (use HALF-SPACE)
        var matrix = builder.BuildMatrix(elements, groundSurfaceY: 10.0, isHalfSpace: true);
        
        // Build RHS with far-field stresses (σ1=-10 MPa, σ3=-5 MPa, horizontal)
        var rhs = BuildRightHandSide(elements, sigma1: -10.0, sigma3: -5.0, angle: 0.0);
        
        // Act: Solve system
        var solution = solver.Solve(matrix, rhs);
        
        // Assert: Check solution properties
        Assert.AreEqual(64, solution.Length, "Solution vector should have 64 entries");
        
        // Check for NaN or Inf
        foreach (var value in solution)
        {
            Assert.IsFalse(double.IsNaN(value), 
                "Solution contains NaN");
            Assert.IsFalse(double.IsInfinity(value), 
                "Solution contains Infinity");
        }
        
        // Check solution magnitude is reasonable
        double maxAbs = solution.Max(Math.Abs);
        double minAbs = solution.Min(Math.Abs);
        
        System.Diagnostics.Debug.WriteLine($"Solution range: [{minAbs:E3}, {maxAbs:E3}]");
        
        // For excavation with 10 MPa far-field stress, displacements should be ~mm scale
        // and boundary tractions should be ~MPa scale
        // NOTE: Actual values depend on problem setup and units. The key is they're not astronomical!
        Assert.IsTrue(maxAbs < 1e6, 
            $"Solution contains unreasonably large values (max={maxAbs:E3})");
        Assert.IsTrue(maxAbs > 1e-10, 
            "Solution is essentially zero (no response to loading)");
    }
    
    [TestMethod]
    [DataRow(16)]
    [DataRow(32)]
    [DataRow(64)]
    public void CircularExcavation_ConditionNumberScalesReasonablyWithRefinement(int numElements)
    {
        // Arrange
        var material = new TestMaterial
        {
            Name = "Rock",
            Density = 2500.0,
            YoungModulus = 10000.0,
            PoissonRatio = 0.25
        };
        
        var config = new BEMConfiguration
        {
            ElementType = ElementType.Constant,
            EnableCaching = false
        };
        
        var builder = new InfluenceMatrixBuilder(material, config);
        var elements = CreateCircularElements(0.0, 0.0, 5.0, numElements);
        
        // Act (use HALF-SPACE)
        var matrix = builder.BuildMatrix(elements, groundSurfaceY: 10.0, isHalfSpace: true);
        double conditionNumber = matrix.ConditionNumber();
        
        // Assert
        System.Diagnostics.Debug.WriteLine(
            $"Elements: {numElements}, Condition number: {conditionNumber:E3}");
        
        // Condition number should grow slowly with refinement (not exponentially)
        // For a well-posed BEM problem, condition number typically grows as O(N) to O(N^2)
        double expectedMaxCondition = Math.Pow(numElements, 2.5); // Conservative upper bound
        Assert.IsTrue(conditionNumber < expectedMaxCondition,
            $"Condition number {conditionNumber:E3} grows too rapidly with refinement");
    }
    
    /// <summary>
    /// Create boundary elements for a circular excavation
    /// </summary>
    private List<BoundaryElement> CreateCircularElements(
        double centerX, double centerY, double radius, int numElements)
    {
        var elements = new List<BoundaryElement>();
        double angleStep = 2.0 * Math.PI / numElements;
        
        for (int i = 0; i < numElements; i++)
        {
            double angle1 = i * angleStep;
            double angle2 = (i + 1) * angleStep;
            
            var start = new Point2D(
                centerX + radius * Math.Cos(angle1),
                centerY + radius * Math.Sin(angle1));
            
            var end = new Point2D(
                centerX + radius * Math.Cos(angle2),
                centerY + radius * Math.Sin(angle2));
            
            var element = BoundaryElement.Create(start, end, elementType: 1, boundaryId: 0);
            
            // Excavation boundary: traction-free (BC type 1 = traction specified)
            element.BoundaryConditionType = 1;
            element.NormalBoundaryCondition = 0.0;
            element.ShearBoundaryCondition = 0.0;
            
            elements.Add(element);
        }
        
        return elements;
    }
    
    /// <summary>
    /// Build right-hand side vector from far-field principal stresses
    /// </summary>
    private Vector<double> BuildRightHandSide(
        List<BoundaryElement> elements, double sigma1, double sigma3, double angle)
    {
        var rhs = Vector<double>.Build.Dense(elements.Count * 2);
        
        // Convert principal stresses to Cartesian
        double angleRad = angle * Math.PI / 180.0;
        double sum = (sigma1 + sigma3) / 2.0;
        double dif = (sigma1 - sigma3) / 2.0;
        
        double sigxx = sum + dif * Math.Cos(2.0 * angleRad);
        double sigyy = sum - dif * Math.Cos(2.0 * angleRad);
        double sigxy = dif * Math.Sin(2.0 * angleRad);
        
        for (int i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            
            // Calculate tractions on boundary from far-field stresses
            // Normal vector (pointing outward from excavation)
            double normalX = -element.SineDirection;
            double normalY = element.CosineDirection;
            
            // Traction = stress · normal
            double tn = sigxx * normalX + sigxy * normalY;
            double ts = sigxy * normalX + sigyy * normalY;
            
            rhs[2 * i] = tn;
            rhs[2 * i + 1] = ts;
        }
        
        return rhs;
    }
    
    /// <summary>
    /// Test material implementation
    /// </summary>
    private class TestMaterial : IIsotropicMaterial
    {
        public string Name { get; set; } = "TestMaterial";
        public double Density { get; set; }
        public double YoungModulus { get; set; }
        public double PoissonRatio { get; set; }
        public double ShearModulus => YoungModulus / (2.0 * (1.0 + PoissonRatio));
    }
}

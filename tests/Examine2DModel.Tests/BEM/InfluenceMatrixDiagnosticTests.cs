using Microsoft.VisualStudio.TestTools.UnitTesting;
using Examine2DModel.BEM;
using Examine2DModel.Materials;
using Examine2DModel.Analysis;
using CAD2DModel.Geometry;

namespace Examine2DModel.Tests.BEM;

[TestClass]
public class InfluenceMatrixDiagnosticTests
{
    [TestMethod]
    public void SingleElement_CheckInfluenceValues()
    {
        // Create a simple 2-element test case to understand the matrix structure
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
        
        // Create 2 horizontal elements
        var elements = new List<BoundaryElement>
        {
            BoundaryElement.Create(
                new Point2D(0, 0),
                new Point2D(1, 0),
                elementType: 1,
                boundaryId: 0),
            BoundaryElement.Create(
                new Point2D(1, 0),
                new Point2D(2, 0),
                elementType: 1,
                boundaryId: 0)
        };
        
        // Set BC type 1 (traction specified)
        foreach (var elem in elements)
        {
            elem.BoundaryConditionType = 1;
        }
        
        // Build matrix
        var matrix = builder.BuildMatrix(elements, groundSurfaceY: 10.0, isHalfSpace: true);
        
        // Print matrix for inspection
        System.Diagnostics.Debug.WriteLine("Influence Matrix (2 elements, 4x4):");
        for (int i = 0; i < matrix.RowCount; i++)
        {
            var row = new List<string>();
            for (int j = 0; j < matrix.ColumnCount; j++)
            {
                row.Add($"{matrix[i, j],12:E3}");
            }
            System.Diagnostics.Debug.WriteLine($"Row {i}: {string.Join(" ", row)}");
        }
        
        // Check diagonal entries (self-influence)
        System.Diagnostics.Debug.WriteLine($"\nDiagonal entries:");
        for (int i = 0; i < matrix.RowCount; i++)
        {
            System.Diagnostics.Debug.WriteLine($"  [{i},{i}] = {matrix[i, i]:E6}");
        }
        
        // Basic sanity checks
        Assert.IsFalse(matrix.Exists(double.IsNaN), "Matrix contains NaN");
        Assert.IsFalse(matrix.Exists(double.IsInfinity), "Matrix contains Infinity");
    }
    
    private class TestMaterial : IIsotropicMaterial
    {
        public string Name { get; set; } = "TestMaterial";
        public double Density { get; set; }
        public double YoungModulus { get; set; }
        public double PoissonRatio { get; set; }
        public double ShearModulus => YoungModulus / (2.0 * (1.0 + PoissonRatio));
    }
}

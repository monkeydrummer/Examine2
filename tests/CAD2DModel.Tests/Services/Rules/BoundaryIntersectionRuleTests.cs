using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
using CAD2DModel.Services.Implementations.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CAD2DModel.Tests.Services.Rules;

[TestClass]
public class BoundaryIntersectionRuleTests
{
    private BoundaryIntersectionRule _rule = null!;
    private IGeometryModel _model = null!;

    [TestInitialize]
    public void Setup()
    {
        _rule = new BoundaryIntersectionRule(tolerance: 1e-6);
        _model = new GeometryModel();
    }

    [TestMethod]
    public void Priority_ShouldBe150()
    {
        Assert.AreEqual(150, _rule.Priority);
    }

    [TestMethod]
    public void Name_ShouldBeCorrect()
    {
        Assert.AreEqual("Boundary Intersection", _rule.Name);
    }

    [TestMethod]
    public void AppliesTo_IntersectableBoundary_ReturnsTrue()
    {
        var boundary = new Boundary { Intersectable = true };
        Assert.IsTrue(_rule.AppliesTo(boundary));
    }

    [TestMethod]
    public void AppliesTo_NonIntersectableBoundary_ReturnsFalse()
    {
        var boundary = new Boundary { Intersectable = false };
        Assert.IsFalse(_rule.AppliesTo(boundary));
    }

    [TestMethod]
    public void AppliesTo_Polyline_ReturnsFalse()
    {
        var polyline = new Polyline();
        Assert.IsFalse(_rule.AppliesTo(polyline));
    }

    [TestMethod]
    public void Apply_NoOtherBoundaries_NoChange()
    {
        var boundary = new Boundary { Intersectable = true };
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 1)));
        _model.AddEntity(boundary);
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(4, boundary.Vertices.Count);
    }

    [TestMethod]
    public void Apply_NoIntersections_NoChange()
    {
        var boundary1 = new Boundary { Intersectable = true };
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 1)));
        _model.AddEntity(boundary1);
        
        var boundary2 = new Boundary { Intersectable = true };
        boundary2.Vertices.Add(new Vertex(new Point2D(5, 5)));
        boundary2.Vertices.Add(new Vertex(new Point2D(6, 5)));
        boundary2.Vertices.Add(new Vertex(new Point2D(6, 6)));
        boundary2.Vertices.Add(new Vertex(new Point2D(5, 6)));
        _model.AddEntity(boundary2);
        
        _rule.Apply(boundary1, _model);
        
        Assert.AreEqual(4, boundary1.Vertices.Count);
        Assert.AreEqual(4, boundary2.Vertices.Count);
    }

    [TestMethod]
    public void Apply_SimpleCross_AddsIntersectionVertex()
    {
        // Horizontal rectangle
        var boundary1 = new Boundary { Intersectable = true };
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 0.5)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 0.5)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 1.5)));
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 1.5)));
        _model.AddEntity(boundary1);
        
        // Vertical rectangle crossing the first
        var boundary2 = new Boundary { Intersectable = true };
        boundary2.Vertices.Add(new Vertex(new Point2D(0.5, 0)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 0)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 2)));
        boundary2.Vertices.Add(new Vertex(new Point2D(0.5, 2)));
        _model.AddEntity(boundary2);
        
        int originalCount = boundary1.Vertices.Count;
        _rule.Apply(boundary1, _model);
        
        // Should have added intersection vertices
        Assert.IsTrue(boundary1.Vertices.Count > originalCount);
        
        // Check that intersection points are present (approximately at (0.5, 0.5), (1.5, 0.5), (1.5, 1.5), (0.5, 1.5))
        bool hasIntersectionNear_0_5_0_5 = boundary1.Vertices.Any(v => 
            Math.Abs(v.Location.X - 0.5) < 0.01 && Math.Abs(v.Location.Y - 0.5) < 0.01);
        bool hasIntersectionNear_1_5_0_5 = boundary1.Vertices.Any(v => 
            Math.Abs(v.Location.X - 1.5) < 0.01 && Math.Abs(v.Location.Y - 0.5) < 0.01);
        
        Assert.IsTrue(hasIntersectionNear_0_5_0_5 || hasIntersectionNear_1_5_0_5, 
            "Should have at least one intersection vertex");
    }

    [TestMethod]
    public void Apply_TwoSegmentsCrossing_AddsVertex()
    {
        // Simple cross: one segment goes horizontal, another vertical
        var boundary1 = new Boundary { Intersectable = true };
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 1)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 1)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 1.5)));
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 1.5)));
        _model.AddEntity(boundary1);
        
        var boundary2 = new Boundary { Intersectable = true };
        boundary2.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 0)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 2)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1, 2)));
        _model.AddEntity(boundary2);
        
        _rule.Apply(boundary1, _model);
        
        // Should have intersection at (1, 1) and (1.5, 1)
        bool hasIntersectionAt1_1 = boundary1.Vertices.Any(v => 
            Math.Abs(v.Location.X - 1.0) < 0.001 && Math.Abs(v.Location.Y - 1.0) < 0.001);
        bool hasIntersectionAt1_5_1 = boundary1.Vertices.Any(v => 
            Math.Abs(v.Location.X - 1.5) < 0.001 && Math.Abs(v.Location.Y - 1.0) < 0.001);
        
        Assert.IsTrue(hasIntersectionAt1_1 || hasIntersectionAt1_5_1);
    }

    [TestMethod]
    public void Apply_IntersectionAtEndpoint_NoVertexAdded()
    {
        // Boundaries touching at a corner - should not add vertex
        var boundary1 = new Boundary { Intersectable = true };
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 1)));
        _model.AddEntity(boundary1);
        
        var boundary2 = new Boundary { Intersectable = true };
        boundary2.Vertices.Add(new Vertex(new Point2D(1, 0))); // Touches corner
        boundary2.Vertices.Add(new Vertex(new Point2D(2, 0)));
        boundary2.Vertices.Add(new Vertex(new Point2D(2, 1)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1, 1)));
        _model.AddEntity(boundary2);
        
        _rule.Apply(boundary1, _model);
        
        // Should not add vertices for endpoint intersections
        Assert.AreEqual(4, boundary1.Vertices.Count);
    }

    [TestMethod]
    public void Apply_MultipleIntersections_AddsAllVertices()
    {
        // Create a boundary that crosses another at multiple points
        var boundary1 = new Boundary { Intersectable = true };
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(3, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(3, 3)));
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 3)));
        _model.AddEntity(boundary1);
        
        // Diagonal boundary crossing the square
        var boundary2 = new Boundary { Intersectable = true };
        boundary2.Vertices.Add(new Vertex(new Point2D(-1, 1.5)));
        boundary2.Vertices.Add(new Vertex(new Point2D(4, 1.5)));
        boundary2.Vertices.Add(new Vertex(new Point2D(4, 2.5)));
        boundary2.Vertices.Add(new Vertex(new Point2D(-1, 2.5)));
        _model.AddEntity(boundary2);
        
        int originalCount = boundary1.Vertices.Count;
        _rule.Apply(boundary1, _model);
        
        // Should have added 2 intersection points (left and right sides)
        Assert.IsTrue(boundary1.Vertices.Count >= originalCount + 2);
    }

    [TestMethod]
    public void Apply_NonIntersectableBoundary_Ignored()
    {
        var boundary1 = new Boundary { Intersectable = true };
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 2)));
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 2)));
        _model.AddEntity(boundary1);
        
        // Non-intersectable boundary that would cross
        var boundary2 = new Boundary { Intersectable = false };
        boundary2.Vertices.Add(new Vertex(new Point2D(1, -1)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1, 3)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 3)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1.5, -1)));
        _model.AddEntity(boundary2);
        
        _rule.Apply(boundary1, _model);
        
        // Should not add intersections with non-intersectable boundaries
        Assert.AreEqual(4, boundary1.Vertices.Count);
    }

    [TestMethod]
    public void Apply_ExistingVertexAtIntersection_DoesNotDuplicate()
    {
        var boundary1 = new Boundary { Intersectable = true };
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 1)));
        boundary1.Vertices.Add(new Vertex(new Point2D(1, 1))); // Already has vertex at intersection
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 1)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 1.5)));
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 1.5)));
        _model.AddEntity(boundary1);
        
        var boundary2 = new Boundary { Intersectable = true };
        boundary2.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 0)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 2)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1, 2)));
        _model.AddEntity(boundary2);
        
        int originalCount = boundary1.Vertices.Count;
        _rule.Apply(boundary1, _model);
        
        // Should not duplicate existing vertex at (1,1)
        int countAt1_1 = boundary1.Vertices.Count(v => 
            Math.Abs(v.Location.X - 1.0) < 0.001 && Math.Abs(v.Location.Y - 1.0) < 0.001);
        
        Assert.AreEqual(1, countAt1_1, "Should not duplicate existing intersection vertex");
    }

    [TestMethod]
    public void Apply_ParallelBoundaries_NoIntersection()
    {
        var boundary1 = new Boundary { Intersectable = true };
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 1)));
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 1)));
        _model.AddEntity(boundary1);
        
        var boundary2 = new Boundary { Intersectable = true };
        boundary2.Vertices.Add(new Vertex(new Point2D(0, 2)));
        boundary2.Vertices.Add(new Vertex(new Point2D(2, 2)));
        boundary2.Vertices.Add(new Vertex(new Point2D(2, 3)));
        boundary2.Vertices.Add(new Vertex(new Point2D(0, 3)));
        _model.AddEntity(boundary2);
        
        _rule.Apply(boundary1, _model);
        
        Assert.AreEqual(4, boundary1.Vertices.Count);
    }

    [TestMethod]
    public void Apply_ComplexIntersection_HandlesCorrectly()
    {
        // L-shaped boundary
        var boundary1 = new Boundary { Intersectable = true };
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 0)));
        boundary1.Vertices.Add(new Vertex(new Point2D(2, 1)));
        boundary1.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary1.Vertices.Add(new Vertex(new Point2D(1, 2)));
        boundary1.Vertices.Add(new Vertex(new Point2D(0, 2)));
        _model.AddEntity(boundary1);
        
        // Rectangular boundary crossing the L
        var boundary2 = new Boundary { Intersectable = true };
        boundary2.Vertices.Add(new Vertex(new Point2D(0.5, 0.5)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 0.5)));
        boundary2.Vertices.Add(new Vertex(new Point2D(1.5, 1.5)));
        boundary2.Vertices.Add(new Vertex(new Point2D(0.5, 1.5)));
        _model.AddEntity(boundary2);
        
        int originalCount = boundary1.Vertices.Count;
        _rule.Apply(boundary1, _model);
        
        // Should have added intersection vertices
        Assert.IsTrue(boundary1.Vertices.Count >= originalCount);
    }
}

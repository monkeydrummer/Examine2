using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
using CAD2DModel.Services.Implementations.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CAD2DModel.Tests.Services.Rules;

[TestClass]
public class RemoveDuplicateVerticesRuleTests
{
    private RemoveDuplicateVerticesRule _rule = null!;
    private IGeometryModel _model = null!;

    [TestInitialize]
    public void Setup()
    {
        _rule = new RemoveDuplicateVerticesRule(tolerance: 0.001);
        _model = new GeometryModel();
    }

    [TestMethod]
    public void Priority_ShouldBe50()
    {
        Assert.AreEqual(50, _rule.Priority);
    }

    [TestMethod]
    public void Name_ShouldBeCorrect()
    {
        Assert.AreEqual("Remove Duplicate Vertices", _rule.Name);
    }

    [TestMethod]
    public void Apply_PolylineWithNoDuplicates_NoChange()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(2, 0)));
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(3, polyline.Vertices.Count);
    }

    [TestMethod]
    public void Apply_PolylineWithConsecutiveDuplicates_RemovesDuplicate()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0))); // Exact duplicate
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(2, polyline.Vertices.Count);
        Assert.AreEqual(0, polyline.Vertices[0].Location.X, 0.0001);
        Assert.AreEqual(1, polyline.Vertices[1].Location.X, 0.0001);
    }

    [TestMethod]
    public void Apply_PolylineWithNearDuplicates_RemovesDuplicate()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0.0005, 0.0005))); // Within tolerance
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(2, polyline.Vertices.Count);
    }

    [TestMethod]
    public void Apply_PolylineWithMultipleDuplicates_RemovesAll()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(2, polyline.Vertices.Count);
    }

    [TestMethod]
    public void Apply_BoundaryWithNoDuplicates_NoChange()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 1)));
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(4, boundary.Vertices.Count);
    }

    [TestMethod]
    public void Apply_BoundaryWithConsecutiveDuplicates_RemovesDuplicate()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0))); // Duplicate
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(3, boundary.Vertices.Count);
    }

    [TestMethod]
    public void Apply_BoundaryWithClosingDuplicate_RemovesDuplicate()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0))); // Same as first (closing)
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(3, boundary.Vertices.Count);
    }

    [TestMethod]
    public void Apply_BoundaryMaintainsMinimumThreeVertices()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(boundary, _model);
        
        // Should keep at least 3 vertices even if they would be duplicates
        Assert.IsTrue(boundary.Vertices.Count >= 3);
    }

    [TestMethod]
    public void Apply_VerticesJustOutsideTolerance_NotRemoved()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0.002, 0))); // Outside tolerance of 0.001
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(3, polyline.Vertices.Count);
    }
}

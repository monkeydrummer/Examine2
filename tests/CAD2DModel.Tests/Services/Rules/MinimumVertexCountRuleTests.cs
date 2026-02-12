using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
using CAD2DModel.Services.Implementations.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CAD2DModel.Tests.Services.Rules;

[TestClass]
public class MinimumVertexCountRuleTests
{
    private MinimumVertexCountRule _rule = null!;
    private IGeometryModel _model = null!;

    [TestInitialize]
    public void Setup()
    {
        _rule = new MinimumVertexCountRule();
        _model = new GeometryModel();
    }

    [TestMethod]
    public void Priority_ShouldBe10()
    {
        Assert.AreEqual(10, _rule.Priority);
    }

    [TestMethod]
    public void Name_ShouldBeCorrect()
    {
        Assert.AreEqual("Minimum Vertex Count", _rule.Name);
    }

    [TestMethod]
    public void AppliesTo_Polyline_ReturnsTrue()
    {
        var polyline = new Polyline();
        Assert.IsTrue(_rule.AppliesTo(polyline));
    }

    [TestMethod]
    public void AppliesTo_Boundary_ReturnsTrue()
    {
        var boundary = new Boundary();
        Assert.IsTrue(_rule.AppliesTo(boundary));
    }

    [TestMethod]
    public void Apply_PolylineWithZeroVertices_RemovesEntity()
    {
        var polyline = new Polyline();
        _model.AddEntity(polyline);
        
        Assert.AreEqual(1, _model.Entities.Count());
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(0, _model.Entities.Count());
    }

    [TestMethod]
    public void Apply_PolylineWithOneVertex_RemovesEntity()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        _model.AddEntity(polyline);
        
        Assert.AreEqual(1, _model.Entities.Count());
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(0, _model.Entities.Count());
    }

    [TestMethod]
    public void Apply_PolylineWithTwoVertices_KeepsEntity()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(1, 1)));
        _model.AddEntity(polyline);
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(1, _model.Entities.Count());
        Assert.AreEqual(2, polyline.Vertices.Count);
    }

    [TestMethod]
    public void Apply_BoundaryWithZeroVertices_RemovesEntity()
    {
        var boundary = new Boundary();
        _model.AddEntity(boundary);
        
        Assert.AreEqual(1, _model.Entities.Count());
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(0, _model.Entities.Count());
    }

    [TestMethod]
    public void Apply_BoundaryWithTwoVertices_RemovesEntity()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        _model.AddEntity(boundary);
        
        Assert.AreEqual(1, _model.Entities.Count());
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(0, _model.Entities.Count());
    }

    [TestMethod]
    public void Apply_BoundaryWithThreeVertices_KeepsEntity()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 1)));
        _model.AddEntity(boundary);
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(1, _model.Entities.Count());
        Assert.AreEqual(3, boundary.Vertices.Count);
    }
}

using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
using CAD2DModel.Services.Implementations.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CAD2DModel.Tests.Services.Rules;

[TestClass]
public class CounterClockwiseWindingRuleTests
{
    private CounterClockwiseWindingRule _rule = null!;
    private IGeometryModel _model = null!;

    [TestInitialize]
    public void Setup()
    {
        _rule = new CounterClockwiseWindingRule();
        _model = new GeometryModel();
    }

    [TestMethod]
    public void Priority_ShouldBe200()
    {
        Assert.AreEqual(200, _rule.Priority);
    }

    [TestMethod]
    public void Name_ShouldBeCorrect()
    {
        Assert.AreEqual("Counter-Clockwise Winding", _rule.Name);
    }

    [TestMethod]
    public void AppliesTo_Boundary_ReturnsTrue()
    {
        var boundary = new Boundary();
        Assert.IsTrue(_rule.AppliesTo(boundary));
    }

    [TestMethod]
    public void AppliesTo_Polyline_ReturnsFalse()
    {
        var polyline = new Polyline();
        Assert.IsFalse(_rule.AppliesTo(polyline));
    }

    [TestMethod]
    public void Apply_CounterClockwiseBoundary_NoChange()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 1)));
        
        var originalOrder = boundary.Vertices.Select(v => v.Location).ToList();
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(4, boundary.Vertices.Count);
        for (int i = 0; i < 4; i++)
        {
            Assert.AreEqual(originalOrder[i].X, boundary.Vertices[i].Location.X, 0.0001);
            Assert.AreEqual(originalOrder[i].Y, boundary.Vertices[i].Location.Y, 0.0001);
        }
    }

    [TestMethod]
    public void Apply_ClockwiseBoundary_ReversesOrder()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(4, boundary.Vertices.Count);
        Assert.AreEqual(0, boundary.Vertices[0].Location.X, 0.0001);
        Assert.AreEqual(0, boundary.Vertices[0].Location.Y, 0.0001);
        Assert.AreEqual(1, boundary.Vertices[1].Location.X, 0.0001);
        Assert.AreEqual(0, boundary.Vertices[1].Location.Y, 0.0001);
    }

    [TestMethod]
    public void Apply_SimpleTriangleCCW_NoChange()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(0.5, 1)));
        
        var originalOrder = boundary.Vertices.Select(v => v.Location).ToList();
        
        _rule.Apply(boundary, _model);
        
        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(originalOrder[i].X, boundary.Vertices[i].Location.X, 0.0001);
            Assert.AreEqual(originalOrder[i].Y, boundary.Vertices[i].Location.Y, 0.0001);
        }
    }

    [TestMethod]
    public void Apply_SimpleTriangleCW_Reverses()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(0.5, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(0, boundary.Vertices[0].Location.X, 0.0001);
        Assert.AreEqual(0, boundary.Vertices[0].Location.Y, 0.0001);
        Assert.AreEqual(1, boundary.Vertices[1].Location.X, 0.0001);
        Assert.AreEqual(0, boundary.Vertices[1].Location.Y, 0.0001);
        Assert.AreEqual(0.5, boundary.Vertices[2].Location.X, 0.0001);
        Assert.AreEqual(1, boundary.Vertices[2].Location.Y, 0.0001);
    }

    [TestMethod]
    public void Apply_ComplexPolygonCCW_NoChange()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(2, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(2, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 2)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 2)));
        
        var originalOrder = boundary.Vertices.Select(v => v.Location).ToList();
        
        _rule.Apply(boundary, _model);
        
        for (int i = 0; i < 6; i++)
        {
            Assert.AreEqual(originalOrder[i].X, boundary.Vertices[i].Location.X, 0.0001);
            Assert.AreEqual(originalOrder[i].Y, boundary.Vertices[i].Location.Y, 0.0001);
        }
    }

    [TestMethod]
    public void Apply_ComplexPolygonCW_Reverses()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 2)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 2)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(2, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(2, 0)));
        
        _rule.Apply(boundary, _model);
        
        // After reversal, should start at same point but go opposite direction
        Assert.AreEqual(0, boundary.Vertices[0].Location.X, 0.0001);
        Assert.AreEqual(0, boundary.Vertices[0].Location.Y, 0.0001);
        Assert.AreEqual(2, boundary.Vertices[1].Location.X, 0.0001);
        Assert.AreEqual(0, boundary.Vertices[1].Location.Y, 0.0001);
    }
}

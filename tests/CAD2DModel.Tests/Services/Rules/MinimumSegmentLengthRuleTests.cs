using CAD2DModel.Geometry;
using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
using CAD2DModel.Services.Implementations.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CAD2DModel.Tests.Services.Rules;

[TestClass]
public class MinimumSegmentLengthRuleTests
{
    private MinimumSegmentLengthRule _rule = null!;
    private IGeometryModel _model = null!;

    [TestInitialize]
    public void Setup()
    {
        _rule = new MinimumSegmentLengthRule(minimumLength: 0.01);
        _model = new GeometryModel();
    }

    [TestMethod]
    public void Priority_ShouldBe100()
    {
        Assert.AreEqual(100, _rule.Priority);
    }

    [TestMethod]
    public void Name_ShouldBeCorrect()
    {
        Assert.AreEqual("Minimum Segment Length", _rule.Name);
    }

    [TestMethod]
    public void Apply_PolylineWithAllValidSegments_NoChange()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(2, 0)));
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(3, polyline.Vertices.Count);
    }

    [TestMethod]
    public void Apply_PolylineWithShortSegment_RemovesVertex()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0.005, 0))); // Too short
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(2, polyline.Vertices.Count);
        Assert.AreEqual(0, polyline.Vertices[0].Location.X, 0.0001);
        Assert.AreEqual(1, polyline.Vertices[1].Location.X, 0.0001);
    }

    [TestMethod]
    public void Apply_PolylineWithMultipleShortSegments_RemovesAllShort()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0.005, 0))); // Too short
        polyline.Vertices.Add(new Vertex(new Point2D(0.008, 0))); // Too short
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(2, polyline.Vertices.Count);
    }

    [TestMethod]
    public void Apply_BoundaryWithAllValidSegments_NoChange()
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
    public void Apply_BoundaryWithShortSegment_RemovesVertex()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1.005, 0.005))); // Too close to previous
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(3, boundary.Vertices.Count);
    }

    [TestMethod]
    public void Apply_BoundaryWithShortClosingSegment_RemovesVertex()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(1, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(0.005, 0.005))); // Too close to first
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(3, boundary.Vertices.Count);
    }

    [TestMethod]
    public void Apply_BoundaryMaintainsMinimumThreeVertices()
    {
        var boundary = new Boundary();
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(0.005, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(0.008, 0.005)));
        
        _rule.Apply(boundary, _model);
        
        Assert.AreEqual(3, boundary.Vertices.Count);
    }

    [TestMethod]
    public void Apply_SegmentAtExactMinimumLength_NotRemoved()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0.01, 0))); // Exactly at minimum
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(3, polyline.Vertices.Count);
    }

    [TestMethod]
    public void Apply_SegmentJustBelowMinimum_IsRemoved()
    {
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0.009, 0))); // Just below minimum
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _rule.Apply(polyline, _model);
        
        Assert.AreEqual(2, polyline.Vertices.Count);
    }
}

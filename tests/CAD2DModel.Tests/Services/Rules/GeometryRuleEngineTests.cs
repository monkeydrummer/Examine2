using CAD2DModel.Services;
using CAD2DModel.Services.Implementations;
using CAD2DModel.Services.Implementations.Rules;
using CAD2DModel.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CAD2DModel.Tests.Services.Rules;

[TestClass]
public class GeometryRuleEngineTests
{
    private GeometryRuleEngine _engine = null!;
    private IGeometryModel _model = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new GeometryRuleEngine();
        _model = new GeometryModel();
    }

    [TestMethod]
    public void RegisterRule_AddsRuleToEngine()
    {
        var rule = new MinimumVertexCountRule();
        
        _engine.RegisterRule(rule);
        
        var rules = _engine.GetRegisteredRules();
        Assert.AreEqual(1, rules.Count);
        Assert.AreEqual(rule, rules[0]);
    }

    [TestMethod]
    public void RegisterRule_SortsByPriority()
    {
        var rule1 = new MinimumSegmentLengthRule(); // Priority 100
        var rule2 = new MinimumVertexCountRule(); // Priority 10
        var rule3 = new CounterClockwiseWindingRule(); // Priority 200
        
        _engine.RegisterRule(rule1);
        _engine.RegisterRule(rule2);
        _engine.RegisterRule(rule3);
        
        var rules = _engine.GetRegisteredRules();
        Assert.AreEqual(10, rules[0].Priority);
        Assert.AreEqual(100, rules[1].Priority);
        Assert.AreEqual(200, rules[2].Priority);
    }

    [TestMethod]
    public void RegisterRule_DuplicateRule_IgnoresSecondAdd()
    {
        var rule = new MinimumVertexCountRule();
        
        _engine.RegisterRule(rule);
        _engine.RegisterRule(rule);
        
        var rules = _engine.GetRegisteredRules();
        Assert.AreEqual(1, rules.Count);
    }

    [TestMethod]
    public void UnregisterRule_RemovesRule()
    {
        var rule = new MinimumVertexCountRule();
        _engine.RegisterRule(rule);
        
        _engine.UnregisterRule(rule);
        
        var rules = _engine.GetRegisteredRules();
        Assert.AreEqual(0, rules.Count);
    }

    [TestMethod]
    public void ApplyRules_AppliesApplicableRules()
    {
        _engine.RegisterRule(new RemoveDuplicateVerticesRule(0.001));
        
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0))); // Duplicate
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _engine.ApplyRules(polyline, _model);
        
        Assert.AreEqual(2, polyline.Vertices.Count);
    }

    [TestMethod]
    public void ApplyRules_SkipsNonApplicableRules()
    {
        _engine.RegisterRule(new CounterClockwiseWindingRule()); // Only applies to boundaries
        
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        // Should not throw or modify
        _engine.ApplyRules(polyline, _model);
        
        Assert.AreEqual(2, polyline.Vertices.Count);
    }

    [TestMethod]
    public void ApplyRules_WhenDisabled_DoesNotApplyRules()
    {
        _engine.RegisterRule(new RemoveDuplicateVerticesRule(0.001));
        _engine.Enabled = false;
        
        var polyline = new Polyline();
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline.Vertices.Add(new Vertex(new Point2D(0, 0))); // Duplicate
        polyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        
        _engine.ApplyRules(polyline, _model);
        
        Assert.AreEqual(3, polyline.Vertices.Count); // No change
    }

    [TestMethod]
    public void ApplyAllRules_AppliesRulesToAllEntities()
    {
        _engine.RegisterRule(new RemoveDuplicateVerticesRule(0.001));
        
        var polyline1 = new Polyline();
        polyline1.Vertices.Add(new Vertex(new Point2D(0, 0)));
        polyline1.Vertices.Add(new Vertex(new Point2D(0, 0))); // Duplicate
        polyline1.Vertices.Add(new Vertex(new Point2D(1, 0)));
        _model.AddEntity(polyline1);
        
        var polyline2 = new Polyline();
        polyline2.Vertices.Add(new Vertex(new Point2D(5, 5)));
        polyline2.Vertices.Add(new Vertex(new Point2D(5, 5))); // Duplicate
        polyline2.Vertices.Add(new Vertex(new Point2D(6, 5)));
        _model.AddEntity(polyline2);
        
        _engine.ApplyAllRules(_model);
        
        Assert.AreEqual(2, polyline1.Vertices.Count);
        Assert.AreEqual(2, polyline2.Vertices.Count);
    }

    [TestMethod]
    public void ApplyAllRules_RulesExecuteInPriorityOrder()
    {
        // Add rules in reverse priority order
        _engine.RegisterRule(new CounterClockwiseWindingRule()); // Priority 200
        _engine.RegisterRule(new RemoveDuplicateVerticesRule(0.001)); // Priority 50
        _engine.RegisterRule(new MinimumVertexCountRule()); // Priority 10
        
        var boundary = new Boundary();
        // Clockwise with duplicates
        boundary.Vertices.Add(new Vertex(new Point2D(0, 0)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 1)));
        boundary.Vertices.Add(new Vertex(new Point2D(0, 1))); // Duplicate
        boundary.Vertices.Add(new Vertex(new Point2D(1, 0)));
        _model.AddEntity(boundary);
        
        _engine.ApplyAllRules(_model);
        
        // Should have removed duplicates (priority 50) then reversed winding (priority 200)
        Assert.AreEqual(3, boundary.Vertices.Count);
        // Check it's now CCW (first two points should be 0,0 then 1,0)
        Assert.AreEqual(0, boundary.Vertices[0].Location.X, 0.0001);
        Assert.AreEqual(0, boundary.Vertices[0].Location.Y, 0.0001);
        Assert.AreEqual(1, boundary.Vertices[1].Location.X, 0.0001);
        Assert.AreEqual(0, boundary.Vertices[1].Location.Y, 0.0001);
    }

    [TestMethod]
    public void ApplyAllRules_CanRemoveEntities()
    {
        _engine.RegisterRule(new MinimumVertexCountRule());
        
        var invalidPolyline = new Polyline();
        invalidPolyline.Vertices.Add(new Vertex(new Point2D(0, 0))); // Only 1 vertex
        _model.AddEntity(invalidPolyline);
        
        var validPolyline = new Polyline();
        validPolyline.Vertices.Add(new Vertex(new Point2D(0, 0)));
        validPolyline.Vertices.Add(new Vertex(new Point2D(1, 0)));
        _model.AddEntity(validPolyline);
        
        Assert.AreEqual(2, _model.Entities.Count());
        
        _engine.ApplyAllRules(_model);
        
        Assert.AreEqual(1, _model.Entities.Count());
        Assert.IsTrue(_model.Entities.Contains(validPolyline));
    }

    [TestMethod]
    public void Enabled_CanBeToggledOnAndOff()
    {
        Assert.IsTrue(_engine.Enabled);
        
        _engine.Enabled = false;
        Assert.IsFalse(_engine.Enabled);
        
        _engine.Enabled = true;
        Assert.IsTrue(_engine.Enabled);
    }
}

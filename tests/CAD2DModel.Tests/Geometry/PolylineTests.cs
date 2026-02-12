using CAD2DModel.Geometry;
using FluentAssertions;

namespace CAD2DModel.Tests.Geometry;

[TestClass]
public class PolylineTests
{
    [TestMethod]
    public void Constructor_ShouldCreateEmptyPolyline()
    {
        // Arrange & Act
        var polyline = new Polyline();
        
        // Assert
        polyline.VertexCount.Should().Be(0);
        polyline.IsClosed.Should().BeFalse();
        polyline.IsVisible.Should().BeTrue();
    }
    
    [TestMethod]
    public void Constructor_WithPoints_ShouldCreatePolylineWithVertices()
    {
        // Arrange
        var points = new[]
        {
            new Point2D(0, 0),
            new Point2D(1, 0),
            new Point2D(1, 1)
        };
        
        // Act
        var polyline = new Polyline(points);
        
        // Assert
        polyline.VertexCount.Should().Be(3);
        polyline.Vertices[0].Location.Should().Be(points[0]);
        polyline.Vertices[1].Location.Should().Be(points[1]);
        polyline.Vertices[2].Location.Should().Be(points[2]);
    }
    
    [TestMethod]
    public void AddVertex_ShouldAddVertexToPolyline()
    {
        // Arrange
        var polyline = new Polyline();
        var point = new Point2D(1, 2);
        
        // Act
        polyline.AddVertex(point);
        
        // Assert
        polyline.VertexCount.Should().Be(1);
        polyline.Vertices[0].Location.Should().Be(point);
    }
    
    [TestMethod]
    public void Length_OpenPolyline_ShouldCalculateCorrectly()
    {
        // Arrange
        var polyline = new Polyline(new[]
        {
            new Point2D(0, 0),
            new Point2D(3, 0),
            new Point2D(3, 4)
        });
        
        // Act
        var length = polyline.Length;
        
        // Assert
        length.Should().BeApproximately(7.0, 1e-10); // 3 + 4
    }
    
    [TestMethod]
    public void Length_ClosedPolyline_ShouldIncludeClosingSegment()
    {
        // Arrange
        var polyline = new Polyline(new[]
        {
            new Point2D(0, 0),
            new Point2D(3, 0),
            new Point2D(3, 4)
        })
        {
            IsClosed = true
        };
        
        // Act
        var length = polyline.Length;
        
        // Assert
        length.Should().BeApproximately(12.0, 1e-10); // 3 + 4 + 5
    }
    
    [TestMethod]
    public void GetBounds_ShouldReturnCorrectBounds()
    {
        // Arrange
        var polyline = new Polyline(new[]
        {
            new Point2D(1, 2),
            new Point2D(5, 3),
            new Point2D(3, 7)
        });
        
        // Act
        var bounds = polyline.GetBounds();
        
        // Assert
        bounds.X.Should().Be(1.0);
        bounds.Y.Should().Be(2.0);
        bounds.Width.Should().Be(4.0);  // 5 - 1
        bounds.Height.Should().Be(5.0); // 7 - 2
    }
    
    [TestMethod]
    public void GetSegmentCount_OpenPolyline_ShouldReturnCorrectCount()
    {
        // Arrange
        var polyline = new Polyline(new[]
        {
            new Point2D(0, 0),
            new Point2D(1, 0),
            new Point2D(1, 1)
        });
        
        // Act
        var count = polyline.GetSegmentCount();
        
        // Assert
        count.Should().Be(2); // 3 vertices = 2 segments (open)
    }
    
    [TestMethod]
    public void GetSegmentCount_ClosedPolyline_ShouldIncludeClosingSegment()
    {
        // Arrange
        var polyline = new Polyline(new[]
        {
            new Point2D(0, 0),
            new Point2D(1, 0),
            new Point2D(1, 1)
        })
        {
            IsClosed = true
        };
        
        // Act
        var count = polyline.GetSegmentCount();
        
        // Assert
        count.Should().Be(3); // 3 vertices = 3 segments (closed)
    }
    
    [TestMethod]
    public void GetSegment_ShouldReturnCorrectSegment()
    {
        // Arrange
        var polyline = new Polyline(new[]
        {
            new Point2D(0, 0),
            new Point2D(1, 0),
            new Point2D(1, 1)
        });
        
        // Act
        var segment = polyline.GetSegment(0);
        
        // Assert
        segment.Start.Should().Be(new Point2D(0, 0));
        segment.End.Should().Be(new Point2D(1, 0));
    }
}

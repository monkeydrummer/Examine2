using System;
using System.Collections.Generic;
using System.Linq;

namespace CAD2DModel.Geometry;

/// <summary>
/// Provides methods for calculating intersections between geometric entities
/// </summary>
public static class IntersectionCalculator
{
    private const double Tolerance = 1e-10;

    /// <summary>
    /// Represents an intersection point with additional context
    /// </summary>
    public class IntersectionPoint
    {
        public Point2D Location { get; }
        public double Parameter1 { get; } // Parameter on first segment (0 to 1)
        public double Parameter2 { get; } // Parameter on second segment (0 to 1)
        public int SegmentIndex1 { get; } // Segment index on first entity
        public int SegmentIndex2 { get; } // Segment index on second entity

        public IntersectionPoint(Point2D location, double param1, double param2, int segIndex1, int segIndex2)
        {
            Location = location;
            Parameter1 = param1;
            Parameter2 = param2;
            SegmentIndex1 = segIndex1;
            SegmentIndex2 = segIndex2;
        }
    }

    /// <summary>
    /// Find intersection between two line segments
    /// </summary>
    public static IntersectionPoint? LineSegmentIntersection(
        Point2D p1, Point2D p2, Point2D p3, Point2D p4,
        bool extendFirst = false, bool extendSecond = false)
    {
        double x1 = p1.X, y1 = p1.Y;
        double x2 = p2.X, y2 = p2.Y;
        double x3 = p3.X, y3 = p3.Y;
        double x4 = p4.X, y4 = p4.Y;

        double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
        
        // Lines are parallel
        if (Math.Abs(denom) < Tolerance)
            return null;

        double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
        double u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;

        // Check if intersection is within segment bounds (or extended if requested)
        bool tValid = extendFirst || (t >= -Tolerance && t <= 1.0 + Tolerance);
        bool uValid = extendSecond || (u >= -Tolerance && u <= 1.0 + Tolerance);

        if (tValid && uValid)
        {
            double x = x1 + t * (x2 - x1);
            double y = y1 + t * (y2 - y1);
            return new IntersectionPoint(new Point2D(x, y), t, u, 0, 0);
        }

        return null;
    }

    /// <summary>
    /// Find all intersections between two polylines
    /// </summary>
    public static List<IntersectionPoint> PolylineIntersections(
        Polyline poly1, Polyline poly2,
        bool extendFirst = false, bool extendSecond = false)
    {
        var intersections = new List<IntersectionPoint>();
        var vertices1 = poly1.Vertices.ToList();
        var vertices2 = poly2.Vertices.ToList();

        for (int i = 0; i < vertices1.Count - 1; i++)
        {
            for (int j = 0; j < vertices2.Count - 1; j++)
            {
                var intersection = LineSegmentIntersection(
                    vertices1[i].Location, vertices1[i + 1].Location,
                    vertices2[j].Location, vertices2[j + 1].Location,
                    extendFirst, extendSecond);

                if (intersection != null)
                {
                    intersections.Add(new IntersectionPoint(
                        intersection.Location,
                        intersection.Parameter1,
                        intersection.Parameter2,
                        i, j));
                }
            }
        }

        return intersections;
    }

    /// <summary>
    /// Find all intersections between a polyline and a boundary
    /// </summary>
    public static List<IntersectionPoint> PolylineBoundaryIntersections(
        Polyline polyline, Boundary boundary,
        bool extendPolyline = false, bool extendBoundary = false)
    {
        var intersections = new List<IntersectionPoint>();
        var polyVertices = polyline.Vertices.ToList();
        var boundVertices = boundary.Vertices.ToList();

        for (int i = 0; i < polyVertices.Count - 1; i++)
        {
            for (int j = 0; j < boundVertices.Count; j++)
            {
                int nextJ = (j + 1) % boundVertices.Count;
                var intersection = LineSegmentIntersection(
                    polyVertices[i].Location, polyVertices[i + 1].Location,
                    boundVertices[j].Location, boundVertices[nextJ].Location,
                    extendPolyline, extendBoundary);

                if (intersection != null)
                {
                    intersections.Add(new IntersectionPoint(
                        intersection.Location,
                        intersection.Parameter1,
                        intersection.Parameter2,
                        i, j));
                }
            }
        }

        return intersections;
    }

    /// <summary>
    /// Find all intersections between two boundaries
    /// </summary>
    public static List<IntersectionPoint> BoundaryIntersections(
        Boundary bound1, Boundary bound2,
        bool extendFirst = false, bool extendSecond = false)
    {
        var intersections = new List<IntersectionPoint>();
        var vertices1 = bound1.Vertices.ToList();
        var vertices2 = bound2.Vertices.ToList();

        for (int i = 0; i < vertices1.Count; i++)
        {
            int nextI = (i + 1) % vertices1.Count;
            
            for (int j = 0; j < vertices2.Count; j++)
            {
                int nextJ = (j + 1) % vertices2.Count;
                
                var intersection = LineSegmentIntersection(
                    vertices1[i].Location, vertices1[nextI].Location,
                    vertices2[j].Location, vertices2[nextJ].Location,
                    extendFirst, extendSecond);

                if (intersection != null)
                {
                    intersections.Add(new IntersectionPoint(
                        intersection.Location,
                        intersection.Parameter1,
                        intersection.Parameter2,
                        i, j));
                }
            }
        }

        return intersections;
    }

    /// <summary>
    /// Find intersections between any two entities
    /// </summary>
    public static List<IntersectionPoint> EntityIntersections(
        IEntity entity1, IEntity entity2,
        bool extendFirst = false, bool extendSecond = false)
    {
        if (entity1 is Polyline p1 && entity2 is Polyline p2)
        {
            return PolylineIntersections(p1, p2, extendFirst, extendSecond);
        }
        else if (entity1 is Polyline p && entity2 is Boundary b)
        {
            return PolylineBoundaryIntersections(p, b, extendFirst, extendSecond);
        }
        else if (entity1 is Boundary b1 && entity2 is Polyline p3)
        {
            return PolylineBoundaryIntersections(p3, b1, extendSecond, extendFirst)
                .Select(i => new IntersectionPoint(i.Location, i.Parameter2, i.Parameter1, i.SegmentIndex2, i.SegmentIndex1))
                .ToList();
        }
        else if (entity1 is Boundary b2 && entity2 is Boundary b3)
        {
            return BoundaryIntersections(b2, b3, extendFirst, extendSecond);
        }
        
        return new List<IntersectionPoint>();
    }

    /// <summary>
    /// Calculate distance from point to line segment
    /// </summary>
    public static double DistanceToSegment(Point2D point, Point2D segmentStart, Point2D segmentEnd, out double parameter)
    {
        double dx = segmentEnd.X - segmentStart.X;
        double dy = segmentEnd.Y - segmentStart.Y;
        double lengthSquared = dx * dx + dy * dy;

        if (lengthSquared < Tolerance)
        {
            // Segment is a point
            parameter = 0;
            return point.DistanceTo(segmentStart);
        }

        // Calculate parameter along segment (0 to 1)
        parameter = ((point.X - segmentStart.X) * dx + (point.Y - segmentStart.Y) * dy) / lengthSquared;
        parameter = Math.Max(0, Math.Min(1, parameter));

        // Calculate closest point on segment
        double closestX = segmentStart.X + parameter * dx;
        double closestY = segmentStart.Y + parameter * dy;
        Point2D closest = new Point2D(closestX, closestY);

        return point.DistanceTo(closest);
    }
}

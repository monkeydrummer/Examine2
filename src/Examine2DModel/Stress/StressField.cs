using CAD2DModel.Geometry;

namespace Examine2DModel.Stress;

/// <summary>
/// Stress grid for evaluating field results
/// </summary>
public class StressGrid
{
    public Rect2D Bounds { get; set; }
    public int XPoints { get; set; }
    public int YPoints { get; set; }
    public int PointCount => XPoints * YPoints;
    
    public Point2D GetPoint(int index)
    {
        int row = index / XPoints;
        int col = index % XPoints;
        
        double x = Bounds.X + (col / (double)(XPoints - 1)) * Bounds.Width;
        double y = Bounds.Y + (row / (double)(YPoints - 1)) * Bounds.Height;
        
        return new Point2D(x, y);
    }
    
    public static StressGrid CreateUniform(Rect2D bounds, int xPoints, int yPoints)
    {
        return new StressGrid
        {
            Bounds = bounds,
            XPoints = xPoints,
            YPoints = yPoints
        };
    }
}

/// <summary>
/// Stress field results
/// </summary>
public class StressField
{
    public StressGrid Grid { get; init; }
    public double[] Sigma1 { get; init; } // Major principal stress
    public double[] Sigma3 { get; init; } // Minor principal stress
    public double[] Theta { get; init; }  // Principal stress angle
    public Vector2D[] Displacements { get; init; }
    
    public StressField(StressGrid grid)
    {
        Grid = grid;
        int count = grid.PointCount;
        Sigma1 = new double[count];
        Sigma3 = new double[count];
        Theta = new double[count];
        Displacements = new Vector2D[count];
    }
}

/// <summary>
/// Displacement field results
/// </summary>
public class DisplacementField
{
    public StressGrid Grid { get; init; }
    public Vector2D[] Displacements { get; init; }
    
    public DisplacementField(StressGrid grid)
    {
        Grid = grid;
        Displacements = new Vector2D[grid.PointCount];
    }
}

/// <summary>
/// Strength factor field results
/// </summary>
public class StrengthFactorField
{
    public StressGrid Grid { get; init; }
    public double[] StrengthFactors { get; init; }
    
    public StrengthFactorField(StressGrid grid)
    {
        Grid = grid;
        StrengthFactors = new double[grid.PointCount];
    }
}

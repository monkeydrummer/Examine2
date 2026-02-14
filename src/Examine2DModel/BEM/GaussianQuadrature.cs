namespace Examine2DModel.BEM;

/// <summary>
/// Pre-computed Gaussian quadrature points and weights for numerical integration
/// Performance optimization: eliminates runtime computation of Gauss points
/// </summary>
public static class GaussianQuadrature
{
    /// <summary>
    /// Gaussian quadrature data (points and weights)
    /// </summary>
    public class QuadratureData
    {
        public double[] Points { get; }
        public double[] Weights { get; }
        public int Order { get; }

        public QuadratureData(double[] points, double[] weights)
        {
            Points = points;
            Weights = weights;
            Order = points.Length;
        }
    }

    /// <summary>
    /// 3-point Gauss quadrature (for far field)
    /// Used when distance > 6 * element_length
    /// </summary>
    private static readonly QuadratureData Order3 = new QuadratureData(
        points: new[]
        {
            0.774596669,   // sqrt(0.6)
            0.0,
            -0.774596669
        },
        weights: new[]
        {
            0.555555556,   // 5/9
            0.888888889,   // 8/9
            0.555555556    // 5/9
        }
    );

    /// <summary>
    /// 5-point Gauss quadrature (for medium distance)
    /// Used when 3 * element_length < distance <= 6 * element_length
    /// </summary>
    private static readonly QuadratureData Order5 = new QuadratureData(
        points: new[]
        {
            0.906179845938664,
            0.538469310105683,
            0.0,
            -0.538469310105683,
            -0.906179845938664
        },
        weights: new[]
        {
            0.236926885056189,
            0.478628670499366,
            0.568888888888889,
            0.478628670499366,
            0.236926885056189
        }
    );

    /// <summary>
    /// 10-point Gauss quadrature (for near field)
    /// Used when 1.5 * element_length < distance <= 3 * element_length
    /// </summary>
    private static readonly QuadratureData Order10 = new QuadratureData(
        points: new[]
        {
            0.973906528517172,
            0.865063366688985,
            0.679409568299024,
            0.433395394129247,
            0.148874338981631,
            -0.148874338981631,
            -0.433395394129247,
            -0.679409568299024,
            -0.865063366688985,
            -0.973906528517172
        },
        weights: new[]
        {
            0.066671344308688,
            0.149451349150581,
            0.219086362515982,
            0.269266719309996,
            0.295524224714753,
            0.295524224714753,
            0.269266719309996,
            0.219086362515982,
            0.149451349150581,
            0.066671344308688
        }
    );

    /// <summary>
    /// 15-point Gauss quadrature (for very near field)
    /// Used when distance <= 1.5 * element_length
    /// Provides highest accuracy for nearly singular integrals
    /// </summary>
    private static readonly QuadratureData Order15 = new QuadratureData(
        points: new[]
        {
            0.987992518020485,
            0.937273392400706,
            0.848206583410427,
            0.724417731360170,
            0.570972172608539,
            0.394151347077563,
            0.201194093997435,
            0.0,
            -0.201194093997435,
            -0.394151347077563,
            -0.570972172608539,
            -0.724417731360170,
            -0.848206583410427,
            -0.937273392400706,
            -0.987992518020485
        },
        weights: new[]
        {
            0.030753241996117,
            0.070366047488108,
            0.107159220467172,
            0.139570677926154,
            0.166269205816994,
            0.186161000115562,
            0.198431485327111,
            0.202578214925561,
            0.198431485327111,
            0.186161000115562,
            0.166269205816994,
            0.139570677926154,
            0.107159220467172,
            0.070366047488108,
            0.030753241996117
        }
    );

    /// <summary>
    /// Get appropriate quadrature based on distance from field point to element
    /// Adaptive integration order for optimal accuracy and performance
    /// </summary>
    /// <param name="fieldX">Field point X coordinate</param>
    /// <param name="fieldY">Field point Y coordinate</param>
    /// <param name="elementMidX">Element midpoint X coordinate</param>
    /// <param name="elementMidY">Element midpoint Y coordinate</param>
    /// <param name="elementLength">Element length</param>
    /// <returns>Appropriate quadrature data</returns>
    public static QuadratureData GetQuadrature(double fieldX, double fieldY, 
        double elementMidX, double elementMidY, double elementLength)
    {
        // Calculate distance from field point to element midpoint
        double dx = fieldX - elementMidX;
        double dy = fieldY - elementMidY;
        double distanceSquared = dx * dx + dy * dy;
        
        // Use distance-based selection for optimal accuracy vs performance
        // These thresholds match the original C++ logic in npoint()
        double len2 = 2.0 * elementLength;
        
        if (distanceSquared <= 4.0 * len2 * len2) // r² <= (2*2L)²
        {
            return Order15; // Very near field - highest accuracy
        }
        else if (distanceSquared <= 9.0 * len2 * len2) // r² <= (3*2L)²
        {
            return Order10; // Near field
        }
        else if (distanceSquared <= 36.0 * len2 * len2) // r² <= (6*2L)²
        {
            return Order5; // Medium distance
        }
        else
        {
            return Order3; // Far field - lowest order sufficient
        }
    }

    /// <summary>
    /// Get quadrature by explicit order (for testing or specific requirements)
    /// </summary>
    /// <param name="order">Quadrature order (3, 5, 10, or 15)</param>
    /// <returns>Quadrature data</returns>
    /// <exception cref="ArgumentException">If unsupported order is requested</exception>
    public static QuadratureData GetQuadratureByOrder(int order)
    {
        return order switch
        {
            3 => Order3,
            5 => Order5,
            10 => Order10,
            15 => Order15,
            _ => throw new ArgumentException($"Unsupported quadrature order: {order}. Supported orders: 3, 5, 10, 15")
        };
    }
}

namespace CAD2DModel.Geometry;

/// <summary>
/// 2D affine transformation matrix
/// </summary>
public class Transform2D
{
    private double _m11, _m12, _m21, _m22, _offsetX, _offsetY;
    
    public Transform2D()
    {
        _m11 = 1; _m12 = 0;
        _m21 = 0; _m22 = 1;
        _offsetX = 0; _offsetY = 0;
    }
    
    public Transform2D(double m11, double m12, double m21, double m22, double offsetX, double offsetY)
    {
        _m11 = m11; _m12 = m12;
        _m21 = m21; _m22 = m22;
        _offsetX = offsetX; _offsetY = offsetY;
    }
    
    public Point2D Transform(Point2D point)
    {
        return new Point2D(
            _m11 * point.X + _m12 * point.Y + _offsetX,
            _m21 * point.X + _m22 * point.Y + _offsetY
        );
    }
    
    public Vector2D TransformVector(Vector2D vector)
    {
        return new Vector2D(
            _m11 * vector.X + _m12 * vector.Y,
            _m21 * vector.X + _m22 * vector.Y
        );
    }
    
    public static Transform2D Identity => new();
    
    public static Transform2D Translation(double dx, double dy)
    {
        return new Transform2D(1, 0, 0, 1, dx, dy);
    }
    
    public static Transform2D Rotation(double angleRadians)
    {
        double cos = Math.Cos(angleRadians);
        double sin = Math.Sin(angleRadians);
        return new Transform2D(cos, -sin, sin, cos, 0, 0);
    }
    
    public static Transform2D Scaling(double scaleX, double scaleY)
    {
        return new Transform2D(scaleX, 0, 0, scaleY, 0, 0);
    }
    
    public static Transform2D Scale(double scale)
    {
        return new Transform2D(scale, 0, 0, scale, 0, 0);
    }
    
    public Transform2D Multiply(Transform2D other)
    {
        return new Transform2D(
            _m11 * other._m11 + _m12 * other._m21,
            _m11 * other._m12 + _m12 * other._m22,
            _m21 * other._m11 + _m22 * other._m21,
            _m21 * other._m12 + _m22 * other._m22,
            _m11 * other._offsetX + _m12 * other._offsetY + _offsetX,
            _m21 * other._offsetX + _m22 * other._offsetY + _offsetY
        );
    }
    
    public Transform2D Inverse()
    {
        double det = _m11 * _m22 - _m12 * _m21;
        if (Math.Abs(det) < 1e-10)
            throw new InvalidOperationException("Transform is not invertible");
        
        double invDet = 1.0 / det;
        
        return new Transform2D(
            _m22 * invDet,
            -_m12 * invDet,
            -_m21 * invDet,
            _m11 * invDet,
            (_m12 * _offsetY - _m22 * _offsetX) * invDet,
            (_m21 * _offsetX - _m11 * _offsetY) * invDet
        );
    }
}

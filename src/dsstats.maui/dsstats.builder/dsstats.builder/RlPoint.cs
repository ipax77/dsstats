namespace dsstats.builder;

public sealed record RlPoint(int X, int Y)
{
    public double DistanceTo(RlPoint other)
    {
        return Math.Sqrt(DistanceSquaredTo(other));
    }

    public int DistanceSquaredTo(RlPoint other)
    {
        int dx = X - other.X;
        int dy = Y - other.Y;
        return dx * dx + dy * dy;
    }

    public static RlPoint Lerp(RlPoint a, RlPoint b, double t)
    {
        return new RlPoint(
            (int)(a.X + (b.X - a.X) * t),
            (int)(a.Y + (b.Y - a.Y) * t)
        );
    }

    public static RlPoint operator +(RlPoint a, RlPoint b) => new(a.X + b.X, a.Y + b.Y);
    public static RlPoint operator -(RlPoint a, RlPoint b) => new(a.X - b.X, a.Y - b.Y);
    public static RlPoint operator /(RlPoint a, int scalar) => new(a.X / scalar, a.Y / scalar);
    public static RlPoint operator *(RlPoint a, double scalar)
        => new((int)(a.X * scalar), (int)(a.Y * scalar));


    public static RlPoint Zero => new(0, 0);
}
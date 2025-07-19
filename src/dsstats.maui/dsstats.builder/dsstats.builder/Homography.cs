using MathNet.Numerics.LinearAlgebra;

namespace dsstats.builder;

public class Homography
{
    private readonly Matrix<double> H;

    public Homography((RlPoint src, RlPoint dst)[] pointPairs)
    {
        var A = Matrix<double>.Build.Dense(8, 8);
        var B = Vector<double>.Build.Dense(8);

        for (int i = 0; i < 4; i++)
        {
            var (src, dst) = pointPairs[i];
            int row = i * 2;

            A[row, 0] = src.X;
            A[row, 1] = src.Y;
            A[row, 2] = 1;
            A[row, 6] = -src.X * dst.X;
            A[row, 7] = -src.Y * dst.X;

            A[row + 1, 3] = src.X;
            A[row + 1, 4] = src.Y;
            A[row + 1, 5] = 1;
            A[row + 1, 6] = -src.X * dst.Y;
            A[row + 1, 7] = -src.Y * dst.Y;

            B[row] = dst.X;
            B[row + 1] = dst.Y;
        }

        var h = A.Solve(B).ToArray(); // returns h1..h8

        H = Matrix<double>.Build.DenseOfArray(new double[,]
        {
            { h[0], h[1], h[2] },
            { h[3], h[4], h[5] },
            { h[6], h[7], 1.0 }
        });
    }

    public RlPoint Transform(RlPoint p)
    {
        return TransformWithOffset(p.X, p.Y);
    }

    public RlPoint TransformToTopLeft(RlPoint p)
    {
        return TransformWithOffset(p.X - 0.5, p.Y + 0.5);
    }

    public RlPoint TransformToTopRight(RlPoint p)
    {
        return TransformWithOffset(p.X + 0.5, p.Y + 0.5);
    }

    public RlPoint TransformToBottomLeft(RlPoint p)
    {
        return TransformWithOffset(p.X - 0.5, p.Y - 0.5);
    }

    public RlPoint TransformToBottomRight(RlPoint p)
    {
        return TransformWithOffset(p.X + 0.5, p.Y - 0.5);
    }

    public RlPoint TransformWithOffset(double x, double y)
    {
        double denom = H[2, 0] * x + H[2, 1] * y + H[2, 2];
        double screenX = (H[0, 0] * x + H[0, 1] * y + H[0, 2]) / denom;
        double screenY = (H[1, 0] * x + H[1, 1] * y + H[1, 2]) / denom;

        return new RlPoint((int)screenX, (int)screenY);
    }
}

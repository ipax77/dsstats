namespace dsstats.shared;

public record UnitmapRequest
{

}


public record Unitmap
{
    public List<PointInfo> Infos { get; set; } = new();
}

public record PointInfo
{
    public Point Point { get; set; } = Point.Zero;
    public Dictionary<string, int> UnitCounts { get; set; } = new();
}

public sealed record NormalizedArea
{
    public Area Area { get; }
    public NormalizedArea(Area area)
    {
        Area = area;
        int translateX = -area.West.X;
        int translateY = -area.West.Y;

        // Translate all points to align WEST with (0, 0)
        South = new Point(area.South.X + translateX, area.South.Y + translateY);
        West = new Point(area.West.X + translateX, area.West.Y + translateY);
        North = new Point(area.North.X + translateX, area.North.Y + translateY);
        East = new Point(area.East.X + translateX, area.East.Y + translateY);


    }

    public Point South { get; init; }
    public Point West { get; init; }
    public Point North { get; init; }
    public Point East { get; init; }

    public Point GetNormalizedPoint(Point areaPoint)
    {
        // Calculate the inverse translation to get the normalized point
        int translateX = -Area.West.X;
        int translateY = -Area.West.Y;

        // Translate the areaPoint to get the normalized point
        int normalizedX = areaPoint.X + translateX;
        int normalizedY = areaPoint.Y + translateY;

        return new Point(normalizedX, normalizedY);
    }
}

public record RotatedArea
{
    private readonly double angle;
    private readonly int offsetX;
    private readonly int offsetY;

    public RotatedArea(Area area)
    {
        Area = area;
        angle = Math.Atan2(Math.Abs(area.West.Y - area.South.Y), Math.Abs(area.West.X - area.South.X));
        // angle = Math.Atan2(area.South.Y - area.West.Y, area.South.X - area.West.X);
        // angle = Math.Atan2(area.West.X - area.South.X, area.West.Y - area.South.Y);

        // Rotate the points counterclockwise around the origin
        Point rotatedSouth = RotatePoint(area.South);
        Point rotatedWest = RotatePoint(area.West);
        Point rotatedNorth = RotatePoint(area.North);
        Point rotatedEast = RotatePoint(area.East);

        // Translate the rectangle so that the new West becomes (0, 0)
        offsetX = -rotatedWest.X;
        offsetY = -rotatedWest.Y;

        South = new Point(rotatedSouth.X + offsetX, rotatedSouth.Y + offsetY);
        West = new Point(rotatedWest.X + offsetX, rotatedWest.Y + offsetY);
        North = new Point(rotatedNorth.X + offsetX, rotatedNorth.Y + offsetY);
        East = new Point(rotatedEast.X + offsetX, rotatedEast.Y + offsetY);
    }

    public Area Area { get; private set; }
    public Point South { get; init; }
    public Point West { get; init; }
    public Point North { get; init; }
    public Point East { get; init; }

    public Point GetNormalizedPoint(Point areaPoint)
    {
        var rotatedPoint = RotatePoint(areaPoint);
        return new(rotatedPoint.X + offsetX, rotatedPoint.Y + offsetY);
    }

    private Point RotatePoint(Point point)
    {
        // Rotate a point counterclockwise around the origin
        double cosAngle = Math.Cos(angle);
        double sinAngle = Math.Sin(angle);

        int x = (int)(cosAngle * point.X - sinAngle * point.Y);
        int y = (int)(sinAngle * point.X + cosAngle * point.Y);

        return new Point(x, y);
    }

    private Point UnRotatePoint(Point rotatedPoint)
    {
        // Rotate a point clockwise around the origin
        double cosAngle = Math.Cos(-angle);
        double sinAngle = Math.Sin(-angle);

        int x = (int)(cosAngle * rotatedPoint.X + sinAngle * rotatedPoint.Y);
        int y = (int)(-sinAngle * rotatedPoint.X + cosAngle * rotatedPoint.Y);

        return new Point(x, y);
    }
}

public record Point(int X, int Y)
{
    public static readonly Point Zero = new(0, 0);
};

public sealed record Area
{
    public Area(Point south, Point west, Point north, Point east)
    {
        South = south;
        West = west;
        North = north;
        East = east;
        rectangleArea = CalculateArea();
    }

    public Point South { get; init; }
    public Point West { get; init; }
    public Point North { get; init; }
    public Point East { get; init; }

    private double rectangleArea;

    private double CalculateArea()
    {
        double area = 0.5 * Math.Abs(
            (North.X * East.Y + East.X * South.Y + South.X * West.Y + West.X * North.Y) -
            (East.X * North.Y + South.X * East.Y + West.X * South.Y + North.X * West.Y)
        );

        return area;
    }

    public bool IsPointInside(Point point)
    {
        var triangle1 = CalculateTriangleArea(North, East, point);
        var triangle2 = CalculateTriangleArea(East, South, point);
        var triangle3 = CalculateTriangleArea(South, West, point);
        var triangle4 = CalculateTriangleArea(West, North, point);

        return Math.Abs(rectangleArea - (triangle1 + triangle2 + triangle3 + triangle4)) < 1e-10;
    }

    private static double CalculateTriangleArea(Point pointA, Point pointB, Point pointC)
    {
        return 0.5 * Math.Abs(
            (pointA.X * (pointB.Y - pointC.Y) + pointB.X * (pointC.Y - pointA.Y) + pointC.X * (pointA.Y - pointB.Y))
        );
    }

    private int Sum(Point a, Point b, Point c)
    {
        return Math.Abs((b.X * a.Y - a.X * b.Y) + (c.X * b.Y - b.X * c.Y) + (a.X * c.X - c.X * a.Y)) / 2;
    }

    public Area MoveTowards(Point targetPoint)
    {
        // Calculate the translation vector
        int deltaX = targetPoint.X - Center().X;
        int deltaY = targetPoint.Y - Center().Y;

        // Translate each vertex of the rectangle
        var south = new Point(South.X + deltaX, South.Y + deltaY);
        var west = new Point(West.X + deltaX, West.Y + deltaY);
        var north = new Point(North.X + deltaX, North.Y + deltaY);
        var east = new Point(East.X + deltaX, East.Y + deltaY);

        return new(south, west, north, east);
    }

    private Point Center()
    {
        double centerX = (South.X + North.X) / 2.0;
        double centerY = (West.Y + East.Y) / 2.0;
        return new Point(Convert.ToInt32(centerX), Convert.ToInt32(centerY));
    }

    public static Point Midpoint(Point A, Point L1, Point L2)
    {
        double midX = (A.X + (L1.X + L2.X) / 2.0) / 2.0;
        double midY = (A.Y + (L1.Y + L2.Y) / 2.0) / 2.0;
        return new Point(Convert.ToInt32(midX), Convert.ToInt32(midY));
    }

    public bool IsPointBetweenParallelLines(Point parallelLinePoint, Point checkPoint, int distance)
    {
        // Calculate vectors
        var vectorNE = new Point(East.X - North.X, East.Y - North.Y);
        var vectorNP = new Point(parallelLinePoint.X - North.X, parallelLinePoint.Y - North.Y);
        var vectorCP = new Point(checkPoint.X - North.X, checkPoint.Y - North.Y);

        // Normalize vectorNE
        double lengthNE = Math.Sqrt(vectorNE.X * vectorNE.X + vectorNE.Y * vectorNE.Y);
        vectorNE = new Point(Convert.ToInt32(vectorNE.X / lengthNE), Convert.ToInt32(vectorNE.Y / lengthNE));

        // Calculate projections
        double projectionCP = (vectorCP.X * vectorNE.X + vectorCP.Y * vectorNE.Y);
        double projectionNP = (vectorNP.X * vectorNE.X + vectorNP.Y * vectorNE.Y);

        // Move the lines toward each other
        projectionCP -= distance;
        projectionNP += distance;

        // Check if checkPoint is between the lines
        return projectionCP >= 0 && projectionCP <= 1 && projectionNP >= 0 && projectionNP <= 1;
    }



    public List<Point> GetAllPointsInside()
    {
        List<Point> pointsInside = new();

        // Calculate the bounding box
        int minX = Math.Min(South.X, Math.Min(West.X, Math.Min(North.X, East.X)));
        int minY = Math.Min(South.Y, Math.Min(West.Y, Math.Min(North.Y, East.Y)));
        int maxX = Math.Max(South.X, Math.Max(West.X, Math.Max(North.X, East.X)));
        int maxY = Math.Max(South.Y, Math.Max(West.Y, Math.Max(North.Y, East.Y)));

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (IsPointInside(new Point(x, y)))
                {
                    pointsInside.Add(new Point(x, y));
                }
            }
        }

        return pointsInside;
    }

    public static readonly Area Zero = new(Point.Zero, Point.Zero, Point.Zero, Point.Zero);
    public static readonly Area SpawnArea1 = new Area(new(173, 147), new(155, 165), new(167, 177), new(185, 159));
    public static readonly Area SpawnArea2 = new Area(new(89, 63), new(71, 81), new(83, 93), new(101, 75));

    public bool Equals(Area? other)
    {
        return (South == other?.South && West == other?.West && North == other?.North && East == other?.East);
    }

    public override int GetHashCode()
    {
        return (South.GetHashCode() + West.GetHashCode() + North.GetHashCode() + East.GetHashCode());
    }
}
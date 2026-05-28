namespace dsstats.play;

public static partial class SpawnPlaybackFactoryNg
{
    private const double PathEpsilon = 0.000001;

    private static int GetPathIndex(
        Dictionary<PathKey, int> pathIndexes,
        List<PathKey> paths,
        PathKey path,
        ref int pathPointCount)
    {
        if (pathIndexes.TryGetValue(path, out int index))
        {
            return index;
        }

        index = paths.Count;
        pathIndexes.Add(path, index);
        paths.Add(path);
        pathPointCount = checked(pathPointCount + path.PointCount);
        return index;
    }

    private static PathKey CreatePath(
        int spawnX,
        int spawnY,
        int? diedX,
        int? diedY,
        int lifetimeGameloops)
    {
        lifetimeGameloops = Math.Max(1, lifetimeGameloops);
        var spawn = new DoublePoint(spawnX, spawnY);
        var routeTarget = new DoublePoint(MapWidth - spawnX, MapHeight - spawnY);

        return diedX is int deathX && diedY is int deathY
            ? CreateDeathPath(spawn, routeTarget, new DoublePoint(deathX, deathY), lifetimeGameloops)
            : CreateFallbackPath(spawn, routeTarget, lifetimeGameloops);
    }

    private static PathKey CreateFallbackPath(DoublePoint spawn, DoublePoint routeTarget, int lifetimeGameloops)
    {
        PathBuilder builder = new();
        builder.Append(spawn, 0);

        double routeDistance = GetDistance(spawn, routeTarget);
        int routeGameloops = GetMinimumSpeedGameloops(routeDistance);
        if (routeGameloops >= lifetimeGameloops)
        {
            builder.Append(MoveTowards(spawn, routeTarget, DefaultSpeedPerGameloop * lifetimeGameloops), lifetimeGameloops);
            return builder.ToPathKey();
        }

        builder.Append(routeTarget, routeGameloops);
        builder.Append(routeTarget, lifetimeGameloops);
        return builder.ToPathKey();
    }

    private static PathKey CreateDeathPath(
        DoublePoint spawn,
        DoublePoint routeTarget,
        DoublePoint death,
        int lifetimeGameloops)
    {
        PathBuilder builder = new();
        builder.Append(spawn, 0);

        var deathLinePoint = GetDeathLinePoint(spawn, routeTarget, death.X + death.Y);
        double lineDistance = GetDistance(spawn, deathLinePoint);
        int lineGameloops = GetMinimumSpeedGameloops(lineDistance);
        if (lineGameloops >= lifetimeGameloops && lifetimeGameloops <= 1)
        {
            builder.Append(death, lifetimeGameloops);
            return builder.ToPathKey();
        }

        if (lineGameloops >= lifetimeGameloops)
        {
            lineGameloops = lifetimeGameloops - 1;
        }

        builder.Append(deathLinePoint, Math.Max(0, lineGameloops));

        double deathDistance = GetDistance(deathLinePoint, death);
        int remainingGameloops = lifetimeGameloops - Math.Max(0, lineGameloops);
        int defaultDeathGameloops = GetMinimumSpeedGameloops(deathDistance);
        if (defaultDeathGameloops > 0
            && defaultDeathGameloops < remainingGameloops)
        {
            builder.Append(deathLinePoint, lifetimeGameloops - defaultDeathGameloops);
        }

        builder.Append(death, lifetimeGameloops);
        return builder.ToPathKey();
    }

    private static DoublePoint GetDeathLinePoint(DoublePoint spawn, DoublePoint routeTarget, double deathLineSum)
    {
        double startSum = spawn.X + spawn.Y;
        double targetSum = routeTarget.X + routeTarget.Y;
        double delta = targetSum - startSum;
        if (Math.Abs(delta) <= PathEpsilon)
        {
            return spawn;
        }

        double progress = Math.Clamp((deathLineSum - startSum) / delta, 0, 1);
        return new(
            spawn.X + (routeTarget.X - spawn.X) * progress,
            spawn.Y + (routeTarget.Y - spawn.Y) * progress);
    }

    private static DoublePoint MoveTowards(DoublePoint start, DoublePoint target, double distance)
    {
        double totalDistance = GetDistance(start, target);
        if (totalDistance <= PathEpsilon || distance >= totalDistance)
        {
            return target;
        }

        double progress = Math.Max(0, distance) / totalDistance;
        return new(
            start.X + (target.X - start.X) * progress,
            start.Y + (target.Y - start.Y) * progress);
    }

    private static int GetMinimumSpeedGameloops(double distance)
    {
        return distance <= PathEpsilon
            ? 0
            : Math.Max(1, (int)Math.Floor(distance / DefaultSpeedPerGameloop));
    }

    private static double GetDistance(DoublePoint left, DoublePoint right)
    {
        double deltaX = right.X - left.X;
        double deltaY = right.Y - left.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private readonly record struct DoublePoint(double X, double Y);

    private readonly record struct PathPoint(int X, int Y, int GameloopOffset)
    {
        public PathPoint(DoublePoint point, int gameloopOffset)
            : this(ToPathCoordinate(point.X, MapWidth), ToPathCoordinate(point.Y, MapHeight), gameloopOffset)
        {
        }
    }

    private readonly record struct PathKey(
        int PointCount,
        PathPoint Point0,
        PathPoint Point1,
        PathPoint Point2,
        PathPoint Point3);

    private struct PathBuilder
    {
        private PathPoint point0;
        private PathPoint point1;
        private PathPoint point2;
        private PathPoint point3;
        private int count;

        public void Append(DoublePoint point, int gameloopOffset)
        {
            Append(new PathPoint(point, Math.Max(0, gameloopOffset)));
        }

        public void Append(PathPoint point)
        {
            if (count > 0 && point.GameloopOffset <= Last.GameloopOffset)
            {
                ReplaceLast(point with { GameloopOffset = Last.GameloopOffset });
                return;
            }

            switch (count)
            {
                case 0:
                    point0 = point;
                    break;
                case 1:
                    point1 = point;
                    break;
                case 2:
                    point2 = point;
                    break;
                case 3:
                    point3 = point;
                    break;
                default:
                    throw new InvalidOperationException("Spawn playback paths support at most four points.");
            }

            count++;
        }

        public readonly PathKey ToPathKey()
        {
            return new(count, point0, point1, point2, point3);
        }

        private readonly PathPoint Last => count switch
        {
            1 => point0,
            2 => point1,
            3 => point2,
            4 => point3,
            _ => default
        };

        private void ReplaceLast(PathPoint point)
        {
            switch (count)
            {
                case 1:
                    point0 = point;
                    break;
                case 2:
                    point1 = point;
                    break;
                case 3:
                    point2 = point;
                    break;
                case 4:
                    point3 = point;
                    break;
            }
        }
    }

    private static int ToPathCoordinate(double value, int maxValue)
    {
        return (int)Math.Round(Math.Clamp(value, 0, maxValue), MidpointRounding.AwayFromZero);
    }
}

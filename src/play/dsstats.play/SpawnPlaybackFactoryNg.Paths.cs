namespace dsstats.play;

public static partial class SpawnPlaybackFactoryNg
{
    private const double PathEpsilon = 0.000001;
    private const double DeathClusterStopForwardDistance = 2;

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
        int spawnGameloop,
        int? diedX,
        int? diedY,
        int lifetimeGameloops,
        ReadOnlySpan<DeathCluster> deathClusters)
    {
        lifetimeGameloops = Math.Max(1, lifetimeGameloops);
        var spawn = new DoublePoint(spawnX, spawnY);
        var mirroredRouteTarget = new DoublePoint(MapWidth - spawnX, MapHeight - spawnY);
        if (diedX is int deathX && diedY is int deathY)
        {
            var death = new DoublePoint(deathX, deathY);
            if (!IsForwardRouteLine(spawn, mirroredRouteTarget, death.X + death.Y))
            {
                return PathBuilder
                    .Build(spawn, lifetimeGameloops)
                    .WithDefaultTarget(mirroredRouteTarget)
                    .ToPathKey();
            }

            return PathBuilder
                .Build(spawn, lifetimeGameloops)
                .WithDefaultTarget(death)
                .WithClusterHold(spawnGameloop, deathClusters)
                .WithStandOff(DeathClusterStopForwardDistance)
                .ToPathKey();
        }

        return PathBuilder
            .Build(spawn, lifetimeGameloops)
            .WithDefaultTarget(mirroredRouteTarget)
            .ToPathKey();
    }

    private static double? TryGetFirstClusterLineSum(
        DoublePoint spawn,
        DoublePoint routeTarget,
        double deathLineSum,
        int spawnGameloop,
        int lifetimeGameloops,
        ReadOnlySpan<DeathCluster> deathClusters)
    {
        if (deathClusters.Length == 0)
        {
            return null;
        }

        double deathProgress = GetLineProgress(spawn, routeTarget, deathLineSum);
        if (deathProgress <= PathEpsilon)
        {
            return null;
        }

        int expiresGameloop = spawnGameloop + lifetimeGameloops;
        double bestProgress = double.MaxValue;
        double bestLineSum = 0;
        for (int i = 0; i < deathClusters.Length; i++)
        {
            var cluster = deathClusters[i];
            if (cluster.LastGameloop < spawnGameloop || cluster.FirstGameloop > expiresGameloop)
            {
                continue;
            }

            double clusterProgress = GetLineProgress(spawn, routeTarget, cluster.LineSum);
            if (clusterProgress <= PathEpsilon
                || clusterProgress >= deathProgress - PathEpsilon
                || clusterProgress >= bestProgress)
            {
                continue;
            }

            bestProgress = clusterProgress;
            bestLineSum = cluster.LineSum;
        }

        return bestProgress < double.MaxValue ? bestLineSum : null;
    }

    private static double GetLineProgress(DoublePoint spawn, DoublePoint routeTarget, double lineSum)
    {
        double startSum = spawn.X + spawn.Y;
        double targetSum = routeTarget.X + routeTarget.Y;
        double delta = targetSum - startSum;
        if (Math.Abs(delta) <= PathEpsilon)
        {
            return 0;
        }

        return (lineSum - startSum) / delta;
    }

    private static bool IsForwardRouteLine(DoublePoint spawn, DoublePoint routeTarget, double lineSum)
    {
        double progress = GetLineProgress(spawn, routeTarget, lineSum);
        return progress > PathEpsilon;
    }

    private static DoublePoint GetLinePoint(DoublePoint spawn, DoublePoint routeTarget, double lineSum)
    {
        double startSum = spawn.X + spawn.Y;
        double targetSum = routeTarget.X + routeTarget.Y;
        double delta = targetSum - startSum;
        if (Math.Abs(delta) <= PathEpsilon)
        {
            return spawn;
        }

        double progress = Math.Clamp((lineSum - startSum) / delta, 0, 1);
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
        PathPoint Point3,
        PathPoint Point4,
        PathPoint Point5);

    private ref struct PathBuilder
    {
        private readonly DoublePoint spawn;
        private readonly int lifetimeGameloops;
        private DoublePoint defaultTarget;
        private ReadOnlySpan<DeathCluster> deathClusters;
        private int spawnGameloop;
        private double standOffDistance;
        private bool hasDefaultTarget;
        private bool useClusterHold;

        private PathBuilder(DoublePoint spawn, int lifetimeGameloops)
        {
            this.spawn = spawn;
            this.lifetimeGameloops = Math.Max(1, lifetimeGameloops);
        }

        public static PathBuilder Build(DoublePoint spawn, int lifetimeGameloops)
        {
            return new(spawn, lifetimeGameloops);
        }

        public PathBuilder WithDefaultTarget(DoublePoint target)
        {
            defaultTarget = target;
            hasDefaultTarget = true;
            return this;
        }

        public PathBuilder WithClusterHold(int spawnGameloop, ReadOnlySpan<DeathCluster> deathClusters)
        {
            this.spawnGameloop = spawnGameloop;
            this.deathClusters = deathClusters;
            useClusterHold = deathClusters.Length > 0;
            return this;
        }

        public PathBuilder WithStandOff(double distance)
        {
            standOffDistance = Math.Max(0, distance);
            return this;
        }

        public PathKey ToPathKey()
        {
            if (!hasDefaultTarget)
            {
                throw new InvalidOperationException("A default path target is required.");
            }

            PathPointBuilder points = new();
            points.Append(spawn, 0);

            if (useClusterHold)
            {
                AddClusterHold(ref points);
            }

            AddTarget(ref points);
            return points.ToPathKey();
        }

        private readonly void AddClusterHold(ref PathPointBuilder points)
        {
            double targetLineSum = defaultTarget.X + defaultTarget.Y;
            double? clusterLineSum = TryGetFirstClusterLineSum(
                spawn,
                defaultTarget,
                targetLineSum,
                spawnGameloop,
                lifetimeGameloops,
                deathClusters);
            if (clusterLineSum is not double clusterSum)
            {
                return;
            }

            var clusterLinePoint = MoveTowards(
                GetLinePoint(spawn, defaultTarget, clusterSum),
                defaultTarget,
                standOffDistance);
            int clusterLineGameloops = GetMinimumSpeedGameloops(GetDistance(spawn, clusterLinePoint));
            if (clusterLineGameloops <= 0 || clusterLineGameloops >= lifetimeGameloops)
            {
                return;
            }

            points.Append(clusterLinePoint, clusterLineGameloops);

            int targetGameloops = GetMinimumSpeedGameloops(GetDistance(clusterLinePoint, defaultTarget));
            int remainingAfterClusterGameloops = lifetimeGameloops - clusterLineGameloops;
            if (targetGameloops > 0 && targetGameloops < remainingAfterClusterGameloops)
            {
                points.Append(clusterLinePoint, lifetimeGameloops - targetGameloops);
            }
        }

        private readonly void AddTarget(ref PathPointBuilder points)
        {
            double targetDistance = GetDistance(points.LastPoint, defaultTarget);
            int targetGameloops = GetMinimumSpeedGameloops(targetDistance);
            int currentGameloopOffset = points.LastGameloopOffset;
            int remainingGameloops = lifetimeGameloops - currentGameloopOffset;
            if (targetGameloops >= remainingGameloops)
            {
                points.Append(
                    MoveTowards(points.LastPoint, defaultTarget, DefaultSpeedPerGameloop * remainingGameloops),
                    lifetimeGameloops);
                return;
            }

            currentGameloopOffset += targetGameloops;
            points.Append(defaultTarget, currentGameloopOffset);
            points.Append(defaultTarget, lifetimeGameloops);
        }
    }

    private struct PathPointBuilder
    {
        private PathPoint point0;
        private PathPoint point1;
        private PathPoint point2;
        private PathPoint point3;
        private PathPoint point4;
        private PathPoint point5;
        private int count;

        public readonly DoublePoint LastPoint => new(Last.X, Last.Y);

        public readonly int LastGameloopOffset => Last.GameloopOffset;

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
                case 4:
                    point4 = point;
                    break;
                case 5:
                    point5 = point;
                    break;
                default:
                    throw new InvalidOperationException("Spawn playback paths support at most six points.");
            }

            count++;
        }

        public readonly PathKey ToPathKey()
        {
            return new(count, point0, point1, point2, point3, point4, point5);
        }

        private readonly PathPoint Last => count switch
        {
            1 => point0,
            2 => point1,
            3 => point2,
            4 => point3,
            5 => point4,
            6 => point5,
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
                case 5:
                    point4 = point;
                    break;
                case 6:
                    point5 = point;
                    break;
            }
        }
    }

    private static int ToPathCoordinate(double value, int maxValue)
    {
        return (int)Math.Round(Math.Clamp(value, 0, maxValue), MidpointRounding.AwayFromZero);
    }
}

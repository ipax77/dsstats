namespace dsstats.parser;

internal class MapLayout(Dictionary<int, DsPlayer> players)
{
    public Pos Nexus { get; set; } = Pos.Zero;
    public Pos Planetary { get; set; } = Pos.Zero;
    public Pos Cannon { get; set; } = Pos.Zero;
    public Pos Bunker { get; set; } = Pos.Zero;
    public Dictionary<int, DsPlayer> Players { get; set; } = players;

    private readonly Polygon team1SpawnArea = new(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
    private readonly Polygon team2SpawnArea = new(new(84, 93), new(101, 76), new(90, 65), new(73, 82));
    private bool readyCheck;

    public bool IsSpawnUnit(Pos pos, int teamId)
    {
        return teamId == 1 ? team1SpawnArea.Contains(pos)
            : team2SpawnArea.Contains(pos);
    }

    public bool IsReady()
    {
        if (readyCheck) return readyCheck;
        bool ready = Nexus != Pos.Zero && Planetary != Pos.Zero && Cannon != Pos.Zero && Bunker != Pos.Zero
            && Players.Count > 0 && Players.Values.All(a => a.Layout.IsReady());
        readyCheck = true;
        if (!ready)
        {
            throw new InvalidOperationException("Map layout not set.");
        }
        SetGamePos();
        return ready;
    }

    public void SetGamePos()
    {
        var playersTeam1 = Players.Values.Where(x => x.TeamId == 1).ToList();
        var playersTeam2 = Players.Values.Where(x => x.TeamId == 2).ToList();

        SetPos(playersTeam1);
        SetPos(playersTeam2);

        foreach (var pl in playersTeam2)
        {
            pl.GamePos += 3;
        }
    }

    private void SetPos(List<DsPlayer> teamPlayers)
    {
        if (teamPlayers.Count == 1)
        {
            teamPlayers.First().GamePos = 1;
        }
        else if (teamPlayers.Count == 2)
        {
            var player1 = teamPlayers.First();
            var player2 = teamPlayers.Last();
            // Updated line
            var d1 = Distance(Planetary, player1.Layout.South);
            // Updated line
            var d2 = Distance(Planetary, player2.Layout.South);

            if (d1 > d2)
            {
                player1.GamePos = 1;
                player2.GamePos = 2;
            }
            else if (d2 > d1)
            {
                player1.GamePos = 2;
                player2.GamePos = 1;
            }
        }
        else if (teamPlayers.Count == 3)
        {
            var player1 = teamPlayers.First();
            var player2 = teamPlayers.Skip(1).First();
            var player3 = teamPlayers.Last();

            // Updated lines
            var d1 = Distance(Planetary, player1.Layout.South);
            var d2 = Distance(Planetary, player2.Layout.South);
            var d3 = Distance(Planetary, player3.Layout.South);

            DsPlayer middlePlayer;
            if (d1 < d2 && d1 < d3)
            {
                middlePlayer = player1;
                Set3ManPos(middlePlayer, player2, player3);
            }
            else if (d2 < d1 && d2 < d3)
            {
                middlePlayer = player2;
                Set3ManPos(middlePlayer, player1, player3);
            }
            else if (d3 < d1 && d3 < d2)
            {
                middlePlayer = player3;
                Set3ManPos(middlePlayer, player1, player2);
            }
        }
    }

    private static void Set3ManPos(DsPlayer middlePlayer, DsPlayer player1, DsPlayer player2)
    {
        // Updated lines
        var dm1 = Distance(middlePlayer.Layout.West, player1.Layout.South);
        var dm2 = Distance(middlePlayer.Layout.West, player2.Layout.South);

        if (dm1 < dm2)
        {
            middlePlayer.GamePos = 2;
            player1.GamePos = 1;
            player2.GamePos = 3;
        }
        else if (dm2 < dm1)
        {
            middlePlayer.GamePos = 2;
            player1.GamePos = 3;
            player2.GamePos = 1;
        }
    }

    private static double Distance(Pos p1, Pos p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }
}

internal class Polygon
{
    private readonly Pos[] _vertices;
    private readonly HashSet<Pos> _allPoints;

    public Polygon(params Pos[] points)
    {
        _vertices = points;
        _allPoints = GetAllPointsInsideOrOnEdge().ToHashSet();
    }

    public List<Pos> GetAllPointsInsideOrOnEdge()
    {
        var points = new List<Pos>();
        var minX = _vertices.Min(v => v.X);
        var maxX = _vertices.Max(v => v.X);
        var minY = _vertices.Min(v => v.Y);
        var maxY = _vertices.Max(v => v.Y);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                var p = new Pos(x, y);
                bool isInside = true;
                for (int i = 0; i < _vertices.Length; i++)
                {
                    var p1 = _vertices[i];
                    var p2 = _vertices[(i + 1) % _vertices.Length];
                    var crossProduct = (p.X - p1.X) * (p2.Y - p1.Y) - (p.Y - p1.Y) * (p2.X - p1.X);
                    if (crossProduct < 0)
                    {
                        isInside = false;
                        break;
                    }
                }

                if (isInside)
                {
                    points.Add(p);
                }
            }
        }

        return points;
    }

    public bool Contains(Pos testPoint)
    {
        return _allPoints.Contains(testPoint);
    }
}
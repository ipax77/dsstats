namespace dsstats.builder;

/// <summary>
/// represents the in game buildArea with an isometric distortion
/// </summary>
public class ScreenArea
{
    public readonly int screenWidth = 2560;
    public readonly int screenHeight = 1440;
    private readonly float _scaleX;
    public readonly float _scaleY;
    private readonly Homography homography;

    private List<RlPoint> polygon = [];
    private List<RlPoint> polygon1 =
    [
        new RlPoint(485, 437),   // Left
        new RlPoint(1124, -110),   // Top
        new RlPoint(2100, 765),  // Right
        new RlPoint(1468, 1423),    // Bottom
    ];

    private List<RlPoint> polygon2 =
    [
        new RlPoint(482, 498),   // Left
        new RlPoint(1128, -50),   // Top
        new RlPoint(2114, 828),  // Right
        new RlPoint(1469, 1503),    // Bottom
    ];

    private RlPoint center = RlPoint.Zero;
    private RlPoint center2 = new(1278, 581);
    private RlPoint center1 = new(1410, 470);

    public ScreenArea(int team, int screenWidth, int screenHeight)
    {
        polygon = team == 1 ? polygon1 : polygon2;
        center = team == 1 ? center1 : center2;
        _scaleX = screenWidth / 2560f;
        _scaleY = screenHeight / 1440f;
        this.screenWidth = screenWidth;
        this.screenHeight = screenHeight;

        var buildPoints = new[]
        {
            (new RlPoint(0, 0), polygon[1]),         // Top
            (new RlPoint(17, -17), polygon[2]),      // Right
            (new RlPoint(6, -28), polygon[3]),       // Bottom
            (new RlPoint(-11, -11), polygon[0]),    // Left
        };
        homography = new Homography(buildPoints);
    }

    public RlPoint ApplyTransforms(RlPoint point)
    {
        return new RlPoint((int)(point.X * _scaleX), (int)(point.Y * _scaleY));
    }

    public RlPoint GetCenter()
    {
        return ApplyTransforms(center);
    }

    /// <summary>
    /// maps the normalized replay unit position to the screen position
    /// </summary>
    /// <param name="buildPoint"></param>
    /// <returns></returns>

    public RlPoint GetScreenPosition(RlPoint normalizedBuildPoint)
    {
        return ApplyTransforms(homography.Transform(normalizedBuildPoint));
    }
}
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

    private List<RlPoint> polygon =
    [
        new RlPoint(485, 437),   // Left
        new RlPoint(1124, -110),   // Top
        new RlPoint(2100, 765),  // Right
        new RlPoint(1468, 1423),    // Bottom
    ];


    public ScreenArea(int screenWidth, int screenHeight)
    {
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
        var transformedPolygon = polygon
            .Select(s => ApplyTransforms(s))
            .ToList();

        int sumX = transformedPolygon.Sum(p => p.X);
        int sumY = transformedPolygon.Sum(p => p.Y);
        int count = transformedPolygon.Count;

        return new(sumX / count, sumY / count);
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
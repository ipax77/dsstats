namespace dsstats.builder;

/// <summary>
/// represents the in game buildArea with an isometric distortion
/// </summary>
public class ScreenArea
{
    private readonly int screenWidth = 2560;
    private readonly int screenHeight = 1440;
    private readonly float _scaleX;
    private readonly float _scaleY;
    private int _cameraYOffset = 0;

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
    }

    private RlPoint ApplyTransforms(RlPoint point)
    {
        return new RlPoint((int)(point.X * _scaleX), (int)(point.Y * _scaleY) + _cameraYOffset);
    }

    public void SetCameraYOffset(int offsetY)
    {
        _cameraYOffset = offsetY;
    }

    public RlPoint GetCenter()
    {
        var transformedPolygon = polygon
            .Select(s => ApplyTransforms(s))
            .ToList();

        int x1 = transformedPolygon.Min(m => m.X);
        int y1 = transformedPolygon.Min(m => m.Y);
        int x2 = transformedPolygon.Max(m => m.X);
        int y2 = transformedPolygon.Max(m => m.Y);

        return new(x1 + ((x2 - x1) / 2), y1 + ((y2 - y1) / 2));
    }

    /// <summary>
    /// maps the normalized replay unit position to the screen position
    /// </summary>
    /// <param name="buildPoint"></param>
    /// <returns></returns>
    public RlPoint GetScreenPosition(RlPoint normalizedBuildPoint)
    {
        var screenTop = polygon[1];    // Normalized origin
        var screenRight = polygon[2];  // (17, -17)
        var screenLeft = polygon[0];   // (-11, -11)

        // Basis vectors (from Top)
        var xBasis = screenRight - screenTop; // ∆x over 17
        var yBasis = screenLeft - screenTop;  // ∆y over -11

        // Scale x/y contributions
        var xComponent = xBasis * (normalizedBuildPoint.X / 17.0);
        var yComponent = yBasis * (normalizedBuildPoint.Y / -11.0);

        var screenPos = screenTop + xComponent + yComponent;

        return ApplyTransforms(screenPos);
    }
}
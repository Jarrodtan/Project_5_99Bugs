using UnityEngine;

/// <summary>
/// Pure placement for the overlay tooltip. Prefers above the target, flips below when needed, then clamps.
/// </summary>
public static class TooltipPlacement
{
    /// <summary>
    /// Computes the screen-space center for a tooltip of <paramref name="tooltipSize"/> near <paramref name="targetScreenRect"/>.
    /// </summary>
    /// <param name="targetScreenRect">Target bounds in screen pixels (origin bottom-left).</param>
    /// <param name="tooltipSize">Tooltip frame size in pixels.</param>
    /// <param name="gap">Pixels between the target edge and the tooltip edge.</param>
    /// <param name="screenSize">Screen width and height in pixels.</param>
    /// <param name="screenPadding">Inset from each screen edge.</param>
    /// <param name="placedAbove">True when the tooltip sits above the target.</param>
    /// <returns>Screen-space center for a center-pivoted overlay rect.</returns>
    public static Vector2 ComputeScreenCenter(
        Rect targetScreenRect,
        Vector2 tooltipSize,
        float gap,
        Vector2 screenSize,
        Vector2 screenPadding,
        out bool placedAbove)
    {
        var half = tooltipSize * 0.5f;
        var x = targetScreenRect.center.x;
        var aboveY = targetScreenRect.yMax + gap + half.y;
        var belowY = targetScreenRect.yMin - gap - half.y;

        placedAbove = aboveY + half.y <= screenSize.y - screenPadding.y;
        if (!placedAbove && belowY - half.y < screenPadding.y)
            placedAbove = true;

        var y = placedAbove ? aboveY : belowY;
        var minX = screenPadding.x + half.x;
        var maxX = screenSize.x - screenPadding.x - half.x;
        var minY = screenPadding.y + half.y;
        var maxY = screenSize.y - screenPadding.y - half.y;

        if (maxX < minX)
            x = screenSize.x * 0.5f;
        else
            x = Mathf.Clamp(x, minX, maxX);

        if (maxY < minY)
            y = screenSize.y * 0.5f;
        else
            y = Mathf.Clamp(y, minY, maxY);

        return new Vector2(x, y);
    }
}

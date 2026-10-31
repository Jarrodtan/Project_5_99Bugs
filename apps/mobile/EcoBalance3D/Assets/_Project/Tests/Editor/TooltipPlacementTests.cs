using NUnit.Framework;
using UnityEngine;

// EditMode tests for overlay tooltip placement: above when room, flip below, clamp sideways.
public class TooltipPlacementTests
{
    static readonly Vector2 Screen = new Vector2(800f, 600f);
    static readonly Vector2 Padding = new Vector2(8f, 8f);
    static readonly Vector2 TooltipSize = new Vector2(120f, 40f);
    const float Gap = 8f;

    [Test]
    public void PrefersAbove_WhenThereIsRoom()
    {
        var target = new Rect(340f, 200f, 80f, 80f);

        var center = TooltipPlacement.ComputeScreenCenter(
            target, TooltipSize, Gap, Screen, Padding, out var placedAbove);

        Assert.IsTrue(placedAbove);
        Assert.AreEqual(target.center.x, center.x, 0.001f);
        Assert.AreEqual(target.yMax + Gap + TooltipSize.y * 0.5f, center.y, 0.001f);
    }

    [Test]
    public void FlipsBelow_WhenTargetIsNearTop()
    {
        var target = new Rect(340f, 560f, 80f, 36f);

        var center = TooltipPlacement.ComputeScreenCenter(
            target, TooltipSize, Gap, Screen, Padding, out var placedAbove);

        Assert.IsFalse(placedAbove);
        Assert.AreEqual(target.center.x, center.x, 0.001f);
        Assert.AreEqual(target.yMin - Gap - TooltipSize.y * 0.5f, center.y, 0.001f);
    }

    [Test]
    public void ClampsHorizontally_WhenTargetIsNearSideEdge()
    {
        var target = new Rect(0f, 200f, 40f, 80f);
        var halfWidth = TooltipSize.x * 0.5f;
        var minX = Padding.x + halfWidth;

        var center = TooltipPlacement.ComputeScreenCenter(
            target, TooltipSize, Gap, Screen, Padding, out _);

        Assert.AreEqual(minX, center.x, 0.001f);
        Assert.GreaterOrEqual(center.x - halfWidth, Padding.x - 0.001f);
    }
}

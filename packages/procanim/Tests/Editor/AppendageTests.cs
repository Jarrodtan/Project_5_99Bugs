// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AppendageTests
{
    // Four-legged "table" animal facing +Z (as in LegLayoutTests) plus a head sticking out at the
    // front (z 1 to 1.4, raised) and a thin tail at the back (z -1.3 to -1).
    static List<Vector3> Animal()
    {
        var points = new List<Vector3>();
        void Box(Vector3 min, Vector3 max)
        {
            for (int i = 0; i <= 4; i++)
            for (int j = 0; j <= 4; j++)
            for (int k = 0; k <= 4; k++)
                points.Add(new Vector3(Mathf.Lerp(min.x, max.x, i / 4f), Mathf.Lerp(min.y, max.y, j / 4f), Mathf.Lerp(min.z, max.z, k / 4f)));
        }
        Box(new Vector3(-0.5f, 0.6f, -1f), new Vector3(0.5f, 1f, 1f));      // body
        foreach (float x in new[] { -0.45f, 0.45f })
        foreach (float z in new[] { -0.95f, 0.95f })
            Box(new Vector3(x - 0.05f, 0f, z - 0.05f), new Vector3(x + 0.05f, 0.6f, z + 0.05f)); // legs
        Box(new Vector3(-0.2f, 0.8f, 1f), new Vector3(0.2f, 1.2f, 1.4f));   // head
        Box(new Vector3(-0.05f, 0.85f, -1.3f), new Vector3(0.05f, 0.95f, -1f)); // tail
        return points;
    }

    static readonly Vector3 HeadTip = new Vector3(0f, 1f, 1.4f);
    static readonly Vector3 TailTip = new Vector3(0f, 0.9f, -1.3f);
    static readonly Vector3 BodyMiddle = new Vector3(0f, 0.8f, 0f);
    static readonly Vector3 Foot = new Vector3(-0.45f, 0f, 0.95f);

    LegLayout body;
    Appendage head, tail;

    [SetUp]
    public void SetUp()
    {
        var points = Animal();
        body = LegLayout.Analyze(points, 0.5f, 0.1f);
        head = Appendage.Analyze(points, body, true, 0.25f, 0.08f);
        tail = Appendage.Analyze(points, body, false, 0.1f, 0.08f);
    }

    [Test]
    public void FindsHeadAtTheFront_AndTailAtTheBack()
    {
        Assert.IsTrue(body.Valid);
        Assert.IsTrue(head.Valid);
        Assert.IsTrue(tail.Valid);
        Assert.Greater(head.Joint.z, 0.5f, "neck is towards the front");
        Assert.Less(tail.Joint.z, -0.9f, "tail root is at the back");
    }

    [Test]
    public void OnlyTheEndsMove_BodyAndLegsStayPut()
    {
        Assert.AreEqual(1f, head.Weight(HeadTip, body.Center), 1e-5f);
        Assert.AreEqual(1f, tail.Weight(TailTip, body.Center), 1e-5f);
        Assert.AreEqual(0f, head.Weight(BodyMiddle, body.Center));
        Assert.AreEqual(0f, tail.Weight(BodyMiddle, body.Center));
        Assert.AreEqual(0f, head.Weight(Foot, body.Center), "legs belong to the legs, not the head");
    }

    [Test]
    public void HeadBendsDown_ForGrazing()
    {
        var bent = head.Rotate(HeadTip, 30f, 0f, 1f);
        Assert.Less(bent.y, HeadTip.y, "+ pitch lowers the head");
        Assert.AreEqual((HeadTip - head.Joint).magnitude, (bent - head.Joint).magnitude, 1e-4f, "neck doesn't stretch");
    }

    [Test]
    public void HeadTurnsSideways_ToLook()
    {
        var turned = head.Rotate(HeadTip, 0f, 30f, 1f);
        Assert.Greater(turned.x, HeadTip.x, "+ yaw turns clockwise seen from above (to the right)");
        Assert.AreEqual(HeadTip.y, turned.y, 1e-4f, "turning keeps the head's height");
    }

    [Test]
    public void TailWags_Sideways()
    {
        var left = tail.Rotate(TailTip, 0f, 20f, 1f);
        var right = tail.Rotate(TailTip, 0f, -20f, 1f);
        Assert.Greater(Mathf.Abs(left.x - right.x), 0.05f);
        Assert.AreEqual(TailTip.y, left.y, 1e-4f);
    }

    [Test]
    public void ModelFacingBackward_SwapsHeadAndTailEnds()
    {
        var points = Animal();
        var flipped = LegLayout.Analyze(points, 0.5f, 0.1f, facesBackward: true);
        var flippedHead = Appendage.Analyze(points, flipped, true, 0.1f, 0.08f);
        Assert.Less(flippedHead.Joint.z, 0f, "head now at the -Z end");
    }

    [Test]
    public void NoBody_NoAppendage()
    {
        Assert.IsFalse(Appendage.Analyze(Animal(), new LegLayout(), true, 0.25f, 0.08f).Valid);
    }
}
// [AI-GENERATED END]

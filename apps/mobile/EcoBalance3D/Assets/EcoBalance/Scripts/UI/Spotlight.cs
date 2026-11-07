// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dims everything except one panel, so the player looks at it (the mascot's introduction in Guided and Free).
/// The panel is lifted onto its own sorting layer above a dark full-screen backdrop that also blocks taps
/// elsewhere; Hide puts both back. Blur would cost too much on a tablet, so it is a dark tint.
/// </summary>
public static class Spotlight
{
    const int TargetOrder = 90;   // above the Guided cards (80) and the HUD
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);

    class Lifted { public Canvas canvas; public bool added; public bool overrode; public int order; public GameObject dim; public Lifted above; }
    static readonly Dictionary<Transform, Lifted> lifted = new Dictionary<Transform, Lifted>();

    public static bool IsOn(Transform target) => target != null && lifted.ContainsKey(target);

    /// <param name="keepAbove">Optional panel drawn one layer above the target (the Play bar stays in front of the mascot).</param>
    public static void Show(Transform target, Transform keepAbove = null)
    {
        if (target == null || lifted.ContainsKey(target)) return;
        var l = Lift(target, TargetOrder);
        if (keepAbove != null) l.above = Lift(keepAbove, TargetOrder + 1);

        // Backdrop: a sibling just before the panel, stretched far past the screen so any parent size is covered.
        var dim = new GameObject("Spotlight Dim", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
        dim.transform.SetParent(target.parent, false);
        dim.transform.SetSiblingIndex(target.GetSiblingIndex());
        var rt = (RectTransform)dim.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = new Vector2(8000f, 8000f);
        rt.anchoredPosition = Vector2.zero;
        var c = dim.GetComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = TargetOrder - 1;
        var img = dim.GetComponent<Image>();
        img.color = Dim;
        img.raycastTarget = true;   // taps outside the panel do nothing while it speaks
        l.dim = dim;
        lifted[target] = l;
    }

    static Lifted Lift(Transform target, int order)
    {
        var l = new Lifted();
        l.canvas = target.GetComponent<Canvas>();
        if (l.canvas == null)
        {
            l.canvas = target.gameObject.AddComponent<Canvas>();
            l.added = true;
            if (target.GetComponent<GraphicRaycaster>() == null) target.gameObject.AddComponent<GraphicRaycaster>();
        }
        l.overrode = l.canvas.overrideSorting;
        l.order = l.canvas.sortingOrder;
        l.canvas.overrideSorting = true;
        l.canvas.sortingOrder = order;
        return l;
    }

    public static void Hide(Transform target)
    {
        if (target == null || !lifted.TryGetValue(target, out var l)) return;
        lifted.Remove(target);
        if (l.dim != null) Object.Destroy(l.dim);
        Restore(l.above);
        Restore(l);
    }

    static void Restore(Lifted l)
    {
        if (l == null || l.canvas == null) return;
        if (l.added)
        {
            // Can't remove a Canvas while a GraphicRaycaster needs it in the same frame: just stop overriding.
            l.canvas.overrideSorting = false;
        }
        else
        {
            l.canvas.overrideSorting = l.overrode;
            l.canvas.sortingOrder = l.order;
        }
    }
}
// [AI-GENERATED END]

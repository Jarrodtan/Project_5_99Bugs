using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared embedding and visibility logic for every <see cref="ITabbedPanelContent"/> panel.
/// Owns the panel's standalone canvas (when not hosted), moves the panel root into a tab host,
/// places it, and hides it with a <see cref="CanvasGroup"/> so the panel's scripts keep running.
/// </summary>
public sealed class TabContentFrame
{
    RectTransform embedHost;
    Canvas standaloneCanvas;
    RectTransform root;
    CanvasGroup rootGroup;
    bool shown = true;

    /// <summary>True once <see cref="EmbedIn"/> has been called.</summary>
    public bool IsEmbedded => embedHost != null;

    /// <summary>True unless the tab host has hidden this panel.</summary>
    public bool IsShown => shown;

    /// <summary>
    /// Parent for the panel root: the tab host when embedded, otherwise a new Screen Space Overlay
    /// canvas under <paramref name="owner"/>.
    /// </summary>
    /// <param name="owner">Panel component's transform.</param>
    /// <param name="canvasName">Name for the standalone canvas GameObject.</param>
    /// <param name="withRaycaster">True when the panel has clickable or click-blocking graphics.</param>
    /// <returns>Transform to parent the panel root to.</returns>
    public Transform ResolveParent(Transform owner, string canvasName, bool withRaycaster = false)
    {
        if (embedHost != null)
            return embedHost;

        var canvasGo = new GameObject(canvasName);
        canvasGo.transform.SetParent(owner, false);
        standaloneCanvas = canvasGo.AddComponent<Canvas>();
        standaloneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        if (withRaycaster)
            canvasGo.AddComponent<GraphicRaycaster>();
        return canvasGo.transform;
    }

    /// <summary>
    /// Registers the panel's root so it can be moved into a host and shown/hidden.
    /// Call once after creating the root.
    /// </summary>
    /// <param name="panelRoot">Top-level RectTransform of the panel content.</param>
    public void AttachRoot(RectTransform panelRoot)
    {
        root = panelRoot;
        rootGroup = root.GetComponent<CanvasGroup>();
        if (rootGroup == null)
            rootGroup = root.gameObject.AddComponent<CanvasGroup>();
        ApplyShown();
    }

    /// <summary>
    /// Moves the panel into <paramref name="host"/>. Before the root exists this only records the host,
    /// so <see cref="ResolveParent"/> builds straight into it.
    /// </summary>
    /// <param name="host">Tab host content area.</param>
    /// <returns>True when an existing root was moved (caller should re-apply layout).</returns>
    public bool EmbedIn(RectTransform host)
    {
        embedHost = host;
        if (root == null)
            return false;

        root.SetParent(host, false);
        if (standaloneCanvas != null)
            Object.Destroy(standaloneCanvas.gameObject);
        standaloneCanvas = null;
        return true;
    }

    /// <summary>
    /// Shows or hides the panel visually. The GameObjects stay active, so polling and animations continue.
    /// </summary>
    /// <param name="isShown">True to show.</param>
    public void SetShown(bool isShown)
    {
        shown = isShown;
        ApplyShown();
    }

    /// <summary>
    /// Sets the standalone canvas sort order. No-op when embedded.
    /// </summary>
    /// <param name="sortOrder">Canvas sorting order.</param>
    public void SetSortOrder(int sortOrder)
    {
        if (standaloneCanvas != null)
            standaloneCanvas.sortingOrder = sortOrder;
    }

    /// <summary>
    /// Embedded: stretches the root to the host's width, flush under the tab bar.
    /// Standalone: pins the root to <paramref name="anchor"/> at <paramref name="anchoredPosition"/>.
    /// Height is left to the panel (content size fitter or explicit).
    /// </summary>
    /// <param name="anchor">Standalone screen anchor/pivot (0–1).</param>
    /// <param name="anchoredPosition">Standalone pixel offset from the anchor.</param>
    /// <param name="width">Standalone width (pixels).</param>
    public void ApplyPlacement(Vector2 anchor, Vector2 anchoredPosition, float width)
    {
        if (root == null)
            return;

        if (embedHost != null)
        {
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(0f, root.sizeDelta.y);
            return;
        }

        root.anchorMin = anchor;
        root.anchorMax = anchor;
        root.pivot = anchor;
        root.anchoredPosition = anchoredPosition;
        root.sizeDelta = new Vector2(width, root.sizeDelta.y);
    }

    void ApplyShown()
    {
        if (rootGroup == null)
            return;

        rootGroup.alpha = shown ? 1f : 0f;
        rootGroup.interactable = shown;
        rootGroup.blocksRaycasts = shown;
    }
}

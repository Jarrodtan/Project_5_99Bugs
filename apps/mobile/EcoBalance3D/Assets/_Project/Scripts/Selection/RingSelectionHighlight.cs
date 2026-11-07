using UnityEngine;

/// <summary>
/// Ground ring/decal highlight. Parents one instance under the target and styles it only from
/// <see cref="SelectionHighlightConfig"/>. Does not tint the selectable's materials.
/// </summary>
public sealed class RingSelectionHighlight : ISelectionHighlightVisual
{
    GameObject instance;
    Material runtimeMaterial;

    /// <inheritdoc />
    public void Apply(Transform target, SelectionHighlightConfig config)
    {
        Clear();

        if (target == null || config == null)
            return;

        instance = config.ringPrefab != null
            ? Object.Instantiate(config.ringPrefab, target)
            : CreateProceduralRing(config);

        if (instance == null)
            return;

        instance.name = "SelectionHighlightRing";
        instance.transform.SetParent(target, false);
        instance.transform.localPosition = config.localOffset;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = config.localScale;

        ApplyColorAndSorting(instance, config);
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (instance != null)
        {
            if (instance)
                Object.Destroy(instance);
            instance = null;
        }

        if (runtimeMaterial != null)
        {
            Object.Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    GameObject CreateProceduralRing(SelectionHighlightConfig config)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        return go;
    }

    void ApplyColorAndSorting(GameObject ring, SelectionHighlightConfig config)
    {
        var renderer = ring.GetComponent<Renderer>();
        if (renderer == null)
            renderer = ring.GetComponentInChildren<Renderer>();
        if (renderer == null)
            return;

        if (config.ringMaterial != null)
        {
            renderer.sharedMaterial = config.ringMaterial;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", config.color);
            block.SetColor("_Color", config.color);
            renderer.SetPropertyBlock(block);
        }
        else
        {
            if (runtimeMaterial != null)
                Object.Destroy(runtimeMaterial);

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            runtimeMaterial = shader != null
                ? new Material(shader)
                : new Material(renderer.sharedMaterial);

            if (runtimeMaterial.HasProperty("_BaseColor"))
                runtimeMaterial.SetColor("_BaseColor", config.color);
            if (runtimeMaterial.HasProperty("_Color"))
                runtimeMaterial.SetColor("_Color", config.color);
            renderer.material = runtimeMaterial;
        }

        if (!string.IsNullOrEmpty(config.sortingLayerName))
            renderer.sortingLayerName = config.sortingLayerName;
        renderer.sortingOrder = config.sortingOrder;
    }
}

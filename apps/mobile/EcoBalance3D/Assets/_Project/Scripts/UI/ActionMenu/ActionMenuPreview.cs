using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Photographs an option prefab into a sprite for the action-menu bubble.
/// Island prefabs have no mesh until the world builds, so those get a small stand-in
/// made from the same grass, rock, and snow layers.
/// Prototype UI — not approved Feature Plan work.
/// </summary>
public static class ActionMenuPreview
{
    const int Size = 128;
    const int PreviewLayer = 31;

    static readonly Dictionary<EntityId, Sprite> Cache = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearCache()
    {
        Cache.Clear();
    }

    /// <summary>Returns a cached photo of <paramref name="prefab"/>, or null when there is nothing to show.</summary>
    public static Sprite Capture(GameObject prefab)
    {
        if (prefab == null)
            return null;

        var key = prefab.GetEntityId();
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        var subject = prefab.GetComponent<HimalayaTerrainBuilder>() != null
            ? BuildIslandStandIn(prefab)
            : SpawnQuiet(prefab);
        if (subject == null)
            return null;

        var sprite = Shoot(subject);
        Object.Destroy(subject);
        if (sprite != null)
            Cache[key] = sprite;

        return sprite;
    }

    static GameObject SpawnQuiet(GameObject prefab)
    {
        var subject = Object.Instantiate(prefab);
        subject.SetActive(false);
        subject.name = prefab.name;

        var behaviours = subject.GetComponentsInChildren<Behaviour>(true);
        for (var i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                Object.DestroyImmediate(behaviours[i]);
        }

        var particles = subject.GetComponentsInChildren<ParticleSystem>(true);
        for (var i = 0; i < particles.Length; i++)
        {
            if (particles[i] != null)
                Object.DestroyImmediate(particles[i]);
        }

        subject.SetActive(true);
        if (subject.GetComponentsInChildren<MeshRenderer>(true).Length == 0
            && subject.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0)
        {
            Object.Destroy(subject);
            return null;
        }

        return subject;
    }

    static GameObject BuildIslandStandIn(GameObject prefab)
    {
        var builder = prefab.GetComponent<HimalayaTerrainBuilder>();
        var layers = builder != null ? builder.BootstrapLayers : null;
        var flat = builder != null && builder.BuildProfile != null && builder.BuildProfile.flattenHeights;

        var root = new GameObject(prefab.name + " Preview");
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform, false);
        ground.transform.localScale = new Vector3(2.4f, 0.12f, 2.4f);
        ground.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        Paint(ground, TextureOf(layers, 0), new Color(0.42f, 0.62f, 0.3f));
        Object.DestroyImmediate(ground.GetComponent<Collider>());

        if (!flat)
        {
            var peak = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            peak.name = "Peak";
            peak.transform.SetParent(root.transform, false);
            peak.transform.localScale = new Vector3(1.15f, 0.9f, 1.15f);
            peak.transform.localPosition = new Vector3(0.08f, 0.34f, 0f);
            Paint(peak, TextureOf(layers, Last(layers)), new Color(0.93f, 0.95f, 0.97f));
            Object.DestroyImmediate(peak.GetComponent<Collider>());

            var ridge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ridge.name = "Ridge";
            ridge.transform.SetParent(root.transform, false);
            ridge.transform.localScale = new Vector3(0.72f, 0.42f, 0.72f);
            ridge.transform.localPosition = new Vector3(-0.55f, 0.1f, 0.2f);
            Paint(ridge, TextureOf(layers, layers != null && layers.Length > 1 ? 1 : 0), new Color(0.48f, 0.4f, 0.34f));
            Object.DestroyImmediate(ridge.GetComponent<Collider>());
        }

        return root;
    }

    static int Last(TerrainLayer[] layers)
    {
        return layers != null && layers.Length > 0 ? layers.Length - 1 : 0;
    }

    static Texture TextureOf(TerrainLayer[] layers, int index)
    {
        if (layers == null || index < 0 || index >= layers.Length || layers[index] == null)
            return null;

        return layers[index].diffuseTexture;
    }

    static void Paint(GameObject target, Texture texture, Color tint)
    {
        var renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            return;

        var material = new Material(shader);
        material.name = "ActionMenuPreview";
        material.color = tint;
        material.SetColor("_BaseColor", tint);
        if (texture != null)
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
        }

        renderer.sharedMaterial = material;
    }

    static Sprite Shoot(GameObject subject)
    {
        var stage = new GameObject("ActionMenuPreviewStage");
        stage.transform.position = new Vector3(0f, -8000f, 0f);
        subject.transform.SetParent(stage.transform, false);
        subject.transform.localPosition = Vector3.zero;
        subject.transform.localRotation = Quaternion.identity;
        SetLayer(subject);

        var renderers = new List<Renderer>();
        renderers.AddRange(subject.GetComponentsInChildren<MeshRenderer>(true));
        renderers.AddRange(subject.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        if (renderers.Count == 0)
        {
            subject.transform.SetParent(null, false);
            Object.Destroy(stage);
            return null;
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Count; i++)
            bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.sqrMagnitude < 0.0001f)
        {
            subject.transform.SetParent(null, false);
            Object.Destroy(stage);
            return null;
        }

        var lightObject = new GameObject("PreviewLight");
        lightObject.transform.SetParent(stage.transform, false);
        lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.cullingMask = 1 << PreviewLayer;

        var fit = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z, 0.2f);
        var cameraObject = new GameObject("PreviewCamera");
        cameraObject.transform.SetParent(stage.transform, false);
        var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.cameraType = CameraType.Game;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.93f, 0.95f, 0.9f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = fit * 1.35f;
        camera.cullingMask = 1 << PreviewLayer;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = Mathf.Max(50f, fit * 20f);
        camera.transform.position = bounds.center + new Vector3(fit, fit * 0.85f, -fit * 1.2f);
        camera.transform.LookAt(bounds.center);
        if (cameraObject.GetComponent<UniversalAdditionalCameraData>() == null)
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

        var texture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        texture.Create();
        camera.targetTexture = texture;
        var previous = RenderTexture.active;
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = texture };
        RenderPipeline.SubmitRenderRequest(camera, request);

        var photo = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        RenderTexture.active = texture;
        photo.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
        photo.Apply();
        RenderTexture.active = previous;
        camera.targetTexture = null;
        Object.Destroy(texture);

        ReleasePreviewMaterials(subject);
        subject.transform.SetParent(null, false);
        Object.Destroy(stage);

        if (IsBlank(photo))
        {
            Object.Destroy(photo);
            return null;
        }

        return Sprite.Create(photo, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
    }

    static void ReleasePreviewMaterials(GameObject subject)
    {
        var renderers = subject.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var material = renderers[i].sharedMaterial;
            if (material != null && material.name.StartsWith("ActionMenuPreview"))
                Object.Destroy(material);
        }
    }

    static bool IsBlank(Texture2D photo)
    {
        var pixels = photo.GetPixels32();
        for (var i = 0; i < pixels.Length; i += 17)
        {
            var pixel = pixels[i];
            if (Mathf.Abs(pixel.r - 237) > 12 || Mathf.Abs(pixel.g - 242) > 12 || Mathf.Abs(pixel.b - 230) > 12)
                return false;
        }

        return true;
    }

    static void SetLayer(GameObject root)
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
            transforms[i].gameObject.layer = PreviewLayer;
    }
}

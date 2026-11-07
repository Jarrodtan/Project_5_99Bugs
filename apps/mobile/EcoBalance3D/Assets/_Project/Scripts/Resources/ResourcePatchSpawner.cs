// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;
using UnityEngine.Rendering;

// Scatters ResourcePatch objects (e.g. grass tufts, water puddles) around the island at start.
// Each patch shrinks when animals eat/drink from it and regrows over time.
public class ResourcePatchSpawner : MonoBehaviour
{
    [Tooltip("Resource every spawned patch belongs to (e.g. Grass, Water).")]
    [SerializeField] ResourceDefinition resource;

    [Tooltip("How many patches to scatter.")]
    [Min(0)] [SerializeField] int count = 6;

    [Tooltip("Patches are placed randomly within this distance of the spawner (XZ).")]
    [SerializeField] float radius = 7f;

    [Tooltip("Keeps patches out of the very centre so animals spawn clear of them.")]
    [SerializeField] float minRadius = 1.5f;

    [SerializeField] PrimitiveType shape = PrimitiveType.Cube;
    [SerializeField] Vector3 size = new Vector3(1.2f, 0.25f, 1.2f);
    [SerializeField] Color color = new Color(0.3f, 0.75f, 0.3f, 1f);

    void Start()
    {
        if (resource == null)
        {
            Debug.LogError($"{nameof(ResourcePatchSpawner)} needs a resource.", this);
            return;
        }

        Material material = MakeMaterial();
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(minRadius, radius);
            Vector3 pos = transform.position + new Vector3(dir.x, 0f, dir.y) * dist;
            pos.y = transform.position.y + size.y * 0.5f;

            GameObject go = GameObject.CreatePrimitive(shape);
            go.name = $"{resource.name} Patch {i + 1}";
            go.transform.SetParent(transform, true);
            go.transform.position = pos;
            go.transform.localScale = size;

            // Visual only: no collider, so patches don't block the NavMesh or animals.
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;

            go.AddComponent<ResourcePatch>().Configure(resource);
        }
    }

    Material MakeMaterial()
    {
        // URP default material (Lit); the built-in default shows magenta in Android builds.
        var pipeline = GraphicsSettings.currentRenderPipeline;
        Material material = pipeline != null && pipeline.defaultMaterial != null
            ? new Material(pipeline.defaultMaterial)
            : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        return material;
    }
}
// [AI-GENERATED END]

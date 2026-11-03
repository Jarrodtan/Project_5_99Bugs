// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Edit the Himalaya terrain while the simulation runs. Adds an "Edit Terrain" button (top right);
/// opening it shows brushes and sliders:
///   Raise / Lower    - push hills up or down under the brush
///   Flatten          - level the ground to the height where the stroke started
///   Smooth           - soften bumps and sharp ridges
///   Add / Remove Grass - paint grass patches (food) on or off
///   Hill Height      - scales every hill on the map at once (flatter <-> higher)
/// With a brush selected, left-drag (or one finger) paints; right-drag, pinch, wheel and keys still move the camera.
/// Pick "Look" (or close the panel) to pan with left-drag again.
///
/// After an edit, grass and water spots are moved onto the new ground and the NavMesh is re-baked in
/// the background, so animals walk on the new shape.
/// HimalayaWorldBuilder adds this component when the world is built.
/// </summary>
[DefaultExecutionOrder(1000)]
public class TerrainEditTool : MonoBehaviour
{
    public enum Tool { Look, Raise, Lower, Flatten, Smooth, AddGrass, RemoveGrass }

    [Tooltip("Brush radius range, world units.")]
    public Vector2 brushSizeRange = new Vector2(3f, 60f);
    [Tooltip("Raise/Lower speed at full strength, in fractions of the map's total height per second.")]
    public float sculptSpeed = 0.25f;
    [Tooltip("Hill Height slider range (1 = as built).")]
    public Vector2 hillHeightRange = new Vector2(0.2f, 2.5f);
    [Tooltip("Painted grass patches are kept at least this far apart.")]
    public float grassSpacing = 3.8f;
    [Tooltip("Seconds after the last edit before the NavMesh is re-baked.")]
    public float rebakeDelay = 0.5f;

    HimalayaTerrainBuilder terrainBuilder;
    HimalayaResourcePlacer resourcePlacer;
    HimalayaSplatPainter splatPainter;
    bool repaintWholeMap;   // Hill Height changed slopes everywhere
    NavMeshSurface navSurface;
    Terrain terrain;
    TerrainCollider terrainCollider;

    Tool tool = Tool.Look;
    float brushSize = 12f;
    float strength = 0.5f;
    float hillScale = 1f;
    float baseHeight;          // terrain size.y as built
    float riverBaseScaleY = 1f;
    float riverBaseY;
    Slider hillSlider;

    bool stroking;
    float flattenTarget;       // normalised height captured at the start of a Flatten stroke
    float grassTimer;
    float rebakeAt = -1f;
    bool baking;

    // UI
    RectTransform panel;
    RectTransform toggleRect;
    TMP_Text toggleLabel;
    Image toggleImage;
    Button toggleButton;
    bool hudLookCopied;
    float nextHudLookTry;
    TMP_Text statusText;
    bool wasStroking;
    readonly Dictionary<Tool, Image> toolButtons = new Dictionary<Tool, Image>();
    LineRenderer ring;
    LineRenderer dot;
    float savedPixelError = -1f;
    bool loggedPointer;

    static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.12f, 0.88f);
    static readonly Color ButtonColor = new Color(0.22f, 0.26f, 0.3f, 1f);
    static readonly Color ActiveColor = new Color(0.25f, 0.6f, 0.35f, 1f);

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    // The Edit Terrain button matches Shumin's HUD buttons (Scenes/UI.unity: white rounded, dark grey text, 88 tall)
    // and sits under DramaDirector's season / day / weather / temperature pill (top right, y -170, 84 tall).
    static readonly Color HudTextColor = new Color(0.196f, 0.196f, 0.196f, 1f);
    static readonly Vector2 HudReference = new Vector2(2560f, 1600f);   // same canvas size as the HUD and the pill
    // The panel was laid out for a 1920x1080 canvas; this scale keeps it the same size on screen (match 0.5).
    static readonly float PanelScale = Mathf.Sqrt(2560f * 1600f / (1920f * 1080f));
    // [AI-GENERATED END]

    public void Init(HimalayaTerrainBuilder tb, HimalayaResourcePlacer placer, NavMeshSurface surface, HimalayaSplatPainter painter = null)
    {
        terrainBuilder = tb;
        resourcePlacer = placer;
        navSurface = surface;
        splatPainter = painter;
        terrain = tb.Terrain;
        terrainCollider = terrain.GetComponent<TerrainCollider>();
        baseHeight = terrain.terrainData.size.y;
        hillScale = 1f;
        if (resourcePlacer != null && resourcePlacer.RiverRoot != null)
        {
            riverBaseScaleY = resourcePlacer.RiverRoot.localScale.y;
            riverBaseY = resourcePlacer.RiverRoot.position.y;
        }
        if (panel == null) BuildUi();
        else if (hillSlider != null) hillSlider.SetValueWithoutNotify(1f);   // world was rebuilt
    }

    void OnEnable() => EnhancedTouchSupport.Enable();

    void OnDisable()
    {
        TerrainCameraController.PrimaryPointerCaptured = false;
        if (ring != null) ring.enabled = false;
        if (dot != null) dot.enabled = false;
    }

    void OnDestroy()
    {
        if (ring != null) Destroy(ring.gameObject);
        if (dot != null) Destroy(dot.gameObject);
        if (panel != null) Destroy(panel.root.gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => TerrainCameraController.PrimaryPointerCaptured = false;

    // ---------- Per frame ----------

    // LateUpdate + late execution order: runs after TerrainCameraController has moved the camera this
    // frame, so the cursor ray matches what is rendered (otherwise the brush trails the cursor).
    void LateUpdate()
    {
        if (terrain == null) return;
        UpdateToggleButton();
        PlaceToggle();

        bool editing = panel != null && panel.gameObject.activeSelf && tool != Tool.Look;
        TerrainCameraController.PrimaryPointerCaptured = editing;

        bool hasPointer = ReadPointer(out Vector2 screen, out bool down, out bool pressedThisFrame);
        // Only our own panel blocks painting (the EventSystem check can report "over UI" for full-screen HUD layers).
        bool overUi = hasPointer && OverOwnUi(screen);
        if (pressedThisFrame) stroking = editing && !overUi;
        if (!down) stroking = false;

        bool onGround = false;
        Vector3 hitPoint = default;
        var cam = ViewCamera();
        bool usedCollider = false;
        if (editing && hasPointer && cam != null)
        {
            // Unity's own terrain raycast first (kept in sync by SetHeights, and re-synced after Hill Height);
            // the hand-written heightmap march is only a fallback.
            var ray = cam.ScreenPointToRay(screen);
            if (terrainCollider != null && terrainCollider.enabled && terrainCollider.Raycast(ray, out var hit, 10000f))
            {
                onGround = true;
                hitPoint = hit.point;
                usedCollider = true;
            }
            else onGround = RaycastHeightmap(ray, out hitPoint);
        }

        if (pressedThisFrame && editing && !overUi)
        {
            SetStatus(onGround ? $"{tool}: painting" : $"{tool}: pointer is not over the terrain");
            if (!onGround) Debug.Log($"[TerrainEdit] {tool} stroke started but the pointer ray missed the terrain.", this);
            if (!loggedPointer && cam != null)   // one-off check of the aim
            {
                loggedPointer = true;
                RaycastHeightmap(cam.ScreenPointToRay(screen), out var marched);
                Debug.Log($"[TerrainEdit] Pointer {screen} | Screen {Screen.width}x{Screen.height} | hit {hitPoint} ({(usedCollider ? "collider" : "heightmap")}) | heightmap march {marched} | camera {cam.transform.position}", this);
            }
        }
        if (!down && wasStroking) SetStatus(tool == Tool.Look ? "" : $"{tool}: drag on the ground to paint");
        wasStroking = stroking;

        UpdateRing(editing && onGround && !(overUi && !stroking), hitPoint);

        if (stroking && onGround)
        {
            if (pressedThisFrame && tool == Tool.Flatten)
            {
                var uv = terrainBuilder.WorldToUV(hitPoint);
                flattenTarget = terrain.terrainData.GetInterpolatedHeight(uv.x, uv.y) / terrain.terrainData.size.y;
            }
            ApplyBrush(hitPoint, Time.unscaledDeltaTime);
        }

        if (rebakeAt > 0f && Time.unscaledTime >= rebakeAt && !baking && !stroking) StartCoroutine(Rebake());
    }

    // Ray against the heightmap itself (not the physics collider, which can lag behind edits).
    bool RaycastHeightmap(Ray ray, out Vector3 point)
    {
        point = default;
        var td = terrain.terrainData;
        var origin = terrain.GetPosition();
        var bounds = new Bounds(origin + td.size * 0.5f, td.size + Vector3.up * 2f);
        if (!bounds.IntersectRay(ray, out float t)) return false;
        t = Mathf.Max(0f, t);   // camera inside the box: start at the camera, not behind it

        float step = Mathf.Max(0.25f, Mathf.Min(td.size.x, td.size.z) / 400f);
        float maxT = t + td.size.magnitude * 1.5f;
        float prevT = t;
        for (; t <= maxT; t += step)
        {
            var p = ray.GetPoint(t);
            if (p.x < origin.x || p.z < origin.z || p.x > origin.x + td.size.x || p.z > origin.z + td.size.z)
            {
                prevT = t;
                continue;
            }
            if (p.y <= origin.y + terrain.SampleHeight(p))
            {
                // Refine between the last point above ground and this one below it.
                float a = prevT, b = t;
                for (int i = 0; i < 12; i++)
                {
                    float m = (a + b) * 0.5f;
                    var q = ray.GetPoint(m);
                    if (q.y <= origin.y + terrain.SampleHeight(q)) b = m; else a = m;
                }
                point = ray.GetPoint(b);
                return true;
            }
            prevT = t;
        }
        return false;
    }

    // The camera the player actually looks through: the one TerrainCameraController drives.
    // Camera.main can be a different, unmoving camera (seen at Unity's default (0, 1, -10)).
    Camera viewCamera;
    Camera ViewCamera()
    {
        if (viewCamera != null && viewCamera.isActiveAndEnabled) return viewCamera;
        var controller = FindAnyObjectByType<TerrainCameraController>();
        viewCamera = controller != null ? controller.GetComponent<Camera>() : null;
        if (viewCamera == null) viewCamera = Camera.main;
        return viewCamera;
    }

    bool OverOwnUi(Vector2 screen)
    {
        if (toggleRect != null && toggleRect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(toggleRect, screen, null)) return true;
        return panel != null && panel.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(panel, screen, null);
    }

    void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    // Mouse left button, or the first finger when exactly one is down.
    static bool ReadPointer(out Vector2 pos, out bool down, out bool pressed)
    {
        var touches = Touch.activeTouches;
        if (Touchscreen.current != null && touches.Count > 0)
        {
            var t = touches[0];
            pos = t.screenPosition;
            down = touches.Count == 1 && t.phase != UnityEngine.InputSystem.TouchPhase.Ended && t.phase != UnityEngine.InputSystem.TouchPhase.Canceled;
            pressed = touches.Count == 1 && t.phase == UnityEngine.InputSystem.TouchPhase.Began;
            return true;
        }
        var mouse = Mouse.current;
        if (mouse == null) { pos = default; down = pressed = false; return false; }
        pos = mouse.position.ReadValue();
        down = mouse.leftButton.isPressed;
        pressed = mouse.leftButton.wasPressedThisFrame;
        return true;
    }

    // ---------- Brushes ----------

    void ApplyBrush(Vector3 centre, float dt)
    {
        switch (tool)
        {
            case Tool.Raise:
            case Tool.Lower:
            case Tool.Flatten:
            case Tool.Smooth:
                Sculpt(centre, dt);
                break;
            case Tool.AddGrass:
                PaintGrass(centre, dt);
                break;
            case Tool.RemoveGrass:
                RemoveGrass(centre);
                break;
        }
    }

    void Sculpt(Vector3 centre, float dt)
    {
        var td = terrain.terrainData;
        int res = td.heightmapResolution;
        Vector2 uv = terrainBuilder.WorldToUV(centre);
        float cx = uv.x * (res - 1), cy = uv.y * (res - 1);
        float rx = brushSize / td.size.x * (res - 1), ry = brushSize / td.size.z * (res - 1);

        // One extra cell round the edge so Smooth can read neighbours.
        int x0 = Mathf.Clamp(Mathf.FloorToInt(cx - rx) - 1, 0, res - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(cx + rx) + 1, 0, res - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(cy - ry) - 1, 0, res - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(cy + ry) + 1, 0, res - 1);
        int w = x1 - x0 + 1, h = y1 - y0 + 1;
        if (w < 2 || h < 2) return;

        var before = CollectPatchGround(centre, brushSize + 3f);

        var heights = td.GetHeights(x0, y0, w, h);   // [row = y, col = x]
        var src = tool == Tool.Smooth ? (float[,])heights.Clone() : heights;
        float rate = strength * dt;

        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                float dx = (x0 + i - cx) / rx, dy = (y0 + j - cy) / ry;
                float d2 = dx * dx + dy * dy;
                if (d2 >= 1f) continue;
                float falloff = (1f - d2) * (1f - d2);   // soft edge
                float v = heights[j, i];

                switch (tool)
                {
                    case Tool.Raise: v += sculptSpeed * rate * falloff; break;
                    case Tool.Lower: v -= sculptSpeed * rate * falloff; break;
                    case Tool.Flatten: v = Mathf.Lerp(v, flattenTarget, Mathf.Clamp01(rate * 6f * falloff)); break;
                    case Tool.Smooth:
                        if (i == 0 || j == 0 || i == w - 1 || j == h - 1) break;
                        float avg = (src[j - 1, i - 1] + src[j - 1, i] + src[j - 1, i + 1] +
                                     src[j, i - 1] + src[j, i] + src[j, i + 1] +
                                     src[j + 1, i - 1] + src[j + 1, i] + src[j + 1, i + 1]) / 9f;
                        v = Mathf.Lerp(v, avg, Mathf.Clamp01(rate * 12f * falloff));
                        break;
                }
                heights[j, i] = Mathf.Clamp01(v);
            }
        }

        td.SetHeights(x0, y0, heights);

        // Repaint the brushed area: ground lowered under the snow line turns to grass, steep ground to rock.
        if (splatPainter != null)
            splatPainter.PaintRegion(terrainBuilder, Rect.MinMaxRect(
                (float)x0 / (res - 1), (float)y0 / (res - 1), (float)x1 / (res - 1), (float)y1 / (res - 1)));
        RestorePatchGround(before);
        MarkDirty();
    }

    void PaintGrass(Vector3 centre, float dt)
    {
        if (resourcePlacer == null) return;
        grassTimer -= dt;
        if (grassTimer > 0f) return;
        grassTimer = Mathf.Lerp(0.25f, 0.03f, strength);   // stronger = denser, faster

        // A few random tries inside the brush; keep only spots with room for a clump.
        for (int attempt = 0; attempt < 6; attempt++)
        {
            var off = Random.insideUnitCircle * brushSize;
            var p = centre + new Vector3(off.x, 0f, off.y);
            var uv = terrainBuilder.WorldToUV(p);
            if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f) continue;
            p = terrainBuilder.WorldPosition(uv.x, uv.y);
            if (HimalayaRiver.Exists && HimalayaRiver.DistanceTo(p) < resourcePlacer.RiverClearance) continue;
            if (GrassNear(p, grassSpacing)) continue;
            resourcePlacer.SpawnGrassAt(p);
            return;
        }
    }

    void RemoveGrass(Vector3 centre)
    {
        if (resourcePlacer == null) return;
        var grass = resourcePlacer.GrassResource;
        float r2 = brushSize * brushSize;
        var doomed = new List<ResourcePatch>();
        foreach (var patch in ResourcePatch.All)
        {
            if (patch == null || patch.Resource != grass) continue;
            var d = patch.transform.position - centre; d.y = 0f;
            if (d.sqrMagnitude <= r2) doomed.Add(patch);
        }
        foreach (var patch in doomed) Destroy(patch.gameObject);   // animals heading there null-check and pick another
    }

    bool GrassNear(Vector3 p, float spacing)
    {
        var grass = resourcePlacer.GrassResource;
        float s2 = spacing * spacing;
        foreach (var patch in ResourcePatch.All)
        {
            if (patch == null || patch.Resource != grass) continue;
            var d = patch.transform.position - p; d.y = 0f;
            if (d.sqrMagnitude < s2) return true;
        }
        return false;
    }

    // ---------- Hill height (whole map) ----------

    void SetHillScale(float scale)
    {
        if (terrain == null || Mathf.Approximately(scale, hillScale)) return;
        var before = CollectPatchGround(Vector3.zero, -1f);

        var td = terrain.terrainData;
        var size = td.size;
        td.size = new Vector3(size.x, Mathf.Max(1f, baseHeight * scale), size.z);
        hillScale = scale;
        repaintWholeMap = true;   // slopes changed everywhere; repainted once the slider settles
        RefreshCollider();        // so the brush aims at the new surface straight away

        // The river mesh is in world space: stretch it with the ground around the terrain's base.
        if (resourcePlacer != null && resourcePlacer.RiverRoot != null)
        {
            var river = resourcePlacer.RiverRoot;
            float baseY = terrain.transform.position.y;
            var s = river.localScale;
            river.localScale = new Vector3(s.x, riverBaseScaleY * scale, s.z);
            var pos = river.position;
            river.position = new Vector3(pos.x, baseY + (riverBaseY - baseY) * scale, pos.z);
        }

        RestorePatchGround(before);
        MarkDirty();
    }

    // ---------- Keep patches on the ground ----------

    struct PatchGround { public Transform t; public float ground; }

    // Ground height under every patch near `centre` (radius < 0 = whole map), before an edit.
    List<PatchGround> CollectPatchGround(Vector3 centre, float radius)
    {
        var list = new List<PatchGround>();
        float r2 = radius * radius;
        foreach (var patch in ResourcePatch.All)
        {
            if (patch == null) continue;
            var p = patch.transform.position;
            if (radius >= 0f) { var d = p - centre; d.y = 0f; if (d.sqrMagnitude > r2) continue; }
            list.Add(new PatchGround { t = patch.transform, ground = terrain.SampleHeight(p) });
        }
        return list;
    }

    // Moves each patch by however much the ground under it changed, so it keeps its offset.
    void RestorePatchGround(List<PatchGround> before)
    {
        foreach (var pg in before)
        {
            if (pg.t == null) continue;
            var p = pg.t.position;
            p.y += terrain.SampleHeight(p) - pg.ground;
            pg.t.position = p;
        }
    }

    // ---------- NavMesh ----------

    void MarkDirty() => rebakeAt = Time.unscaledTime + rebakeDelay;

    // The NavMesh and the brush raycast use the physics collider. Re-assigning the TerrainData forces the
    // TerrainCollider to rebuild from the current heights and size (a size change alone isn't picked up).
    void RefreshCollider()
    {
        if (terrainCollider == null) return;
        var td = terrain.terrainData;
        terrainCollider.terrainData = null;
        terrainCollider.terrainData = td;
        Physics.SyncTransforms();
    }

    IEnumerator Rebake()
    {
        rebakeAt = -1f;
        if (navSurface == null)
        {
            Debug.LogWarning("[TerrainEdit] No NavMeshSurface given; animals will keep walking on the old ground.", this);
            yield break;
        }
        baking = true;
        SetStatus("Updating where animals can walk...");

        if (repaintWholeMap && splatPainter != null)
        {
            repaintWholeMap = false;
            splatPainter.PaintRegion(terrainBuilder, new Rect(0f, 0f, 1f, 1f));
        }

        RefreshCollider();
        yield return new WaitForFixedUpdate();

        if (navSurface.navMeshData == null) navSurface.BuildNavMesh();
        else
        {
            var op = navSurface.UpdateNavMesh(navSurface.navMeshData);   // background, no hitch
            while (op != null && !op.isDone) yield return null;
        }

        SnapAgentsToNavMesh();
        baking = false;
        SetStatus(tool == Tool.Look ? "" : $"{tool}: drag on the ground to paint");
        Debug.Log("[TerrainEdit] NavMesh updated for the edited terrain.", this);
    }

    // Agents keep their old height after the NavMesh moves under them; put each back on the new
    // surface and keep its current destination.
    static void SnapAgentsToNavMesh()
    {
        foreach (var agent in FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None))
        {
            if (agent == null || !agent.enabled || !agent.gameObject.activeInHierarchy) continue;
            if (!UnityEngine.AI.NavMesh.SamplePosition(agent.transform.position, out var hit, 50f, UnityEngine.AI.NavMesh.AllAreas)) continue;
            if (agent.isOnNavMesh && Mathf.Abs(hit.position.y - agent.nextPosition.y) < 0.3f) continue;

            bool hadPath = agent.isOnNavMesh && agent.hasPath;
            var dest = hadPath ? agent.destination : default;
            agent.Warp(hit.position);
            if (hadPath && agent.isOnNavMesh) agent.SetDestination(dest);
        }
    }

    // ---------- Brush ring ----------

    void UpdateRing(bool show, Vector3 centre)
    {
        if (ring == null)
        {
            var go = new GameObject("Terrain Brush Ring");
            ring = go.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.positionCount = 48;
            ring.widthMultiplier = 0.35f;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            ring.material = new Material(shader);
        }
        if (dot == null)
        {
            dot = Instantiate(ring.gameObject).GetComponent<LineRenderer>();
            dot.name = "Terrain Brush Centre";
            dot.positionCount = 16;
            dot.widthMultiplier = 0.2f;
            dot.material = ring.material;
        }
        ring.enabled = dot.enabled = show;
        if (!show) return;

        var c = tool == Tool.RemoveGrass || tool == Tool.Lower ? new Color(1f, 0.4f, 0.3f) : new Color(1f, 1f, 1f);
        ring.material.color = c;
        ring.startColor = ring.endColor = c;
        dot.startColor = dot.endColor = c;
        DrawCircle(ring, centre, brushSize);
        DrawCircle(dot, centre, Mathf.Max(0.4f, brushSize * 0.04f));   // marks the exact point under the cursor
    }

    void DrawCircle(LineRenderer lr, Vector3 centre, float radius)
    {
        float baseY = terrain.transform.position.y;
        for (int i = 0; i < lr.positionCount; i++)
        {
            float a = i / (float)lr.positionCount * Mathf.PI * 2f;
            var p = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            p.y = terrain.SampleHeight(p) + baseY + 0.1f;   // hug the ground so the angle of view doesn't shift it
            lr.SetPosition(i, p);
        }
    }

    // ---------- UI ----------

    void BuildUi()
    {
        // [AI-GENERATED] Claude Code (Claude Opus 5.5): in every build since 3 Nov (was Editor / development
        // only): players can reshape the land and watch how the animals respond.

        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        var canvasGo = new GameObject("TerrainEditCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = HudReference;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): toggle button, top right, right under the
        // season / day / weather / temperature pill, styled like the HUD's own buttons.
        toggleButton = MakeButton(canvasGo.transform, "Edit Terrain", Color.white, TogglePanel, out toggleLabel, out toggleImage);
        var tr = (RectTransform)toggleButton.transform;
        toggleRect = tr;
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(1f, 1f);
        tr.anchoredPosition = new Vector2(-40f, -170f - 84f - 20f);   // pill top, pill height, gap
        tr.sizeDelta = new Vector2(330f, 88f);                          // Field Journal button size
        StyleLikeHud();

        // Panel under it.
        panel = NewRect("TerrainEditPanel", canvasGo.transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-40f, tr.anchoredPosition.y - tr.sizeDelta.y - 16f);
        panel.localScale = Vector3.one * PanelScale;
        panel.sizeDelta = new Vector2(340f, 0f);
        panel.gameObject.AddComponent<Image>().color = PanelColor;
        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 8f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        MakeLabel(panel, "Brushes", 22f, FontStyles.Bold);
        var grid = NewRect("Tools", panel);
        var g = grid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(150f, 48f);
        g.spacing = new Vector2(8f, 8f);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 2;
        grid.gameObject.AddComponent<LayoutElement>().preferredHeight = 4 * 48f + 3 * 8f;

        AddToolButton(grid, Tool.Look, "Look");
        AddToolButton(grid, Tool.Smooth, "Smooth");
        AddToolButton(grid, Tool.Raise, "Raise");
        AddToolButton(grid, Tool.Lower, "Lower");
        AddToolButton(grid, Tool.Flatten, "Flatten");
        AddToolButton(grid, Tool.RemoveGrass, "Remove Grass");
        AddToolButton(grid, Tool.AddGrass, "Add Grass");

        MakeSlider(panel, "Brush Size", brushSizeRange.x, brushSizeRange.y, brushSize, v => brushSize = v, v => $"{v:0}");
        MakeSlider(panel, "Strength", 0.05f, 1f, strength, v => strength = v, v => $"{v * 100f:0}%");

        MakeLabel(panel, "Whole map", 22f, FontStyles.Bold);
        hillSlider = MakeSlider(panel, "Hill Height", hillHeightRange.x, hillHeightRange.y, hillScale, SetHillScale, v => $"{v:0.00}x");

        MakeLabel(panel, "Paint with left-drag / one finger.\nRight-drag, pinch & wheel still move the camera.", 16f, FontStyles.Italic);
        statusText = MakeLabel(panel, "", 16f, FontStyles.Normal);
        statusText.color = new Color(0.7f, 1f, 0.75f);

        panel.gameObject.SetActive(false);
        SelectTool(Tool.Look);
    }

    void TogglePanel()
    {
        bool open = !panel.gameObject.activeSelf;
        panel.gameObject.SetActive(open);
        toggleLabel.text = open ? "Close Editor" : "Edit Terrain";
        if (!open) SelectTool(Tool.Look);

        // Distant terrain is drawn simplified (pixel error), so the visible surface can sit a few units off
        // the real heightmap the brush aims at; seen at an angle that looks like the brush is beside the
        // cursor. Draw it at full detail while editing, and restore the normal setting afterwards.
        if (terrain != null)
        {
            if (open) { savedPixelError = terrain.heightmapPixelError; terrain.heightmapPixelError = 1f; }
            else if (savedPixelError > 0f) terrain.heightmapPixelError = savedPixelError;
        }
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5)
    // With Shumin's HUD the season pill is hidden (the day panel shows it), so sit right under the top row
    // of HUD buttons instead of leaving a gap; the panel follows.
    void PlaceToggle()
    {
        if (toggleRect == null) return;
        float y = SimHud.Active ? -40f - 88f - 20f : -170f - 84f - 20f;   // top row (88 tall) + gap, or under the pill
        if (Mathf.Approximately(toggleRect.anchoredPosition.y, y)) return;
        toggleRect.anchoredPosition = new Vector2(-40f, y);
        if (panel != null) panel.anchoredPosition = new Vector2(-40f, y - toggleRect.sizeDelta.y - 16f);
    }

    void UpdateToggleButton()
    {
        if (toggleRect == null) return;

        // The HUD scene loads a moment after the world: once it is there, copy its button's exact sprite and font.
        if (!hudLookCopied && Time.unscaledTime >= nextHudLookTry)
        {
            nextHudLookTry = Time.unscaledTime + 0.5f;
            hudLookCopied = CopyHudLook();
        }

        // The animal card opens on the right, just under the pill: get out of its way (and off its close button)
        // unless the editor itself is open.
        var info = AnimalInfoUI.Instance;
        bool cardOpen = info != null && info.SelectedAnimal != null;
        bool show = !cardOpen || panel.gameObject.activeSelf;
        if (toggleRect.gameObject.activeSelf != show) toggleRect.gameObject.SetActive(show);
    }

    // White rounded button with dark grey text and the HUD's colour tint, before the HUD's own sprite is found.
    void StyleLikeHud()
    {
        toggleImage.color = Color.white;
        toggleImage.sprite = RoundedSprite();
        toggleImage.type = Image.Type.Sliced;
        var colors = toggleButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = colors.selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        toggleButton.colors = colors;
        toggleLabel.color = HudTextColor;
        toggleLabel.fontSize = 24f;
    }

    bool CopyHudLook()
    {
        var src = GameObject.Find("FieldJournalBtn");
        if (src == null || !src.TryGetComponent(out Image img) || img.sprite == null) return false;
        toggleImage.sprite = img.sprite;
        toggleImage.type = img.type;
        toggleImage.pixelsPerUnitMultiplier = img.pixelsPerUnitMultiplier;
        if (src.TryGetComponent(out Button b)) toggleButton.colors = b.colors;
        var text = src.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            toggleLabel.font = text.font;
            toggleLabel.color = text.color;
            toggleLabel.fontSize = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
        }
        return true;
    }

    static Sprite roundedSprite;
    static Sprite RoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;
        const int size = 32;
        const float r = 10f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(r - (x + 0.5f), x + 0.5f - (size - r)));
                float dy = Mathf.Max(0f, Mathf.Max(r - (y + 0.5f), y + 0.5f - (size - r)));
                float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);   // soft edge
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        return roundedSprite;
    }
    // [AI-GENERATED END]

    void AddToolButton(Transform parent, Tool t, string label)
    {
        MakeButton(parent, label, ButtonColor, () => SelectTool(t), out _, out var img);
        toolButtons[t] = img;
    }

    void SelectTool(Tool t)
    {
        tool = t;
        stroking = false;
        SetStatus(t == Tool.Look ? "" : $"{t}: drag on the ground to paint");
        foreach (var kv in toolButtons) kv.Value.color = kv.Key == t ? ActiveColor : ButtonColor;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static TMP_Text MakeLabel(Transform parent, string text, float size, FontStyles style)
    {
        var r = NewRect("Label", parent);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(Transform parent, string text, Color color, UnityEngine.Events.UnityAction onClick, out TMP_Text label, out Image image)
    {
        var r = NewRect(text, parent);
        image = r.gameObject.AddComponent<Image>();
        image.color = color;
        var b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = image;
        b.onClick.AddListener(onClick);

        var lr = NewRect("Text", r);
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        label = lr.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return b;
    }

    static Slider MakeSlider(Transform parent, string name, float min, float max, float value,
                           System.Action<float> onChange, System.Func<float, string> format)
    {
        var row = NewRect(name, parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

        var title = MakeLabel(row, name, 18f, FontStyles.Normal);
        var trt = (RectTransform)title.transform;
        trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(0f, 26f); trt.anchoredPosition = Vector2.zero;

        var valueText = MakeLabel(row, format(value), 18f, FontStyles.Normal);
        valueText.alignment = TextAlignmentOptions.Right;
        var vrt = (RectTransform)valueText.transform;
        vrt.anchorMin = trt.anchorMin; vrt.anchorMax = trt.anchorMax; vrt.pivot = trt.pivot;
        vrt.sizeDelta = trt.sizeDelta; vrt.anchoredPosition = Vector2.zero;

        // Standard uGUI slider: background, fill, handle.
        var sr = NewRect("Slider", row);
        sr.anchorMin = new Vector2(0f, 0f); sr.anchorMax = new Vector2(1f, 0f); sr.pivot = new Vector2(0.5f, 0f);
        sr.sizeDelta = new Vector2(-16f, 28f); sr.anchoredPosition = new Vector2(0f, 4f);
        var slider = sr.gameObject.AddComponent<Slider>();

        var bg = NewRect("Background", sr);
        bg.anchorMin = new Vector2(0f, 0.35f); bg.anchorMax = new Vector2(1f, 0.65f); bg.offsetMin = bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        var fillArea = NewRect("Fill Area", sr);
        fillArea.anchorMin = new Vector2(0f, 0.35f); fillArea.anchorMax = new Vector2(1f, 0.65f); fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
        var fill = NewRect("Fill", fillArea);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = ActiveColor;

        var handleArea = NewRect("Handle Slide Area", sr);
        handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one; handleArea.offsetMin = handleArea.offsetMax = Vector2.zero;
        var handle = NewRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(28f, 0f);
        var handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => { onChange(v); valueText.text = format(v); });
        return slider;
    }
}

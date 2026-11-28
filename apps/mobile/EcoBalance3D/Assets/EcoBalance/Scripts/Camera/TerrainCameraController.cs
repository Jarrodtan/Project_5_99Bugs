// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Free camera for the Himalaya terrain, built for an Android tablet first.
///
/// Touch:  1 finger drag = pan · pinch = zoom · twist = rotate · 2 fingers up/down = tilt
///         tap an animal = follow it (3rd person) · tap empty ground = stop following
/// Mouse:  left drag = pan · right drag = rotate + tilt · scroll = zoom · click animal = follow
/// Keys:   WASD = pan · Q/E = rotate · R/F = tilt · Z/X = zoom · V = toggle POV · Esc = stop following
///
/// UI buttons can call <see cref="Follow"/>, <see cref="StopFollowing"/>, <see cref="TogglePov"/>
/// and <see cref="ResetView"/>.
/// </summary>
public class TerrainCameraController : MonoBehaviour
{
    [Header("Terrain")]
    [Tooltip("The Himalaya terrain. Used for the start point, edge limits and ground height. Empty = Terrain.activeTerrain.")]
    [SerializeField] Terrain terrain;
    [Tooltip("Camera never goes lower than this above the ground.")]
    [SerializeField] float minHeightAboveGround = 2f;
    [Tooltip("The point the camera looks at stays this far inside the terrain's edges.")]
    [SerializeField] float edgeMargin = 35f;

    [Header("Zoom (distance from the focus point)")]
    [SerializeField] float minDistance = 3f;
    [SerializeField] float maxDistance = 110f;
    [SerializeField] float startDistance = 110f;
    [Tooltip("Mouse wheel: fraction of the distance per notch.")]
    [SerializeField] float wheelZoomStep = 0.12f;

    [Header("Angles")]
    [SerializeField] float minPitch = 8f;
    [SerializeField] float maxPitch = 70f;
    [SerializeField] float startPitch = 30f;
    [SerializeField] float startYaw = 0f;
    [Tooltip("Degrees per pixel for rotate/tilt drags.")]
    [SerializeField] float rotateSpeed = 0.25f;

    [Header("Feel")]
    [Tooltip("Smaller = snappier, larger = smoother.")]
    [SerializeField] float smoothTime = 0.12f;
    [Tooltip("Keyboard pan speed, per unit of distance, per second.")]
    [SerializeField] float keyPanSpeed = 0.8f;

    [Header("Opening shot")]
    [Tooltip("Start high above the mountains, then glide down into the valley where the grass is.")]
    [SerializeField] bool introFlyIn = true;
    [SerializeField] float introStartDistance = 140f;
    [SerializeField] float introStartPitch = 60f;
    [Tooltip("Smooth time during the fly-in. Larger = slower, gentler glide.")]
    [SerializeField] float introSmoothTime = 1.1f;
    [SerializeField] float introSeconds = 4f;

    [Header("Follow / POV")]
    [Tooltip("Distance used when following an animal starts.")]
    [SerializeField] float followDistance = 12f;
    [Tooltip("Camera angle above the ground while following (low = more cinematic).")]
    [SerializeField] float followPitch = 18f;
    [Tooltip("How quickly the camera swings round behind a followed animal as it turns (0 = never).")]
    [SerializeField] float followTurnSpeed = 1.5f;
    [Tooltip("Eye height above the animal's pivot in POV mode.")]
    [SerializeField] float povEyeHeight = 1.2f;
    [SerializeField] LayerMask tapMask = ~0;

    // Targets (what input changes) and current values (what the camera shows, eased toward targets).
    Vector3 focusTarget, focus, focusVel;
    float yawTarget, yaw, yawVel;
    float pitchTarget, pitch, pitchVel;
    float distTarget, dist, distVel;

    Transform followTarget;
    bool pov;
    bool introStarted;
    float introTimeLeft;

    Camera cam;
    Vector2 tapStart;
    bool tapCandidate;
    bool gestureOverUI;

    public Transform FollowTarget => followTarget;
    public bool IsPov => pov;

    /// <summary>
    /// [AI-GENERATED] Claude Code (Claude Opus 5.5): set by TerrainEditTool while a brush is active, so a
    /// left-drag / one-finger drag paints instead of panning. Right/middle drag, pinch, twist, wheel and keys still work.
    /// </summary>
    public static bool PrimaryPointerCaptured;

    void Awake() => cam = GetComponent<Camera>();

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Start() => ResetView();

    public void ResetView()
    {
        followTarget = null;
        pov = false;
        if (terrain == null) terrain = Terrain.activeTerrain;
        // M3 bigger map: the overview zoom grows with the map (80 at the old 400-wide map, 200 at 1000).
        if (terrain != null && terrain.terrainData != null) maxDistance = Mathf.Max(maxDistance, terrain.terrainData.size.x * 0.2f);
        focusTarget = HasTerrain ? terrain.GetPosition() + terrain.terrainData.size * 0.5f : Vector3.zero;
        focusTarget.y = GroundHeight(focusTarget);
        yawTarget = startYaw;
        pitchTarget = startPitch;
        distTarget = startDistance;
        focus = focusTarget; yaw = yawTarget; pitch = pitchTarget; dist = distTarget;
    }

    // Once grass exists (the world has been built): jump high, then let the easing glide the
    // camera down to a low view over the grass patch nearest the middle of all the grass.
    void StartIntro()
    {
        introStarted = true;

        Vector3 centre = Vector3.zero;
        foreach (var p in ResourcePatch.All) centre += p.transform.position;
        centre /= ResourcePatch.All.Count;

        Vector3 best = centre;
        float bestSqr = float.PositiveInfinity;
        foreach (var p in ResourcePatch.All)
        {
            float d = (p.transform.position - centre).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = p.transform.position; }
        }

        focusTarget = best;
        yawTarget = startYaw; pitchTarget = startPitch; distTarget = startDistance;
        dist = introStartDistance; pitch = introStartPitch;
        distVel = pitchVel = 0f;
        introTimeLeft = introSeconds;
    }

    public void Follow(Transform target)
    {
        followTarget = target;
        if (target == null) return;
        // Documentary shot: close, low, just behind the animal.
        distTarget = followDistance;
        pitchTarget = followPitch;
        yawTarget = target.eulerAngles.y;
    }

    // [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande
    public void Follow(Transform target, float distance)
    {
        Follow(target);
        distTarget = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    // [AI-GENERATED] Cursor (Grok 4.7) - reviewed by Yande
    public void FrameOverview()
    {
        followTarget = null;
        pov = false;
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain == null) return;
        focusTarget = terrain.GetPosition() + terrain.terrainData.size * 0.5f;
        focusTarget.y = GroundHeight(focusTarget);
        yawTarget = startYaw;
        pitchTarget = 50f;
        distTarget = maxDistance;
    }

    public void StopFollowing()
    {
        followTarget = null;
        pov = false;
    }

    /// <summary>For on-screen + / − buttons (also the only zoom in Unity's Device Simulator, which has no pinch).</summary>
    public void ZoomIn() => distTarget *= 0.7f;
    public void ZoomOut() => distTarget /= 0.7f;

    public void TogglePov()
    {
        if (followTarget == null) { pov = false; return; }
        pov = !pov;
        // In POV, yaw/pitch are a look offset from where the animal faces, so start straight ahead.
        // Back in 3rd person, start behind the animal.
        yawTarget = yaw = pov ? 0f : followTarget.eulerAngles.y;
        pitchTarget = pitch = pov ? 5f : 25f;
        yawVel = pitchVel = 0f;
    }

    // [AI-GENERATED] Claude Code (Claude Opus 5.5): M3 3D audio. The camera sits 30-200 units up, so every 3D sound
    // (river, animals) was out of range. Hear from the point the camera looks at (or the animal it follows) instead.
    Transform ear;
    void UpdateEar()
    {
        if (ear == null)
        {
            var own = GetComponentInChildren<AudioListener>();
            if (own == null) own = FindFirstObjectByType<AudioListener>();
            if (own != null && own.transform == transform) own.enabled = false;
            else if (own != null) { ear = own.transform; ear.SetParent(null); }
            if (ear == null) ear = new GameObject("[Ears]", typeof(AudioListener)).transform;
        }
        // Zoomed in: hear from the ground. Zoomed out (Overview / god view): the ears rise towards the camera, so
        // nearby sounds fade and the world sounds far away, like looking down from the sky.
        Vector3 ground = (followTarget != null ? followTarget.position : focus) + Vector3.up * 2f;
        float zoom = Mathf.InverseLerp(minDistance, maxDistance, dist);
        ear.position = Vector3.Lerp(ground, transform.position, zoom * 0.75f);
        ear.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);   // left/right still match the screen
    }

    void LateUpdate()
    {
        UpdateEar();
        // Terrain may be created after Start (it's built at runtime).
        if (terrain == null && Terrain.activeTerrain != null) { terrain = Terrain.activeTerrain; ResetView(); }
        if (followTarget != null && !followTarget.gameObject.activeInHierarchy) StopFollowing();
        if (introFlyIn && !introStarted && ResourcePatch.All.Count > 0) StartIntro();
        if (introTimeLeft > 0f) introTimeLeft -= Time.unscaledDeltaTime;

        if (Touchscreen.current != null && Touch.activeTouches.Count > 0) HandleTouch();
        else HandleMouse();
        HandleScroll();
        HandleKeyboard();

        if (followTarget != null)
        {
            focusTarget = followTarget.position;
            // Drift round behind the animal as it turns, unless the player is twisting the view themselves.
            if (!pov && Touch.activeTouches.Count < 2 && (Mouse.current == null || !Mouse.current.leftButton.isPressed))
                yawTarget = Mathf.MoveTowardsAngle(yawTarget, followTarget.eulerAngles.y,
                    Mathf.Abs(Mathf.DeltaAngle(yawTarget, followTarget.eulerAngles.y)) * followTurnSpeed * Time.unscaledDeltaTime);
        }
        ClampTargets();
        Ease();
        Apply();
    }

    // ---------- Input ----------

    void HandleTouch()
    {
        var touches = Touch.activeTouches;

        // A gesture that starts on a button belongs to the UI, not the camera.
        if (touches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
            gestureOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touches[0].touchId);
        if (gestureOverUI)
        {
            if (touches.Count == 1 && touches[0].phase == UnityEngine.InputSystem.TouchPhase.Ended) gestureOverUI = false;
            return;
        }

        if (touches.Count == 1)
        {
            if (PrimaryPointerCaptured) { tapCandidate = false; return; }   // [AI-GENERATED] Claude Code (Claude Opus 5.5): brush owns one-finger drags
            var t = touches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began) { tapStart = t.screenPosition; tapCandidate = true; }
            if ((t.screenPosition - tapStart).sqrMagnitude > 20f * 20f) tapCandidate = false;

            if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved && !tapCandidate) Orbit(t.delta);   // [AI-GENERATED] Claude Code (Claude Opus 5.5): Google Earth style, one finger spins
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended && tapCandidate) Tap(t.screenPosition);
            return;
        }

        tapCandidate = false;
        var a = touches[0];
        var b = touches[1];
        Vector2 prevA = a.screenPosition - a.delta, prevB = b.screenPosition - b.delta;

        // Pinch: change in finger spread.
        float prevSpread = (prevA - prevB).magnitude, spread = (a.screenPosition - b.screenPosition).magnitude;
        if (prevSpread > 1f && spread > 1f) distTarget *= prevSpread / spread;

        // Twist: change in the angle between the fingers.
        float prevAngle = Mathf.Atan2((prevB - prevA).y, (prevB - prevA).x);
        float angle = Mathf.Atan2((b.screenPosition - a.screenPosition).y, (b.screenPosition - a.screenPosition).x);
        yawTarget -= Mathf.DeltaAngle(prevAngle * Mathf.Rad2Deg, angle * Mathf.Rad2Deg);

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): two fingers dragging together move across the map.
        Pan((a.delta + b.delta) * 0.5f);
    }

    void HandleMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool pressed = mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame;
        if (pressed) gestureOverUI = PointerOverUI.IsOver();
        if (gestureOverUI)
        {
            if (!mouse.leftButton.isPressed && !mouse.rightButton.isPressed && !mouse.middleButton.isPressed) gestureOverUI = false;
            return;
        }

        var pos = mouse.position.ReadValue();
        var delta = mouse.delta.ReadValue();

        if (!PrimaryPointerCaptured)   // [AI-GENERATED] Claude Code (Claude Opus 5.5): brush owns the left button
        {
            if (mouse.leftButton.wasPressedThisFrame) { tapStart = pos; tapCandidate = true; }
            if ((pos - tapStart).sqrMagnitude > 6f * 6f) tapCandidate = false;
            if (mouse.leftButton.isPressed && !tapCandidate) Pan(delta);
            if (mouse.leftButton.wasReleasedThisFrame && tapCandidate) Tap(pos);
        }

        if (mouse.middleButton.isPressed) Pan(delta);
        if (mouse.rightButton.isPressed) Orbit(delta);   // right drag = spin + tilt (was Pan, so the mouse had no rotation)
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    // Spin round the point being looked at (sideways) and tilt (up/down), like Google Earth.
    // In first person it looks around instead.
    void Orbit(Vector2 screenDelta)
    {
        yawTarget += screenDelta.x * rotateSpeed;
        pitchTarget -= screenDelta.y * rotateSpeed;
    }
    // [AI-GENERATED END]

    // Read on its own so the wheel still zooms while a (simulated) touch is active.
    void HandleScroll()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f && !PointerOverUI.IsOver())
            distTarget *= 1f - Mathf.Sign(scroll) * wheelZoomStep;
    }

    void HandleKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        float dt = Time.unscaledDeltaTime;

        var move = Vector2.zero;
        if (kb.wKey.isPressed) move.y += 1f;
        if (kb.sKey.isPressed) move.y -= 1f;
        if (kb.dKey.isPressed) move.x += 1f;
        if (kb.aKey.isPressed) move.x -= 1f;
        if (move != Vector2.zero)
        {
            followTarget = null; pov = false;
            focusTarget += YawRotation() * new Vector3(move.x, 0f, move.y) * (keyPanSpeed * distTarget * dt);
        }

        if (kb.qKey.isPressed) yawTarget += 90f * dt;
        if (kb.eKey.isPressed) yawTarget -= 90f * dt;
        if (kb.rKey.isPressed) pitchTarget += 45f * dt;
        if (kb.fKey.isPressed) pitchTarget -= 45f * dt;
        if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) ZoomIn();
        if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) ZoomOut();
        if (kb.zKey.isPressed) distTarget *= 1f - 1.5f * dt;
        if (kb.xKey.isPressed) distTarget *= 1f + 1.5f * dt;
        if (kb.vKey.wasPressedThisFrame) TogglePov();
        if (kb.escapeKey.wasPressedThisFrame) StopFollowing();
    }

    // Moves the focus so the ground follows the finger. Panning drops any follow.
    void Pan(Vector2 screenDelta)
    {
        if (pov) { yawTarget += screenDelta.x * rotateSpeed; pitchTarget -= screenDelta.y * rotateSpeed; return; }
        followTarget = null;
        float worldPerPixel = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
        focusTarget -= YawRotation() * new Vector3(screenDelta.x, 0f, screenDelta.y) * worldPerPixel;
    }

    void Tap(Vector2 screenPos)
    {
        var ray = cam.ScreenPointToRay(screenPos);
        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // Thin ray first; if it misses a small, moving animal, try a fatter one (fingers are not precise),
        // and open the card here too, since OnMouseDown on the animal never fires for touch.
        EcoEntity entity = null;
        if (Physics.Raycast(ray, out var hit, 1000f, tapMask, QueryTriggerInteraction.Collide))
            entity = hit.collider.GetComponentInParent<EcoEntity>();
        if (entity == null)
        {
            float best = float.PositiveInfinity;   // the sphere also touches the ground: take the nearest animal it touches
            foreach (var h in Physics.SphereCastAll(ray, 0.03f * dist + 0.3f, 1000f, tapMask, QueryTriggerInteraction.Collide))
            {
                var e = h.collider.GetComponentInParent<EcoEntity>();
                if (e != null && h.distance < best) { best = h.distance; entity = e; }
            }
        }
        if (entity != null)
        {
            Follow(entity.transform);
            var brain = entity.GetComponentInChildren<AnimalBrain>();
            if (brain != null && AnimalInfoUI.Instance != null) AnimalInfoUI.Instance.ShowAnimal(brain);
            return;
        }
        // [AI-GENERATED END]
        StopFollowing();
        if (AnimalInfoUI.Instance != null) AnimalInfoUI.Instance.HidePanel();   // tapping empty ground closes the animal card
    }

    // ---------- Camera maths ----------

    void ClampTargets()
    {
        distTarget = Mathf.Clamp(distTarget, minDistance, maxDistance);
        pitchTarget = Mathf.Clamp(pitchTarget, pov ? -30f : minPitch, maxPitch);

        if (HasTerrain && followTarget == null)
        {
            // Keep the view over the land: a margin from the edges, so the camera never shows the void beyond.
            var min = terrain.GetPosition() + new Vector3(edgeMargin, 0f, edgeMargin);
            var max = terrain.GetPosition() + terrain.terrainData.size - new Vector3(edgeMargin, 0f, edgeMargin);
            focusTarget.x = Mathf.Clamp(focusTarget.x, min.x, max.x);
            focusTarget.z = Mathf.Clamp(focusTarget.z, min.z, max.z);
            focusTarget.y = GroundHeight(focusTarget);
        }
    }

    void Ease()
    {
        float s = introTimeLeft > 0f ? introSmoothTime : smoothTime;
        float t = followTarget != null ? s * 0.5f : s;   // keep up with moving animals
        focus = Vector3.SmoothDamp(focus, focusTarget, ref focusVel, t, Mathf.Infinity, Time.unscaledDeltaTime);
        yaw = Mathf.SmoothDampAngle(yaw, yawTarget, ref yawVel, s, Mathf.Infinity, Time.unscaledDeltaTime);
        pitch = Mathf.SmoothDampAngle(pitch, pitchTarget, ref pitchVel, s, Mathf.Infinity, Time.unscaledDeltaTime);
        dist = Mathf.SmoothDamp(dist, distTarget, ref distVel, s, Mathf.Infinity, Time.unscaledDeltaTime);
    }

    void Apply()
    {
        if (pov && followTarget != null)
        {
            // First person: eyes on the animal, looking where it faces plus the player's look offset.
            transform.position = followTarget.position + Vector3.up * povEyeHeight;
            transform.rotation = Quaternion.Euler(pitch, followTarget.eulerAngles.y + yaw, 0f);
            return;
        }

        var rot = Quaternion.Euler(pitch, yaw, 0f);
        var pos = focus + rot * (Vector3.back * dist);

        // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
        // A hill between the camera and what it looks at: rise gently over it and look down past it,
        // instead of zooming in. Eased both ways, so it never jumps.
        float lift = LiftToSee(focus, pos);
        viewLift = Mathf.SmoothDamp(viewLift, lift, ref viewLiftVel, lift > viewLift ? 0.35f : 0.8f, Mathf.Infinity, Time.unscaledDeltaTime);
        pos.y += viewLift;
        // [AI-GENERATED END]

        // [AI-GENERATED] Claude Code (Claude Opus 5.5): the camera itself stays over the terrain, not just the point
        // it looks at, so zooming out never shows the empty edge of the world.
        if (HasTerrain)
        {
            Vector3 tMin = terrain.GetPosition(), tMax = tMin + terrain.terrainData.size;
            pos.x = Mathf.Clamp(pos.x, tMin.x + 2f, tMax.x - 2f);
            pos.z = Mathf.Clamp(pos.z, tMin.z + 2f, tMax.z - 2f);
        }

        // Don't go through mountains.
        float ground = GroundHeight(pos) + minHeightAboveGround;
        if (pos.y < ground) pos.y = ground;

        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(focus - pos, Vector3.up);
    }

    // [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
    float viewLift, viewLiftVel;

    // How much higher the camera must be so the line down to the focus clears every hill on the way.
    float LiftToSee(Vector3 from, Vector3 to)
    {
        if (terrain == null || terrain.terrainData == null) return 0f;
        const int Steps = 20;
        const float Clearance = 1.5f;
        float need = 0f;
        for (int i = 3; i <= Steps; i++)   // skip the bit right at the focus, which sits on the ground
        {
            float t = i / (float)Steps;
            var p = Vector3.Lerp(from, to, t);
            float gap = GroundHeight(p) + Clearance - p.y;
            if (gap > 0f) need = Mathf.Max(need, gap / t);   // raising the camera by h raises this point by t*h
        }
        return Mathf.Min(need, dist);   // never more than the zoom distance
    }
    // [AI-GENERATED END]

    Quaternion YawRotation() => Quaternion.Euler(0f, yaw, 0f);

    float GroundHeight(Vector3 p) =>
        HasTerrain ? terrain.SampleHeight(p) + terrain.GetPosition().y : p.y;

    // The terrain's data can already be gone while the scene closes (leaving the run, stopping Play)
    // and the camera still gets one last LateUpdate: then act as if there were no terrain.
    bool HasTerrain => terrain != null && terrain.terrainData != null;
}

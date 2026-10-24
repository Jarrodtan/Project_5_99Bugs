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

    [Header("Zoom (distance from the focus point)")]
    [SerializeField] float minDistance = 3f;
    [SerializeField] float maxDistance = 250f;
    [SerializeField] float startDistance = 110f;
    [Tooltip("Mouse wheel: fraction of the distance per notch.")]
    [SerializeField] float wheelZoomStep = 0.12f;

    [Header("Angles")]
    [SerializeField] float minPitch = 8f;
    [SerializeField] float maxPitch = 85f;
    [SerializeField] float startPitch = 30f;
    [SerializeField] float startYaw = 0f;
    [Tooltip("Degrees per pixel for rotate/tilt drags.")]
    [SerializeField] float rotateSpeed = 0.25f;

    [Header("Feel")]
    [Tooltip("Smaller = snappier, larger = smoother.")]
    [SerializeField] float smoothTime = 0.12f;
    [Tooltip("Keyboard pan speed, per unit of distance, per second.")]
    [SerializeField] float keyPanSpeed = 0.8f;

    [Header("Follow / POV")]
    [Tooltip("Distance used when following an animal starts.")]
    [SerializeField] float followDistance = 12f;
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

    Camera cam;
    Vector2 tapStart;
    bool tapCandidate;
    bool gestureOverUI;

    public Transform FollowTarget => followTarget;
    public bool IsPov => pov;

    void Awake() => cam = GetComponent<Camera>();

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Start() => ResetView();

    public void ResetView()
    {
        followTarget = null;
        pov = false;
        if (terrain == null) terrain = Terrain.activeTerrain;
        focusTarget = terrain != null ? terrain.GetPosition() + terrain.terrainData.size * 0.5f : Vector3.zero;
        focusTarget.y = GroundHeight(focusTarget);
        yawTarget = startYaw;
        pitchTarget = startPitch;
        distTarget = startDistance;
        focus = focusTarget; yaw = yawTarget; pitch = pitchTarget; dist = distTarget;
    }

    public void Follow(Transform target)
    {
        followTarget = target;
        if (target != null) distTarget = Mathf.Min(distTarget, followDistance);
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

    void LateUpdate()
    {
        // Terrain may be created after Start (it's built at runtime).
        if (terrain == null && Terrain.activeTerrain != null) { terrain = Terrain.activeTerrain; ResetView(); }
        if (followTarget != null && !followTarget.gameObject.activeInHierarchy) StopFollowing();

        if (Touchscreen.current != null && Touch.activeTouches.Count > 0) HandleTouch();
        else HandleMouse();
        HandleScroll();
        HandleKeyboard();

        if (followTarget != null) focusTarget = followTarget.position;
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
            var t = touches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began) { tapStart = t.screenPosition; tapCandidate = true; }
            if ((t.screenPosition - tapStart).sqrMagnitude > 20f * 20f) tapCandidate = false;

            if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved && !tapCandidate) Pan(t.delta);
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

        // Tilt: both fingers moving up or down together.
        if (Mathf.Sign(a.delta.y) == Mathf.Sign(b.delta.y))
            pitchTarget -= (a.delta.y + b.delta.y) * 0.5f * rotateSpeed;
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

        if (mouse.leftButton.wasPressedThisFrame) { tapStart = pos; tapCandidate = true; }
        if ((pos - tapStart).sqrMagnitude > 6f * 6f) tapCandidate = false;
        if (mouse.leftButton.isPressed && !tapCandidate) Pan(delta);
        if (mouse.leftButton.wasReleasedThisFrame && tapCandidate) Tap(pos);

        if (mouse.middleButton.isPressed) Pan(delta);
        if (mouse.rightButton.isPressed)
        {
            yawTarget += delta.x * rotateSpeed;
            pitchTarget -= delta.y * rotateSpeed;
        }
    }

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
        if (Physics.Raycast(ray, out var hit, 1000f, tapMask, QueryTriggerInteraction.Collide))
        {
            var entity = hit.collider.GetComponentInParent<EcoEntity>();
            if (entity != null) { Follow(entity.transform); return; }
        }
        StopFollowing();
    }

    // ---------- Camera maths ----------

    void ClampTargets()
    {
        distTarget = Mathf.Clamp(distTarget, minDistance, maxDistance);
        pitchTarget = Mathf.Clamp(pitchTarget, pov ? -30f : minPitch, maxPitch);

        if (terrain != null && followTarget == null)
        {
            var min = terrain.GetPosition();
            var max = min + terrain.terrainData.size;
            focusTarget.x = Mathf.Clamp(focusTarget.x, min.x, max.x);
            focusTarget.z = Mathf.Clamp(focusTarget.z, min.z, max.z);
            focusTarget.y = GroundHeight(focusTarget);
        }
    }

    void Ease()
    {
        float t = followTarget != null ? smoothTime * 0.5f : smoothTime;   // keep up with moving animals
        focus = Vector3.SmoothDamp(focus, focusTarget, ref focusVel, t, Mathf.Infinity, Time.unscaledDeltaTime);
        yaw = Mathf.SmoothDampAngle(yaw, yawTarget, ref yawVel, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        pitch = Mathf.SmoothDampAngle(pitch, pitchTarget, ref pitchVel, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        dist = Mathf.SmoothDamp(dist, distTarget, ref distVel, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
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

        // Don't go through mountains.
        float ground = GroundHeight(pos) + minHeightAboveGround;
        if (pos.y < ground) pos.y = ground;

        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(focus - pos, Vector3.up);
    }

    Quaternion YawRotation() => Quaternion.Euler(0f, yaw, 0f);

    float GroundHeight(Vector3 p) =>
        terrain != null ? terrain.SampleHeight(p) + terrain.GetPosition().y : p.y;
}

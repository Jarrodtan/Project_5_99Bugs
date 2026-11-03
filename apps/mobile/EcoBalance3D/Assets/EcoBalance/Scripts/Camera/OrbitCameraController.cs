using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Orbits this transform around an <see cref="IOrbitTarget"/> pivot. All speeds, clamps,
/// damping, and input bindings come from <see cref="CameraConfig"/> — no device hardcoding.
/// One finger / left-drag rotates. Two fingers grab the ground under the midpoint and
/// scale distance by finger span. Middle or right mouse drag pans the same way. Mouse
/// wheel multiplies distance toward the cursor. WASD / stick also pan. When a
/// <see cref="SelectionContext"/> selection is active, the pivot follows that entity
/// (pan skipped; enlarge still allowed).
/// </summary>
public class OrbitCameraController : MonoBehaviour
{
    [Tooltip("Camera hub with input refs, clamps, damping, and defaults.")]
    [SerializeField]
    CameraConfig config;

    [Tooltip("Pivot provider (e.g. IslandManager). Must implement IOrbitTarget.")]
    [SerializeField]
    MonoBehaviour orbitTargetBehaviour;

    [Tooltip("Optional. When assigned, selecting an entity locks the orbit pivot to it.")]
    [SerializeField]
    SelectionContext selectionContext;

    IOrbitTarget _orbitTarget;
    Camera _camera;
    InputAction _dragAction;
    InputAction _dragDeltaAction;
    InputAction _zoomAction;
    InputAction _panAction;

    float _yaw;
    float _pitch;
    float _distance;
    float _targetYaw;
    float _targetPitch;
    float _targetDistance;
    float _yawVelocity;
    float _pitchVelocity;
    float _distanceVelocity;

    Vector3 _pivot;
    Vector3 _pivotVelocity;
    Vector3 _panOffset;
    Vector3 _panOffsetVelocity;
    bool _dragBlocked;
    bool _recentering;
    float _recenterYawVel;
    float _recenterPitchVel;
    float _recenterDistVel;

    Transform _followTarget;

    bool _twoFingerLatched;
    bool _twoFingerActive;
    bool _twoFingerBlocked;
    bool _twoFingerHasSample;
    Vector2 _twoFingerPrevMid;
    float _twoFingerPrevSpan;

    bool _mousePanActive;
    bool _mousePanBlocked;
    Vector2 _mousePanPrevPos;

    bool IsFollowLocked => _followTarget != null;
    bool IsGesturePanActive => _twoFingerActive || _mousePanActive;

    void Awake()
    {
        _orbitTarget = orbitTargetBehaviour as IOrbitTarget;
        _camera = GetComponent<Camera>();
    }

    void OnEnable()
    {
        if (config == null)
            return;

        _dragAction = config.dragAction != null ? config.dragAction.action : null;
        _dragDeltaAction = config.dragDeltaAction != null ? config.dragDeltaAction.action : null;
        _zoomAction = config.zoomAction != null ? config.zoomAction.action : null;
        _panAction = config.panAction != null ? config.panAction.action : null;

        _dragAction?.Enable();
        _dragDeltaAction?.Enable();
        _zoomAction?.Enable();
        _panAction?.Enable();

        if (_orbitTarget != null)
            _orbitTarget.OrbitPivotChanged += OnOrbitPivotChanged;

        if (selectionContext != null)
        {
            selectionContext.Changed += OnSelectionChanged;
            ApplyFollowFromHandle(selectionContext.Current);
        }
    }

    void OnDisable()
    {
        if (_orbitTarget != null)
            _orbitTarget.OrbitPivotChanged -= OnOrbitPivotChanged;

        if (selectionContext != null)
            selectionContext.Changed -= OnSelectionChanged;

        _followTarget = null;
        ClearTwoFingerState();
        _mousePanActive = false;
        _mousePanBlocked = false;

        _dragAction?.Disable();
        _dragDeltaAction?.Disable();
        _zoomAction?.Disable();
        _panAction?.Disable();
    }

    void Start()
    {
        if (config == null)
        {
            Debug.LogError($"{nameof(OrbitCameraController)} requires a {nameof(CameraConfig)}.", this);
            enabled = false;
            return;
        }

        if (_orbitTarget == null)
        {
            Debug.LogError($"{nameof(OrbitCameraController)} requires an {nameof(IOrbitTarget)}.", this);
            enabled = false;
            return;
        }

        if (_camera == null)
            _camera = GetComponent<Camera>();

        _targetYaw = config.defaultYaw;
        _targetPitch = config.defaultPitch;
        _targetDistance = config.defaultDistance;
        _yaw = _targetYaw;
        _pitch = _targetPitch;
        _distance = _targetDistance;
        _panOffset = Vector3.zero;
        _pivot = ResolvePivotTarget();
        ApplyTransform();
    }

    void Update()
    {
        if (config == null || _orbitTarget == null)
            return;

        if (_followTarget == null && selectionContext != null && selectionContext.Current.IsValid)
            ApplyFollowFromHandle(selectionContext.Current);

        UpdateTwoFingerGesture();
        UpdateMousePan();
        UpdateDragInput();
        UpdateWheelZoom();
        UpdateStickPan();
        UpdateSmoothing();
        ApplyTransform();
    }

    void UpdateDragInput()
    {
        if (_dragAction == null)
            return;

        if (_twoFingerLatched || _mousePanActive || CountActiveTouches() >= 2)
            return;

        if (_dragAction.WasPressedThisFrame())
        {
            _dragBlocked = config.ignorePointerOverUI && PointerOverUI.IsOver();
            _recentering = false;
        }

        if (_dragAction.WasReleasedThisFrame())
            _dragBlocked = false;

        if (!_dragAction.IsPressed() || _dragBlocked || _dragDeltaAction == null)
            return;

        var delta = _dragDeltaAction.ReadValue<Vector2>();
        _targetYaw += delta.x * config.rotationSpeed;
        _targetPitch -= delta.y * config.rotationSpeed;
        _targetPitch = Mathf.Clamp(_targetPitch, config.minPitch, config.maxPitch);
    }

    void UpdateMousePan()
    {
        _mousePanActive = false;

        var mouse = Mouse.current;
        if (mouse == null || _twoFingerLatched || CountActiveTouches() >= 2)
            return;

        var pressed = mouse.middleButton.isPressed || mouse.rightButton.isPressed;
        var pressedThisFrame = mouse.middleButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
        var releasedThisFrame = (mouse.middleButton.wasReleasedThisFrame && !mouse.rightButton.isPressed)
            || (mouse.rightButton.wasReleasedThisFrame && !mouse.middleButton.isPressed);

        if (pressedThisFrame)
        {
            _mousePanBlocked = config.ignorePointerOverUI && PointerOverUI.IsOver();
            _mousePanPrevPos = mouse.position.ReadValue();
            _recentering = false;
        }

        if (releasedThisFrame)
            _mousePanBlocked = false;

        if (!pressed || _mousePanBlocked || IsFollowLocked || _recentering)
            return;

        _mousePanActive = true;
        var pos = mouse.position.ReadValue();
        ApplyGroundPan(_mousePanPrevPos, pos, snapPose: true);
        _mousePanPrevPos = pos;
    }

    void UpdateTwoFingerGesture()
    {
        _twoFingerActive = false;

        var touchCount = CountActiveTouches();
        if (touchCount == 0)
        {
            ClearTwoFingerState();
            return;
        }

        if (touchCount < 2)
        {
            _twoFingerHasSample = false;
            return;
        }

        if (!TryGetTwoFingerSample(out var mid, out var span, out var fingerIdA, out var fingerIdB))
            return;

        _twoFingerLatched = true;
        _twoFingerActive = true;
        _recentering = false;

        if (!_twoFingerHasSample)
        {
            _twoFingerBlocked = config.ignorePointerOverUI
                && (PointerOverUI.IsOver(fingerIdA) || PointerOverUI.IsOver(fingerIdB));
            _twoFingerPrevMid = mid;
            _twoFingerPrevSpan = Mathf.Max(span, 0.001f);
            _twoFingerHasSample = true;
            return;
        }

        if (_twoFingerBlocked)
        {
            _twoFingerPrevMid = mid;
            _twoFingerPrevSpan = Mathf.Max(span, 0.001f);
            return;
        }

        ApplyTransform();

        if (span > 0.001f && _twoFingerPrevSpan > 0.001f)
        {
            _targetDistance = Mathf.Clamp(
                _targetDistance * (_twoFingerPrevSpan / span),
                config.minDistance,
                config.maxDistance);
            _distance = _targetDistance;
            _distanceVelocity = 0f;
            ApplyTransform();
        }

        if (!IsFollowLocked)
            ApplyGroundPan(_twoFingerPrevMid, mid, snapPose: true);

        _twoFingerPrevMid = mid;
        _twoFingerPrevSpan = Mathf.Max(span, 0.001f);
    }

    void UpdateWheelZoom()
    {
        if (_zoomAction == null || IsGesturePanActive)
            return;

        var scroll = _zoomAction.ReadValue<Vector2>();
        var notches = NormalizeScrollNotches(scroll.y);
        if (Mathf.Abs(notches) <= 0f)
            return;

        _recentering = false;

        Vector2 screenPoint = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        ApplyTransform();
        var hasGround = TryRayGround(screenPoint, out var groundPrev);

        var factor = Mathf.Clamp(config.zoomNotchFactor, 0.5f, 0.99f);
        var scale = notches > 0f
            ? Mathf.Pow(factor, notches)
            : Mathf.Pow(1f / factor, -notches);

        _targetDistance = Mathf.Clamp(
            _targetDistance * scale,
            config.minDistance,
            config.maxDistance);

        if (!IsFollowLocked && hasGround)
        {
            var poseDistance = _distance;
            _distance = _targetDistance;
            ApplyTransform();

            if (TryRayGround(screenPoint, out var groundCurr))
            {
                _panOffset += groundPrev - groundCurr;
                ClampPanOffset();
            }

            _distance = poseDistance;
        }
    }

    void UpdateStickPan()
    {
        if (_recentering || IsFollowLocked || IsGesturePanActive || _panAction == null)
            return;

        var stick = _panAction.ReadValue<Vector2>();
        if (stick.sqrMagnitude <= 0.0001f)
            return;

        var scale = DistanceScale(_distance);
        var yawRotation = Quaternion.Euler(0f, _yaw, 0f);
        var right = yawRotation * Vector3.right;
        var forward = yawRotation * Vector3.forward;

        _panOffset += (right * stick.x + forward * stick.y) * config.panSpeed * scale * Time.deltaTime;
        ClampPanOffset();
    }

    void ClampPanOffset()
    {
        var maxRadius = Mathf.Max(0f, config.maxPanRadius);
        if (maxRadius > 0f && _panOffset.sqrMagnitude > maxRadius * maxRadius)
            _panOffset = _panOffset.normalized * maxRadius;
    }

    float DistanceScale(float currentDistance)
    {
        var reference = config.panSpeedReferenceDistance;
        if (reference <= 0f)
            return 1f;
        return currentDistance / reference;
    }

    static float NormalizeScrollNotches(float scrollY)
    {
        var abs = Mathf.Abs(scrollY);
        if (abs <= 0f)
            return 0f;

        if (abs >= 20f)
            return Mathf.Sign(scrollY) * Mathf.Max(1f, Mathf.Round(abs / 120f));

        return Mathf.Sign(scrollY) * Mathf.Max(1f, Mathf.Round(abs));
    }

    void ClearTwoFingerState()
    {
        _twoFingerLatched = false;
        _twoFingerActive = false;
        _twoFingerBlocked = false;
        _twoFingerHasSample = false;
        _twoFingerPrevMid = Vector2.zero;
        _twoFingerPrevSpan = 0f;
    }

    static int CountActiveTouches()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return 0;

        var count = 0;
        foreach (var touch in touchscreen.touches)
        {
            if (touch.press.isPressed)
                count++;
        }

        return count;
    }

    static bool TryGetTwoFingerSample(out Vector2 mid, out float span, out int fingerIdA, out int fingerIdB)
    {
        mid = Vector2.zero;
        span = 0f;
        fingerIdA = -1;
        fingerIdB = -1;
        var touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return false;

        TouchControl first = null;
        TouchControl second = null;
        foreach (var touch in touchscreen.touches)
        {
            if (!touch.press.isPressed)
                continue;

            if (first == null)
            {
                first = touch;
                continue;
            }

            second = touch;
            break;
        }

        if (first == null || second == null)
            return false;

        var a = first.position.ReadValue();
        var b = second.position.ReadValue();
        mid = (a + b) * 0.5f;
        span = Vector2.Distance(a, b);
        fingerIdA = first.touchId.ReadValue();
        fingerIdB = second.touchId.ReadValue();
        return true;
    }

    void ApplyGroundPan(Vector2 fromScreen, Vector2 toScreen, bool snapPose)
    {
        ApplyTransform();

        if (TryRayGround(fromScreen, out var groundPrev) && TryRayGround(toScreen, out var groundCurr))
        {
            _panOffset += groundPrev - groundCurr;
        }
        else
        {
            var delta = toScreen - fromScreen;
            if (delta.sqrMagnitude <= 0.0001f)
                return;

            var pixelsToWorld = ApproximatePixelsToWorld();
            var yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            var right = yawRotation * Vector3.right;
            var forward = yawRotation * Vector3.forward;
            _panOffset += (right * -delta.x + forward * -delta.y) * pixelsToWorld;
        }

        ClampPanOffset();

        if (!snapPose)
            return;

        _pivot = ResolvePivotTarget();
        _pivotVelocity = Vector3.zero;
        ApplyTransform();
    }

    float ApproximatePixelsToWorld()
    {
        var height = Mathf.Max(1f, Screen.height);
        if (_camera != null && !_camera.orthographic)
        {
            var halfFov = _camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            return 2f * _distance * Mathf.Tan(halfFov) / height;
        }

        return _distance / height;
    }

    bool TryRayGround(Vector2 screenPoint, out Vector3 ground)
    {
        ground = Vector3.zero;
        if (_camera == null)
            return false;

        var ray = _camera.ScreenPointToRay(screenPoint);
        var plane = new Plane(Vector3.up, new Vector3(0f, _pivot.y, 0f));
        if (!plane.Raycast(ray, out var enter) || enter < 0f)
            return false;

        ground = ray.GetPoint(enter);
        return true;
    }

    void UpdateSmoothing()
    {
        if (IsGesturePanActive)
        {
            _distance = _targetDistance;
            _distanceVelocity = 0f;
            _pivot = ResolvePivotTarget();
            _pivotVelocity = Vector3.zero;
            _yaw = SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, config.rotationDamping);
            _pitch = SmoothDampAngle(_pitch, _targetPitch, ref _pitchVelocity, config.rotationDamping);
            _pitch = Mathf.Clamp(_pitch, config.minPitch, config.maxPitch);
            return;
        }

        var pivotTarget = ResolvePivotTarget();
        var pivotSmooth = config.panDamping > 0f ? config.panDamping : config.pivotRecenterDamping;
        _pivot = SmoothDampVector(_pivot, pivotTarget, ref _pivotVelocity, pivotSmooth);

        if (_recentering)
        {
            var recenterSmooth = config.pivotRecenterDamping;
            _panOffset = SmoothDampVector(_panOffset, Vector3.zero, ref _panOffsetVelocity, recenterSmooth);
            _targetYaw = SmoothDampAngle(_targetYaw, config.defaultYaw, ref _recenterYawVel, recenterSmooth);
            _targetPitch = SmoothDampAngle(_targetPitch, config.defaultPitch, ref _recenterPitchVel, recenterSmooth);
            _targetDistance = SmoothDampFloat(_targetDistance, config.defaultDistance, ref _recenterDistVel, recenterSmooth);
            _targetPitch = Mathf.Clamp(_targetPitch, config.minPitch, config.maxPitch);
            _targetDistance = Mathf.Clamp(_targetDistance, config.minDistance, config.maxDistance);

            if (_panOffset.sqrMagnitude < 0.0001f &&
                ApproximatelyAngle(_targetYaw, config.defaultYaw) &&
                ApproximatelyAngle(_targetPitch, config.defaultPitch) &&
                Mathf.Abs(_targetDistance - config.defaultDistance) < 0.01f)
            {
                _panOffset = Vector3.zero;
                _targetYaw = config.defaultYaw;
                _targetPitch = config.defaultPitch;
                _targetDistance = config.defaultDistance;
                _recentering = false;
            }
        }

        _yaw = SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, config.rotationDamping);
        _pitch = SmoothDampAngle(_pitch, _targetPitch, ref _pitchVelocity, config.rotationDamping);
        _distance = SmoothDampFloat(_distance, _targetDistance, ref _distanceVelocity, config.zoomDamping);
        _pitch = Mathf.Clamp(_pitch, config.minPitch, config.maxPitch);
        _distance = Mathf.Clamp(_distance, config.minDistance, config.maxDistance);
    }

    Vector3 ResolvePivotTarget()
    {
        if (_followTarget != null)
            return _followTarget.position;

        return _orbitTarget.GetPivotPoint() + _panOffset;
    }

    void OnSelectionChanged(SelectionChange change)
    {
        ApplyFollowFromHandle(change.Current);
    }

    void ApplyFollowFromHandle(SelectionHandle handle)
    {
        if (!handle.IsValid)
        {
            _followTarget = null;
            return;
        }

        if (!SelectableRegistry.TryGet(handle, out var selectable))
        {
            _followTarget = null;
            return;
        }

        var behaviour = selectable as Component;
        if (behaviour == null)
        {
            _followTarget = null;
            return;
        }

        _followTarget = behaviour.transform;
        _panOffset = Vector3.zero;
        _panOffsetVelocity = Vector3.zero;
    }

    void OnOrbitPivotChanged()
    {
        _dragBlocked = true;
        _recentering = true;
        _recenterYawVel = 0f;
        _recenterPitchVel = 0f;
        _recenterDistVel = 0f;
        _pivotVelocity = Vector3.zero;
        _panOffsetVelocity = Vector3.zero;
        ClearTwoFingerState();
    }

    void ApplyTransform()
    {
        var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = _pivot + rotation * (Vector3.back * _distance);
        transform.rotation = rotation;
    }

    static float SmoothDampAngle(float current, float target, ref float velocity, float smoothTime)
    {
        if (smoothTime <= 0f)
        {
            velocity = 0f;
            return target;
        }

        return Mathf.SmoothDampAngle(current, target, ref velocity, smoothTime);
    }

    static float SmoothDampFloat(float current, float target, ref float velocity, float smoothTime)
    {
        if (smoothTime <= 0f)
        {
            velocity = 0f;
            return target;
        }

        return Mathf.SmoothDamp(current, target, ref velocity, smoothTime);
    }

    static Vector3 SmoothDampVector(Vector3 current, Vector3 target, ref Vector3 velocity, float smoothTime)
    {
        if (smoothTime <= 0f)
        {
            velocity = Vector3.zero;
            return target;
        }

        return Vector3.SmoothDamp(current, target, ref velocity, smoothTime);
    }

    static bool ApproximatelyAngle(float a, float b)
    {
        return Mathf.Abs(Mathf.DeltaAngle(a, b)) < 0.5f;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Orbits this transform around an <see cref="IOrbitTarget"/> pivot. All speeds, clamps,
/// damping, and input bindings come from <see cref="CameraConfig"/> — no device hardcoding.
/// </summary>
public class OrbitCameraController : MonoBehaviour
{
    [Tooltip("Camera hub with input refs, clamps, damping, and defaults.")]
    [SerializeField]
    CameraConfig config;

    [Tooltip("Pivot provider (e.g. IslandManager). Must implement IOrbitTarget.")]
    [SerializeField]
    MonoBehaviour orbitTargetBehaviour;

    IOrbitTarget _orbitTarget;
    InputAction _dragAction;
    InputAction _dragDeltaAction;
    InputAction _zoomAction;

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
    bool _dragBlocked;
    bool _recentering;
    float _recenterYawVel;
    float _recenterPitchVel;
    float _recenterDistVel;

    void Awake()
    {
        _orbitTarget = orbitTargetBehaviour as IOrbitTarget;
    }

    void OnEnable()
    {
        if (config == null)
            return;

        _dragAction = config.dragAction != null ? config.dragAction.action : null;
        _dragDeltaAction = config.dragDeltaAction != null ? config.dragDeltaAction.action : null;
        _zoomAction = config.zoomAction != null ? config.zoomAction.action : null;

        _dragAction?.Enable();
        _dragDeltaAction?.Enable();
        _zoomAction?.Enable();

        if (_orbitTarget != null)
            _orbitTarget.OrbitPivotChanged += OnOrbitPivotChanged;
    }

    void OnDisable()
    {
        if (_orbitTarget != null)
            _orbitTarget.OrbitPivotChanged -= OnOrbitPivotChanged;

        _dragAction?.Disable();
        _dragDeltaAction?.Disable();
        _zoomAction?.Disable();
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

        _targetYaw = config.defaultYaw;
        _targetPitch = config.defaultPitch;
        _targetDistance = config.defaultDistance;
        _yaw = _targetYaw;
        _pitch = _targetPitch;
        _distance = _targetDistance;
        _pivot = _orbitTarget.GetPivotPoint();
        ApplyTransform();
    }

    void Update()
    {
        if (config == null || _orbitTarget == null)
            return;

        UpdateDragInput();
        UpdateZoomInput();
        UpdateSmoothing();
        ApplyTransform();
    }

    void UpdateDragInput()
    {
        if (_dragAction == null)
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

    void UpdateZoomInput()
    {
        if (_zoomAction == null)
            return;

        var scroll = _zoomAction.ReadValue<Vector2>();
        if (Mathf.Abs(scroll.y) <= 0f)
            return;

        _targetDistance -= Mathf.Sign(scroll.y) * config.zoomSpeed;
        _targetDistance = Mathf.Clamp(_targetDistance, config.minDistance, config.maxDistance);
    }

    void UpdateSmoothing()
    {
        var pivotTarget = _orbitTarget.GetPivotPoint();
        var pivotSmooth = config.pivotRecenterDamping;
        _pivot = SmoothDampVector(_pivot, pivotTarget, ref _pivotVelocity, pivotSmooth);

        if (_recentering)
        {
            var recenterSmooth = config.pivotRecenterDamping;
            _targetYaw = SmoothDampAngle(_targetYaw, config.defaultYaw, ref _recenterYawVel, recenterSmooth);
            _targetPitch = SmoothDampAngle(_targetPitch, config.defaultPitch, ref _recenterPitchVel, recenterSmooth);
            _targetDistance = SmoothDampFloat(_targetDistance, config.defaultDistance, ref _recenterDistVel, recenterSmooth);
            _targetPitch = Mathf.Clamp(_targetPitch, config.minPitch, config.maxPitch);
            _targetDistance = Mathf.Clamp(_targetDistance, config.minDistance, config.maxDistance);

            if (ApproximatelyAngle(_targetYaw, config.defaultYaw) &&
                ApproximatelyAngle(_targetPitch, config.defaultPitch) &&
                Mathf.Abs(_targetDistance - config.defaultDistance) < 0.01f)
            {
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

    void OnOrbitPivotChanged()
    {
        _dragBlocked = true;
        _recentering = true;
        _recenterYawVel = 0f;
        _recenterPitchVel = 0f;
        _recenterDistVel = 0f;
        _pivotVelocity = Vector3.zero;
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

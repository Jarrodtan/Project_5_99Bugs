using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sole owner of the current island instance. Spawns at the config world anchor with
/// centerOffset pivot correction, and runs the swap sequence through pluggable collaborators.
/// Also provides <see cref="IOrbitTarget"/> for the orbit camera.
/// </summary>
public class IslandManager : MonoBehaviour, IOrbitTarget
{
    [Tooltip("Central island platform hub.")]
    [SerializeField]
    IslandPlatformConfig config;

    [Tooltip("Optional fade transition. When empty, swaps skip visual fade.")]
    [SerializeField]
    MonoBehaviour transitionBehaviour;

    [Tooltip("Optional ecosystem reset invoked during swap. When empty, reset is skipped.")]
    [SerializeField]
    MonoBehaviour ecosystemResetBehaviour;

    [Tooltip("Swap listeners (walls, containment, etc.). Authorable order.")]
    [SerializeField]
    List<MonoBehaviour> swapListenerBehaviours = new();

    IIslandTransition _transition;
    IEcosystemReset _ecosystemReset;
    readonly List<IIslandSwapListener> _listeners = new();

    IslandDefinition _activeDefinition;
    Transform _islandRoot;
    Coroutine _swapRoutine;
    readonly HashSet<Transform> _placedIslands = new();

    /// <summary>Currently active island definition, or null before first spawn.</summary>
    public IslandDefinition ActiveDefinition => _activeDefinition;

    /// <summary>Root transform at the world anchor (orbit pivot), or null.</summary>
    public Transform IslandRoot => _islandRoot;

    /// <summary>Platform hub assigned on this manager.</summary>
    public IslandPlatformConfig Config => config;

    /// <summary>True while a swap coroutine is running.</summary>
    public bool IsBusy => _swapRoutine != null;

    /// <inheritdoc />
    public event Action OrbitPivotChanged;

    void Awake()
    {
        CacheCollaborators();
    }

    void Start()
    {
        if (config == null)
        {
            Debug.LogError($"{nameof(IslandManager)} requires an {nameof(IslandPlatformConfig)}.", this);
            return;
        }

        if (config.defaultIsland == null)
        {
            Debug.LogError($"{nameof(IslandManager)}: defaultIsland is unassigned on the config.", config);
            return;
        }

        RememberPlacedIslands();
        SpawnIsland(config.defaultIsland, notifyListeners: true);
        HideOtherPlacedIslands();
    }

    /// <inheritdoc />
    public Vector3 GetPivotPoint()
    {
        if (_islandRoot != null)
            return _islandRoot.position;

        if (config != null)
            return config.worldAnchor;

        return Vector3.zero;
    }

    /// <summary>
    /// Requests a swap to <paramref name="definition"/>. Ignored when busy, null, or already active.
    /// </summary>
    public bool RequestSwap(IslandDefinition definition)
    {
        if (IsBusy || definition == null)
            return false;

        if (_activeDefinition == definition)
            return false;

        if (definition.islandPrefab == null)
        {
            Debug.LogError($"{nameof(IslandManager)}: target island has no islandPrefab.", definition);
            return false;
        }

        _swapRoutine = StartCoroutine(SwapRoutine(definition));
        return true;
    }

    IEnumerator SwapRoutine(IslandDefinition next)
    {
        try
        {
            if (_transition != null && _islandRoot != null && config != null)
                yield return _transition.Transition(_islandRoot, false, config);

            DespawnCurrent();

            _ecosystemReset?.ResetEcosystem();

            SpawnIsland(next, notifyListeners: true);

            if (_transition != null && _islandRoot != null && config != null)
                yield return _transition.Transition(_islandRoot, true, config);
        }
        finally
        {
            _swapRoutine = null;
        }
    }

    /// <summary>
    /// Requests a swap by catalog id. Resolves via <see cref="IslandPlatformConfig.TryGetIsland"/>.
    /// </summary>
    public bool RequestSwap(string id)
    {
        if (config == null || !config.TryGetIsland(id, out var definition) || definition == null)
            return false;

        return RequestSwap(definition);
    }

    void CacheCollaborators()
    {
        _transition = transitionBehaviour as IIslandTransition;
        _ecosystemReset = ecosystemResetBehaviour as IEcosystemReset;
        _listeners.Clear();

        if (swapListenerBehaviours == null)
            return;

        for (var i = 0; i < swapListenerBehaviours.Count; i++)
        {
            if (swapListenerBehaviours[i] is IIslandSwapListener listener)
                _listeners.Add(listener);
        }
    }

    void RememberPlacedIslands()
    {
        _placedIslands.Clear();
        if (config.islands == null)
            return;

        for (var i = 0; i < config.islands.Count; i++)
        {
            var placed = FindPlacedIsland(config.islands[i]);
            if (placed != null)
                _placedIslands.Add(placed);
        }
    }

    void HideOtherPlacedIslands()
    {
        foreach (var placed in _placedIslands)
        {
            if (placed == null || placed == _islandRoot)
                continue;

            placed.gameObject.SetActive(false);
        }
    }

    Transform FindPlacedIsland(IslandDefinition definition)
    {
        if (definition == null || definition.islandPrefab == null)
            return null;

        var prefabName = definition.islandPrefab.name;
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name == prefabName)
                return child;
        }

        return null;
    }

    void SpawnIsland(IslandDefinition definition, bool notifyListeners)
    {
        if (definition == null || config == null)
            return;

        if (definition.islandPrefab == null)
        {
            Debug.LogError($"{nameof(IslandManager)}: island has no islandPrefab.", definition);
            return;
        }

        var placed = FindPlacedIsland(definition);
        if (placed != null)
        {
            _placedIslands.Add(placed);
            _activeDefinition = definition;
            _islandRoot = placed;
            placed.gameObject.SetActive(true);
            placed.position = config.worldAnchor;
            placed.rotation = Quaternion.identity;
            placed.localScale = Vector3.one;

            if (notifyListeners)
            {
                for (var i = 0; i < _listeners.Count; i++)
                    _listeners[i].OnIslandSpawned(definition, _islandRoot, config);
            }

            OrbitPivotChanged?.Invoke();
            return;
        }

        _activeDefinition = definition;

        var rootName = string.IsNullOrEmpty(definition.displayName)
            ? definition.id
            : definition.displayName;
        var root = new GameObject(rootName);
        root.transform.SetParent(transform, false);
        root.transform.position = config.worldAnchor;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        _islandRoot = root.transform;

        var visual = Instantiate(definition.islandPrefab, _islandRoot);
        visual.name = definition.islandPrefab.name;
        visual.transform.localPosition = -definition.centerOffset;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        if (notifyListeners)
        {
            for (var i = 0; i < _listeners.Count; i++)
                _listeners[i].OnIslandSpawned(definition, _islandRoot, config);
        }

        OrbitPivotChanged?.Invoke();
    }

    void DespawnCurrent()
    {
        for (var i = 0; i < _listeners.Count; i++)
            _listeners[i].OnIslandDespawning(_activeDefinition, _islandRoot);

        if (_islandRoot != null)
        {
            if (_placedIslands.Contains(_islandRoot))
                _islandRoot.gameObject.SetActive(false);
            else if (Application.isPlaying)
                Destroy(_islandRoot.gameObject);
            else
                DestroyImmediate(_islandRoot.gameObject);
        }

        _islandRoot = null;
        _activeDefinition = null;
    }

    void OnDrawGizmos()
    {
        if (config == null || !config.drawAnchorGizmos)
            return;

        if (_islandRoot == null || _activeDefinition == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_islandRoot.position, config.gizmoPivotRadius);

        Gizmos.color = Color.cyan;
        var visualCenter = _islandRoot.TransformPoint(_activeDefinition.centerOffset);
        Gizmos.DrawWireSphere(visualCenter, config.gizmoCenterRadius);
    }
}

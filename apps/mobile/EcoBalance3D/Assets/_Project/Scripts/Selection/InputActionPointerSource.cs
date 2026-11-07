using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Default pointer source that resolves press and position from
/// <see cref="SelectionConfig"/> action paths. Uses <see cref="SelectionConfig.actionsAsset"/>
/// when assigned; otherwise falls back to project-wide <see cref="InputSystem.actions"/>.
/// Presses are captured via the action <c>started</c> callback (once per gesture) and
/// debounced using <see cref="SelectionConfig.pressDebounceSeconds"/> so a single click
/// cannot select then immediately toggle-clear.
/// </summary>
public sealed class InputActionPointerSource : ISelectionPointerSource
{
    SelectionConfig config;
    InputAction selectAction;
    InputAction pointerPositionAction;
    bool warnedMissing;
    bool pendingPress;
    Vector2 pendingScreenPosition;
    float nextAllowedPressTime;

    /// <inheritdoc />
    public void Enable(SelectionConfig selectionConfig)
    {
        Disable();

        config = selectionConfig;
        warnedMissing = false;
        pendingPress = false;
        nextAllowedPressTime = 0f;

        if (config == null)
        {
            LogMissing("SelectionConfig is null");
            return;
        }

        var actionsAsset = ResolveActionsAsset(config);
        if (actionsAsset == null)
        {
            LogMissing(
                "no InputActionAsset: assign SelectionConfig.actionsAsset, or set Project Settings → Input System Package → Input Actions");
            return;
        }

        if (string.IsNullOrWhiteSpace(config.selectActionPath) ||
            string.IsNullOrWhiteSpace(config.pointerPositionActionPath))
        {
            LogMissing("selectActionPath / pointerPositionActionPath");
            return;
        }

        selectAction = actionsAsset.FindAction(config.selectActionPath, throwIfNotFound: false);
        pointerPositionAction = actionsAsset.FindAction(config.pointerPositionActionPath, throwIfNotFound: false);

        if (selectAction == null || pointerPositionAction == null)
        {
            LogMissing(
                $"FindAction failed for '{config.selectActionPath}' and/or '{config.pointerPositionActionPath}' on asset '{actionsAsset.name}'");
            selectAction = null;
            pointerPositionAction = null;
            return;
        }

        // Ensure enabled, but do not Disable() shared project-wide actions on teardown —
        // that can leave Select permanently inert for the rest of Play mode.
        if (!selectAction.enabled)
            selectAction.Enable();
        if (!pointerPositionAction.enabled)
            pointerPositionAction.Enable();

        selectAction.started += OnSelectStarted;
    }

    /// <inheritdoc />
    public void Disable()
    {
        if (selectAction != null)
            selectAction.started -= OnSelectStarted;

        selectAction = null;
        pointerPositionAction = null;
        config = null;
        pendingPress = false;
        warnedMissing = false;
    }

    /// <inheritdoc />
    public bool TryGetPress(out Vector2 screenPosition)
    {
        screenPosition = default;

        if (!pendingPress)
            return false;

        pendingPress = false;
        screenPosition = pendingScreenPosition;
        return true;
    }

    void OnSelectStarted(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        var now = Time.unscaledTime;
        var debounce = config != null ? Mathf.Max(0f, config.pressDebounceSeconds) : 0f;
        if (now < nextAllowedPressTime)
            return;

        nextAllowedPressTime = now + debounce;
        pendingPress = true;
        pendingScreenPosition = pointerPositionAction != null
            ? pointerPositionAction.ReadValue<Vector2>()
            : Vector2.zero;
    }

    static InputActionAsset ResolveActionsAsset(SelectionConfig selectionConfig)
    {
        if (selectionConfig.actionsAsset != null)
            return selectionConfig.actionsAsset;

        return InputSystem.actions;
    }

    void LogMissing(string detail)
    {
        if (warnedMissing)
            return;

        Debug.LogError(
            $"{nameof(InputActionPointerSource)}: SelectionConfig input is incomplete ({detail}). Selection presses are ignored.");
        warnedMissing = true;
    }
}

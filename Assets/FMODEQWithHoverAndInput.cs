using UnityEngine;
using FMODUnity;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class FMODParameterChangeByLeftHandX : MonoBehaviour
{
    [Header("FMOD")]
    public StudioEventEmitter emitter;
    public string parameterName = "EQ";

    [Header("Input")]
    public InputActionAsset inputActionsAsset;
    public string actionMapName = "RightHand";
    public string actionName = "BButton";

    [Header("Hand Tracking")]
    public Transform leftHandTransform;

    [Header("Empfindlichkeit")]
    public float minDelta = -0.05f;
    public float maxDelta = 0.05f;

    private InputAction bButtonAction;
    private bool isHovered = false;
    private float startX;

    // XR Hover Event
    public void OnHoverEnter(HoverEnterEventArgs args)
    {
        isHovered = true;
        if (leftHandTransform != null)
        {
            startX = leftHandTransform.position.x;
            Debug.Log($"[HoverEnter] StartX gesetzt: {startX}");
        }
    }

    public void OnHoverExit(HoverExitEventArgs args)
    {
        isHovered = false;
        Debug.Log("[HoverExit] Hover beendet");
    }

    private void OnEnable()
    {
        if (inputActionsAsset == null)
        {
            Debug.LogError("Kein InputActionAsset zugewiesen!");
            return;
        }

        var map = inputActionsAsset.FindActionMap(actionMapName);
        if (map == null)
        {
            Debug.LogError($"Action Map '{actionMapName}' nicht gefunden!");
            return;
        }

        bButtonAction = map.FindAction(actionName);
        if (bButtonAction == null)
        {
            Debug.LogError($"Action '{actionName}' in Map '{actionMapName}' nicht gefunden!");
            return;
        }

        bButtonAction.Enable();
    }

    private void OnDisable()
    {
        if (bButtonAction != null)
            bButtonAction.Disable();
    }

    private void Update()
    {
        if (!isHovered || bButtonAction == null || leftHandTransform == null || emitter == null || !emitter.IsPlaying())
            return;

        if (bButtonAction.ReadValue<float>() > 0.1f)
        {
            float currentX = leftHandTransform.position.x;
            float delta = currentX - startX;

            float value = Mathf.InverseLerp(minDelta, maxDelta, delta);
            value = Mathf.Clamp01(value);

            emitter.SetParameter(parameterName, value);
            Debug.Log($"[B gedrückt] LeftHand X: {currentX}, Delta: {delta}, FMOD-Wert: {value}");
        }
    }
}

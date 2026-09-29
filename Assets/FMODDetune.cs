using UnityEngine;
using FMODUnity;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class FMODDetuneWithAsset : MonoBehaviour
{
    public StudioEventEmitter emitter;
    public string parameterName = "detune";

    public InputActionAsset inputActionsAsset;  // Dein gesamtes Input Action Asset

    private InputAction rightTriggerAction;
    private InputAction aButtonAction;

    public Transform leftHandTransform;

    public float minY = 0.5f;
    public float maxY = 1.5f;

    private bool isHovered = false;

    private void OnEnable()
    {
        // Actions aus dem Asset laden (ActionMapName = "XRI", ActionName anpassen!)
        rightTriggerAction = inputActionsAsset.FindAction("RightHand/TriggerButton");
        aButtonAction = inputActionsAsset.FindAction("RightHand/Button");

        rightTriggerAction?.Enable();
        aButtonAction?.Enable();
    }

    private void OnDisable()
    {
        rightTriggerAction?.Disable();
        aButtonAction?.Disable();
    }

    public void OnHoverEnter(HoverEnterEventArgs args)
    {
        isHovered = true;
        Debug.Log("Hover Enter");
    }

    public void OnHoverExit(HoverExitEventArgs args)
    {
        isHovered = false;
        Debug.Log("Hover Exit");
    }

    private void Update()
    {
        if (!isHovered) return;

        if (rightTriggerAction != null && aButtonAction != null)
        {
            bool triggerPressed = rightTriggerAction.IsPressed();
            bool aPressed = aButtonAction.IsPressed();

            if (triggerPressed && aPressed)
            {
                if (emitter != null && leftHandTransform != null && emitter.IsPlaying())
                {
                    float y = leftHandTransform.position.y;
                    float value = Mathf.InverseLerp(minY, maxY, y);
                    value = Mathf.Clamp01(value);
                    emitter.SetParameter(parameterName, value);
                    Debug.Log($"Detune Parameter gesetzt auf: {value}");
                }
            }
        }
    }
}

using UnityEngine;
using FMODUnity;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class FMODParameterOnHoverWithInputAsset : MonoBehaviour
{
    [Header("FMOD")]
    public StudioEventEmitter emitter;
    public string parameterName = "detune";

    [Header("Input")]
    public InputActionAsset inputActionsAsset;
    public string actionMapName = "RightHand"; // Name deiner Action Map (z.B. "XRI RightHand")
    public string actionName = "AButton";        // Name der Action im Asset (z.B. "A Button")

    [Header("Hand Tracking")]
    public Transform leftHandTransform;

    [Header("Parameter Range")]
    public float minY = 0.0f;
    public float maxY = 0.5f;

    private InputAction aButtonAction;
    private bool isHovered = false;

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

    private void OnEnable()
    {
        if (inputActionsAsset == null)
        {
            Debug.LogError("InputActionAsset nicht gesetzt!");
            return;
        }

        var map = inputActionsAsset.FindActionMap(actionMapName);
        if (map == null)
        {
            Debug.LogError($"Action Map '{actionMapName}' nicht gefunden!");
            return;
        }

        aButtonAction = map.FindAction(actionName);
        if (aButtonAction == null)
        {
            Debug.LogError($"Action '{actionName}' in Map '{actionMapName}' nicht gefunden!");
            return;
        }

        aButtonAction.Enable();
    }

    private void OnDisable()
    {
        if (aButtonAction != null)
            aButtonAction.Disable();
    }

    private void Update()
    {
        if (!isHovered) return;

        if (aButtonAction != null)
        {
            if (aButtonAction.ReadValue<float>() > 0.1f)  // für Button Press, funktioniert auch mit float
            {
                if (emitter != null && leftHandTransform != null && emitter.IsPlaying())
                {
                    float y = leftHandTransform.position.y;
                    float value = Mathf.InverseLerp(minY, maxY, y);
                    value = Mathf.Clamp01(value);
                    emitter.SetParameter(parameterName, value);

                    Debug.Log($"A Button gedrückt, linke Hand Y: {y}, Parameter Wert: {value}");
                }
            }
            else
            {
                Debug.Log("A Button nicht gedrückt");
            }
        }
    }
}

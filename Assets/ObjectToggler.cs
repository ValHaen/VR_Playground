using UnityEngine;
using UnityEngine.InputSystem; // Wichtig für das neue Input System

public class ObjectToggler : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Hier die Input Action für die B-Taste reinziehen")]
    public InputActionProperty toggleAction;

    [Header("Target")]
    [Tooltip("Das Objekt, das ein/ausgeschaltet werden soll")]
    public GameObject targetObject;

    private void OnEnable()
    {
        // Die Action aktivieren
        toggleAction.action.Enable();
        // Die Methode an das 'started' oder 'performed' Event hängen
        toggleAction.action.performed += OnButtonBPressed;
    }

    private void OnDisable()
    {
        toggleAction.action.performed -= OnButtonBPressed;
        toggleAction.action.Disable();
    }

    private void OnButtonBPressed(InputAction.CallbackContext context)
    {
        if (targetObject != null)
        {
            // Den aktuellen Zustand umkehren
            bool isActive = targetObject.activeSelf;
            targetObject.SetActive(!isActive);
            
            Debug.Log($"Objekt {targetObject.name} ist jetzt {( !isActive ? "AN" : "AUS")}");
        }
    }
}
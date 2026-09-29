using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System; // Für die Events

[RequireComponent(typeof(Renderer))]
public class SoundSphere : MonoBehaviour
{
    [Header("FMOD Settings")]
    public FMODUnity.StudioEventEmitter eventEmitter;
    public bool isGlobalReverb = false;
    public string parameterX = "Pitch";
    public string parameterY = "Reverb";
    public string parameterZ = "Filter";

    [Header("FMOD Value Mapping (0 bis 1)")]
    public float fmodTargetMin = 0f;
    public float fmodTargetMax = 1f;

    [Header("Kleines LOKALES Bewegungs-Umfeld (In Metern)")]
    public Vector2 rangeX = new Vector2(-0.2f, 0.2f);
    public Vector2 rangeY = new Vector2(-0.2f, 0.2f);
    public Vector2 rangeZ = new Vector2(-0.2f, 0.2f);

    [Header("Activation & Color Settings")]
    public InputActionReference toggleAction;
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;
    public string shaderColorPropertyName = "_BaseColor";

    [HideInInspector] public Vector3 localStartPos;
    [HideInInspector] public bool isActive = false;

    // --- NEU: Signale für das Network-Skript ---
    public Action<bool> OnToggleRequested; 
    public Action OnGrabRequested;
    // -------------------------------------------

    Transform currentHand;
    bool isDragging = false;
    Vector3 lastHandLocalPos;
    Renderer meshRenderer;

    void Start()
    {
        localStartPos = transform.localPosition;
        meshRenderer = GetComponent<Renderer>();

        if (toggleAction != null)
        {
            toggleAction.action.Enable();
            toggleAction.action.performed += ToggleSphereState;
        }

        isActive = false;
        ApplyColor(inactiveColor);

        if (eventEmitter != null)
            eventEmitter.Stop();
    }

    private void OnDestroy()
    {
        if (toggleAction != null)
        {
            toggleAction.action.performed -= ToggleSphereState;
            toggleAction.action.Disable();
        }
    }

    private void ToggleSphereState(InputAction.CallbackContext context)
    {
        if (!isDragging) return;

        // Wir schalten nicht mehr sofort um, sondern bitten das Netzwerk darum!
        // Das NetworkSoundSphere-Skript wird darauf hören.
        OnToggleRequested?.Invoke(!isActive);
    }

    public void ActivateFromNetwork()
    {
        if (isActive) return;
        isActive = true;
        ApplyColor(activeColor);
        if (eventEmitter != null) eventEmitter.Play();
        Debug.Log($"{gameObject.name} wurde via Netzwerk AKTIVIERT.");
    }

    public void DeactivateFromNetwork()
    {
        if (!isActive) return;
        isActive = false;
        ApplyColor(inactiveColor);
        if (eventEmitter != null) eventEmitter.Stop();
        Debug.Log($"{gameObject.name} wurde via Netzwerk DEAKTIVIERT.");
    }

    private void ApplyColor(Color targetColor)
    {
        if (meshRenderer == null) return;

        if (meshRenderer.material.HasProperty(shaderColorPropertyName))
            meshRenderer.material.SetColor(shaderColorPropertyName, targetColor);
        else
        {
            if (meshRenderer.material.HasProperty("_BaseColor"))
                meshRenderer.material.SetColor("_BaseColor", targetColor);
            else if (meshRenderer.material.HasProperty("_Color"))
                meshRenderer.material.SetColor("_Color", targetColor);
        }
    }

    void Update()
    {
        // 1. BEWEGUNG: Nur berechnen, wenn ICH es greife
        if (isDragging && currentHand != null)
        {
            Vector3 currentHandLocalPos = transform.parent != null
                ? transform.parent.InverseTransformPoint(currentHand.position)
                : currentHand.position;

            Vector3 deltaLocal = currentHandLocalPos - lastHandLocalPos;
            Vector3 targetLocalPos = transform.localPosition + deltaLocal;

            float clampedX = Mathf.Clamp(targetLocalPos.x, localStartPos.x + rangeX.x, localStartPos.x + rangeX.y);
            float clampedY = Mathf.Clamp(targetLocalPos.y, localStartPos.y + rangeY.x, localStartPos.y + rangeY.y);
            float clampedZ = Mathf.Clamp(targetLocalPos.z, localStartPos.z + rangeZ.x, localStartPos.z + rangeZ.y);

            transform.localPosition = new Vector3(clampedX, clampedY, clampedZ);
            lastHandLocalPos = currentHandLocalPos;
        }

        // 2. FMOD UPDATE: Für ALLE Spieler berechnen (auch wenn sie nur zuschauen), solange es aktiv ist!
        if (isActive)
        {
            UpdateFMODParameters();
        }
    }

    private void UpdateFMODParameters()
    {
        if (eventEmitter == null) return;

        float valX = CalculateMappedValue(transform.localPosition.x, localStartPos.x, rangeX);
        float valY = CalculateMappedValue(transform.localPosition.y, localStartPos.y, rangeY);
        float valZ = CalculateMappedValue(transform.localPosition.z, localStartPos.z, rangeZ);

        eventEmitter.SetParameter(parameterX, valX);
        eventEmitter.SetParameter(parameterZ, valZ);

        if (isGlobalReverb)
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName(parameterY, valY);
        else
            eventEmitter.SetParameter(parameterY, valY);
    }

    private float CalculateMappedValue(float currentLocalPos, float startLocalPos, Vector2 range)
    {
        float localOffset = currentLocalPos - startLocalPos;
        float normalizedZeroToOne = Mathf.InverseLerp(range.x, range.y, localOffset);
        return Mathf.Lerp(fmodTargetMin, fmodTargetMax, normalizedZeroToOne);
    }

    public void StartDragging(SelectEnterEventArgs args)
    {
        currentHand = args.interactorObject.transform;

        lastHandLocalPos = transform.parent != null
            ? transform.parent.InverseTransformPoint(currentHand.position)
            : currentHand.position;

        isDragging = true;

        // Signal ans Netzwerk: "Ich habe die Kugel gegriffen, gib mir die Ownership!"
        OnGrabRequested?.Invoke();
    }

    public void StopDragging(SelectExitEventArgs args)
    {
        isDragging = false;
        currentHand = null;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ... (OnDrawGizmosSelected bleibt wie gehabt)
}
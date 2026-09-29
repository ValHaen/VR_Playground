using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // WICHTIG für XR-Referenzen

public class Wurf_Script : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public GameObject nameText;

    [Header("Throw Settings")]
    public float throwMultiplier = 1.5f;
    public int velocitySampleFrames = 5;
    public float yOffset = 180f;

    // Diese Variable wird jetzt automatisch gesetzt!
    private Transform currentHandTransform; 
    
    private Vector3[] positionBuffer;
    private int bufferIndex, sampleCount;
    private bool isGrabbed = false;

    private void Awake() 
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        positionBuffer = new Vector3[Mathf.Max(2, velocitySampleFrames)];
    }

    private void Update() 
    {
        // Nutzt die Hand, die den Ball aktuell hält
        if (isGrabbed && currentHandTransform != null)
        {
            transform.position = currentHandTransform.position;
            positionBuffer[bufferIndex] = currentHandTransform.position;
            bufferIndex = (bufferIndex + 1) % positionBuffer.Length;
            if (sampleCount < positionBuffer.Length) sampleCount++;
        }

        if (nameText && nameText.activeSelf && Camera.main) 
        {
            nameText.transform.rotation = Camera.main.transform.rotation;
            nameText.transform.Rotate(0, yOffset, 0);
        }
    }

    // WICHTIG: Diese Methode braucht jetzt ein Argument vom XR Toolkit
    public void BeginGrab(SelectEnterEventArgs args) 
    {
        // Hier passiert die Magie: Wir holen uns den Transform der Hand, die gerade greift
        currentHandTransform = args.interactorObject.transform;
        
        isGrabbed = true;
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        sampleCount = 0;
        if (nameText) nameText.SetActive(true);
        if (BallManager.Instance) BallManager.Instance.RegisterAsLastTouched(this.gameObject);
    }

    public void EndGrab(SelectExitEventArgs args) 
    {
        isGrabbed = false;
        currentHandTransform = null; // Hand-Referenz löschen

        if (rb != null)
        {
            rb.isKinematic = false;
            if (sampleCount >= 2) 
            {
                int last = (bufferIndex + positionBuffer.Length - 1) % positionBuffer.Length;
                int first = (bufferIndex + positionBuffer.Length - sampleCount) % positionBuffer.Length;
                float timeSpan = sampleCount * Time.deltaTime;
                if (timeSpan > 0)
                {
                    Vector3 measuredVelocity = (positionBuffer[last] - positionBuffer[first]) / timeSpan;
                    rb.linearVelocity = measuredVelocity * throwMultiplier;
                }
            }
        }
        if (nameText) nameText.SetActive(false);
    }

    public void ResetState()
    {
        isGrabbed = false;
        currentHandTransform = null;
        if (rb != null)
        {
            rb.isKinematic = true; 
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false; 
        }
        if (nameText) nameText.SetActive(false);
    }
}
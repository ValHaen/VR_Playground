using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // Wichtig für die Hand-Events

public class RhythmBallY : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public GameObject nameText;

    [Header("Bounce Control")]
    public LayerMask groundLayer;
    
    private Transform currentHandTransform; // Speichert die aktuelle Hand
    private bool isGrabbed = false;
    private bool hasBeenReleased = false;
    private float releaseHeight;

    private void OnEnable()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        ResetState(); 
    }

    void Update()
    {
        // Nur bewegen, wenn gegriffen UND eine Hand zugewiesen ist
        if (isGrabbed && currentHandTransform != null) {
            transform.position = currentHandTransform.position;
            
            // Velocity nur nullen, wenn NICHT kinematic (verhindert Fehlermeldungen)
            if (!rb.isKinematic) {
                rb.linearVelocity = Vector3.zero;
            }
        }

        // Billboard-Logik für den Text
        if (nameText && nameText.activeSelf && Camera.main) {
            nameText.transform.rotation = Camera.main.transform.rotation;
            nameText.transform.Rotate(0, 180f, 0); // yOffset manuell auf 180 oder Variable nutzen
        }
    }

    void FixedUpdate()
    {
        // Geschwindigkeit nur manipulieren, wenn der Ball frei fällt
        if (hasBeenReleased && !isGrabbed && rb != null && !rb.isKinematic) {
            Vector3 v = rb.linearVelocity;
            v.x = 0f; v.z = 0f; // Erzwingt rein vertikale Bewegung
            rb.linearVelocity = v;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!this.enabled || isGrabbed || !hasBeenReleased || rb.isKinematic) return;
        if (((1 << collision.gameObject.layer) & groundLayer) == 0) return;

        float gravity = Mathf.Abs(Physics.gravity.y);
        float currentY = transform.position.y;
        float heightToReach = releaseHeight - currentY;

        if (heightToReach > 0.05f) {
            float v = Mathf.Sqrt(2 * gravity * heightToReach);
            rb.linearVelocity = new Vector3(0, v, 0);
        }
    }

    // WICHTIG: Aufruf durch XR Grab Interactable (Select Entered)
    public void BeginGrab(SelectEnterEventArgs args) 
    { 
        currentHandTransform = args.interactorObject.transform; // Hand merken
        isGrabbed = true; 
        hasBeenReleased = false;
        rb.isKinematic = true; 
        
        if (nameText) nameText.SetActive(true);
        if (BallManager.Instance) BallManager.Instance.RegisterAsLastTouched(this.gameObject);
    }

    // WICHTIG: Aufruf durch XR Grab Interactable (Select Exited)
    public void EndGrab(SelectExitEventArgs args) 
    {
        isGrabbed = false;
        currentHandTransform = null; // Hand vergessen
        hasBeenReleased = true;
        rb.isKinematic = false;
        releaseHeight = transform.position.y; 
        
        if (nameText) nameText.SetActive(false);
    }

    public void ResetState()
    {
        isGrabbed = false;
        hasBeenReleased = false;
        currentHandTransform = null;

        if (rb != null)
        {
            rb.isKinematic = true; // Erstmal stoppen für BallManager
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = true; // Damit er nach dem Unfreeze fällt
        }
        if (nameText) nameText.SetActive(false);
    }
}
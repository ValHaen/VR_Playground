using UnityEngine;

public class IntroBlockScript : MonoBehaviour
{
    public Rigidbody rb;

    private void Awake() 
    {
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision) 
    {
        // Einfaches Abprall-Verhalten ohne FMOD-Trigger
        if (rb != null && rb.linearVelocity.magnitude > 0.1f) 
        {
            rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, collision.contacts[0].normal) * 0.8f; // Leicht gedämpft
        }
    }
}
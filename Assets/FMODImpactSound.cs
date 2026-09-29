using UnityEngine;
using FMODUnity;
using System.Collections;

public class FMODImpactEmitter : MonoBehaviour
{
    [Header("FMOD Impact Sound")]
    public StudioEventEmitter impactEmitter;

    [Header("Impact Ring Effect")]
    public GameObject impactRingPrefab; // <- Hier Prefab reinziehen!

    [Header("Impact Settings")]
    public float minSpeed = 0.2f;
    public int numberOfRings = 3;        // Anzahl der Ringe
    public float ringDelay = 0.1f;       // Abstand zwischen den Ringen in Sekunden

    private void OnCollisionEnter(Collision collision)
    {
        float impactVelocity = collision.relativeVelocity.magnitude;

        if (impactVelocity < minSpeed)
            return;

        // Kontaktpunkt bestimmen
        ContactPoint contact = collision.contacts[0];

        // FMOD Sound abspielen
        if (impactEmitter != null)
        {
            impactEmitter.transform.position = contact.point;
            impactEmitter.Play();
        }

        // Impact Rings erzeugen (Coroutine)
        if (impactRingPrefab != null)
        {
            StartCoroutine(SpawnRings(contact.point, contact.normal));
        }
    }

    private IEnumerator SpawnRings(Vector3 position, Vector3 normal)
    {
        for (int i = 0; i < numberOfRings; i++)
        {
            GameObject ring = Instantiate(impactRingPrefab, position, Quaternion.identity);
            ring.transform.forward = -normal; // Ring normal zur Wand ausrichten
            yield return new WaitForSeconds(ringDelay);
        }
    }
}
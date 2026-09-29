using UnityEngine;
using System.Collections.Generic;

public class BallManager : MonoBehaviour
{
    public static BallManager Instance;

    [Header("Setup")]
    public List<GameObject> allBalls = new List<GameObject>();

    private Dictionary<GameObject, Vector3> startPos = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> startRot = new Dictionary<GameObject, Quaternion>();

    [HideInInspector] public GameObject lastTouchedBall;

    private void Awake()
    {
        Instance = this;
        foreach (GameObject ball in allBalls)
        {
            if (ball != null)
            {
                startPos[ball] = ball.transform.position;
                startRot[ball] = ball.transform.rotation;
            }
        }
    }

    // Diese Methode MUSS von deinem Wurf_Script/Grab-Script aufgerufen werden!
    public void RegisterAsLastTouched(GameObject ball)
    {
        lastTouchedBall = ball;
        Debug.Log("Letzter Ball registriert: " + ball.name);
    }

    public void TeleportAllToSpawn()
    {
        foreach (GameObject ball in allBalls)
        {
            if (ball != null) ResetPhysically(ball);
        }
    }

    public void TeleportLastTouchedToSpawn()
    {
        if (lastTouchedBall != null)
        {
            ResetPhysically(lastTouchedBall);
        }
        else
        {
            Debug.LogWarning("Kein Ball wurde vorher angefasst!");
        }
    }

    private void ResetPhysically(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();

        // 1. Bewegung stoppen und "einfrieren" gegen Bouncing
        if (rb != null)
        {
            rb.isKinematic = true; 
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2. Teleport auf die exakten Startwerte
        obj.transform.position = startPos[obj];
        obj.transform.rotation = startRot[obj];

        // 3. Reset-Nachricht an andere Scripte (RhythmBallY etc.)
        obj.SendMessage("ResetState", SendMessageOptions.DontRequireReceiver);

        // 4. Physik kurz danach wieder "aufwecken" (verhindert das Bouncen)
        Invoke("Unfreeze", 0.05f); 
        
        // Hilfsfunktion im selben Script
        void Unfreeze() { 
            if(rb != null) {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
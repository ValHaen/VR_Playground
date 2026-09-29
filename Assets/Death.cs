using UnityEngine;
using UnityEngine.UI; // Falls du UI Text benutzt

public class DeathPlaneTrigger : MonoBehaviour
{
    public Transform player;             // Referenz auf den Spieler
    public GameObject hypertextObject;  // Das Hypertext-Objekt, das erscheinen soll
    public Vector3 respawnPosition = Vector3.zero; // Respawn-Position (0,0,0)

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            // Spieler teleportieren
            player.position = respawnPosition;

            // Hypertext anzeigen
            if (hypertextObject != null)
            {
                hypertextObject.SetActive(true);
                CancelInvoke(nameof(HideHypertext)); // Falls vorher schon Invoke lief
                Invoke(nameof(HideHypertext), 5f);   // Nach 5 Sekunden verstecken
            }
        }
    }

    private void HideHypertext()
    {
        if (hypertextObject != null)
        {
            hypertextObject.SetActive(false);
        }
    }
    
    void Start()
{
    if (hypertextObject != null)
    {
        hypertextObject.SetActive(false);
    }
}
}

using UnityEngine;

public class BillboardText : MonoBehaviour
{
    public float yOffset = 180f;

    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // Schaut zur Kamera
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            // Korrektur, falls der Text gespiegelt ist
            transform.Rotate(0, yOffset, 0);
        }
    }
}
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRMultiplayer
{
    public class YAxisRotationOnly : MonoBehaviour
    {
        private Vector3 initialPosition;
        private float initialXRotation;
        private float initialZRotation;

        void Awake()
        {
            initialPosition = transform.position;
            initialXRotation = transform.rotation.eulerAngles.x;
            initialZRotation = transform.rotation.eulerAngles.z;
        }

        void Update()
        {
            // Position einfrieren
            transform.position = initialPosition;
            
            // Nur Y-Rotation erlauben, X und Z einfrieren
            Vector3 currentEuler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(initialXRotation, currentEuler.y, initialZRotation);
        }
    }
}
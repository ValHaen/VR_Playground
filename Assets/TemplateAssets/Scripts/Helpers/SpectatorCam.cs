using UnityEngine;
using UnityEngine.InputSystem;

namespace XRMultiplayer
{
    /// <summary>
    /// Controls the movement of the spectator camera
    /// </summary>
    public class SpectatorCam : MonoBehaviour
    {
        public float rotationSpeed = 50f;

        void Update()
        {
            float horizontal = 0f;
            float vertical = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
                    horizontal = -1f;
                else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
                    horizontal = 1f;

                if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
                    vertical = 1f;
                else if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
                    vertical = -1f;
            }

            transform.Rotate(Vector3.up, horizontal * rotationSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, -vertical * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
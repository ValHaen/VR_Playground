using Unity.XR.CoreUtils;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEngine.Android;

namespace XRMultiplayer
{
    /// <summary>
    /// Represents the offline player avatar.
    /// </summary>
    public class OfflinePlayerAvatar : MonoBehaviour
    {
        public static BindableVariable<float> voiceAmp = new BindableVariable<float>();

        /// <summary>
        /// Gets or sets a value indicating whether the player is muted.
        /// </summary>
        public static bool muted
        {
            get => s_Muted;
            set
            {
                if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                    s_Muted = value;
            }
        }

        /// <summary>
        /// A value indicating whether the player is muted.
        /// </summary>
        static bool s_Muted;

        /// <summary>
        /// The head transform.
        /// </summary>
        [SerializeField] Transform m_HeadTransform;

        /// <summary>
        /// The head renderer.
        /// </summary>
        [SerializeField] SkinnedMeshRenderer m_HeadRend;

        /// <summary>
        /// The head origin.
        /// </summary>
        Transform m_HeadOrigin;

        /// <inheritdoc/>
        void Start()
        {
            XROrigin rig = FindFirstObjectByType<XROrigin>();
            m_HeadOrigin = rig.Camera.transform;

        }

        void OnEnable()
        {
            XRINetworkGameManager.LocalPlayerColor.Subscribe(UpdatePlayerColor);
            XRINetworkGameManager.Connected.Subscribe(connected =>
            {
                gameObject.SetActive(!connected);
            });
        }

        void OnDisable()
        {
            XRINetworkGameManager.LocalPlayerColor.Unsubscribe(UpdatePlayerColor);
            XRINetworkGameManager.Connected.Unsubscribe(connected =>
            {
                gameObject.SetActive(!connected);
            });
        }

        /// <inheritdoc/>
        private void LateUpdate()
        {
            m_HeadTransform.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);
        }

        void UpdatePlayerColor(Color color)
        {
            m_HeadRend.materials[2].color = color;
        }

    }
}

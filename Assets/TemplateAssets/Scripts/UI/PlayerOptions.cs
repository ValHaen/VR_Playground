using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using TMPro;
using System;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.Android;
// WICHTIG: Das hier wird für den Szenenwechsel benötigt!
using UnityEngine.SceneManagement; 

namespace XRMultiplayer
{
    [DefaultExecutionOrder(100)]
    public class PlayerOptions : MonoBehaviour
    {
        [SerializeField] InputActionReference m_ToggleMenuAction;

        [Header("Panels")]
        [SerializeField] GameObject[] m_Panels;
        [SerializeField] Toggle[] m_PanelToggles;

        [Header("Text Components")]
        [SerializeField] TMP_Text m_SnapTurnText;
        [SerializeField] TMP_Text m_TimeText;

        [Header("Player Options")]
        [SerializeField] Vector2 m_MinMaxMoveSpeed = new Vector2(2.0f, 10.0f);
        [SerializeField] Vector2 m_MinMaxTurnAmount = new Vector2(15.0f, 180.0f);
        [SerializeField] float m_SnapTurnUpdateAmount = 15.0f;

        [Header("Scene Transition Settings")]
        [Tooltip("Der exakte Name deiner Lobby-Szene in den Build Settings")]
        [SerializeField] string m_LobbySceneName = "Lobby";

        DynamicMoveProvider m_MoveProvider;
        SnapTurnProvider m_TurnProvider;
        UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController m_TunnelingVignetteController;

        PermissionCallbacks permCallbacks;

        private void Awake()
        {
            m_MoveProvider = FindFirstObjectByType<DynamicMoveProvider>();
            m_TurnProvider = FindFirstObjectByType<SnapTurnProvider>();
            m_TunnelingVignetteController = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController>();

            XRINetworkGameManager.Connected.Subscribe(ConnectOnline);

            ConnectOnline(false);

            if (m_ToggleMenuAction != null)
                m_ToggleMenuAction.action.performed += ctx => ToggleMenu();
            else
                Utils.Log("No toggle menu action assigned to OptionsPanel", 1);

            permCallbacks = new PermissionCallbacks();
            permCallbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
            permCallbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
        }

        internal void PermissionCallbacks_PermissionGranted(string permissionName)
        {
            Utils.Log($"{permissionName} PermissionCallbacks_PermissionGranted");
        }

        internal void PermissionCallbacks_PermissionDenied(string permissionName)
        {
            Utils.Log($"{permissionName} PermissionCallbacks_PermissionDenied");
        }

        void OnEnable()
        {
            TogglePanel(0);
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(ConnectOnline);
        }

        private void Update()
        {
            m_TimeText.text = $"{DateTime.Now:h:mm}<size=4><voffset=1em>{DateTime.Now:tt}</size></voffset>";
        }

        void ConnectOnline(bool connected)
        {
            if (!connected)
            {
                ToggleMenu(false);
            }
        }

        public void TogglePanel(int panelID)
        {
            for (int i = 0; i < m_Panels.Length; i++)
            {
                m_PanelToggles[i].SetIsOnWithoutNotify(panelID == i);
                m_Panels[i].SetActive(i == panelID);
            }
        }

        public void ToggleMenu(bool overrideToggle = false, bool overrideValue = false)
        {
            if (overrideToggle)
            {
                gameObject.SetActive(overrideValue);
            }
            else
            {
                ToggleMenu();
            }
            TogglePanel(0);
        }

        public void ToggleMenu()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void LogOut()
        {
            XRINetworkGameManager.Instance.Disconnect();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ==========================================
        // NEU: HIER SIND DIE FUNKTIONEN FÜR DEN SZENENWECHSEL
        // ==========================================

        /// <summary>
        /// Lädt die im Inspector definierte Lobby-Szene und trennt vorher saubere Netzwerkverbindungen.
        /// </summary>
        public void LoadLobbyScene()
        {
            LoadSpecificScene(m_LobbySceneName);
        }

        /// <summary>
        /// Lädt jede beliebige Szene anhand des Namens (ideal für dynamische UI-Buttons).
        /// </summary>
        public void LoadSpecificScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("Szenenname ist leer! Bitte im Inspector oder Button eintragen.");
                return;
            }

            // Falls der Spieler in einer aktiven Multiplayer-Sitzung ist, trennen wir erst die Verbindung
            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
            {
                Debug.Log("Multiplayer aktiv. Trenne Verbindung vor Szenenwechsel...");
                XRINetworkGameManager.Instance.Disconnect();
            }

            Debug.Log($"Lade Szene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        // ==========================================
        // Ende der neuen Funktionen
        // ==========================================

        // Player Options
        public void SetHandOrientation(bool toggle)
        {
            if (toggle)
            {
                m_MoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HandRelative;
            }
        }
        public void SetHeadOrientation(bool toggle)
        {
            if (toggle)
            {
                m_MoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
            }
        }
        public void SetMoveSpeed(float speedPercent)
        {
            m_MoveProvider.moveSpeed = Mathf.Lerp(m_MinMaxMoveSpeed.x, m_MinMaxMoveSpeed.y, speedPercent);
        }

        public void UpdateSnapTurn(int dir)
        {
            float newTurnAmount = Mathf.Clamp(m_TurnProvider.turnAmount + (m_SnapTurnUpdateAmount * dir), m_MinMaxTurnAmount.x, m_MinMaxTurnAmount.y);
            m_TurnProvider.turnAmount = newTurnAmount;
            m_SnapTurnText.text = $"{newTurnAmount}°";
        }

        public void ToggleTunnelingVignette(bool toggle)
        {
            m_TunnelingVignetteController.gameObject.SetActive(toggle);
        }

        public void ToggleFlight(bool toggle)
        {
            m_MoveProvider.useGravity = !toggle;
            m_MoveProvider.enableFly = toggle;
        }
    }
}
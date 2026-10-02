using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using FMODUnity;

namespace XRMultiplayer
{
    /// <summary>
    /// Netzwerk-synchronisiertes UI Board mit:
    /// - Delay Volume Slider
    /// - Delay An/Aus Toggle
    /// - Reset All (Bälle zurück + FMOD Reset)
    /// - Reset Last Touched (letzter berührter Ball zurück)
    /// 
    /// SETUP:
    /// 1. Leeres GameObject "UIBoard" in der Szene
    /// 2. NetworkObject Component drauf
    /// 3. Dieses Script drauf
    /// 4. Canvas mit den UI Elementen erstellen und im Inspector verknüpfen
    /// 5. Alle SoundSpheres im Inspector in m_SoundSpheres eintragen
    /// </summary>
    public class NetworkUIBoard : NetworkBehaviour
    {
        [Header("UI Referenzen")]
        [SerializeField] Slider m_DelayVolumeSlider;
        [SerializeField] Toggle m_DelayToggle;
        [SerializeField] Button m_ResetAllButton;
        [SerializeField] Button m_ResetLastTouchedButton;
        [SerializeField] TMP_Text m_LastTouchedText;

        [Header("FMOD")]
        [SerializeField, Tooltip("FMOD Global Parameter Name für Delay Volume")]
        string m_DelayVolumeParameter = "DelayVolume";

        [SerializeField, Tooltip("FMOD Global Parameter Name für Delay An/Aus (0=aus, 1=an)")]
        string m_DelayActiveParameter = "DelayActive";

        [Header("Sound Spheres")]
        [SerializeField] SoundSphere[] m_SoundSpheres;

        // ── Netzwerk-Zustand ──────────────────────────────────────
        NetworkVariable<float> m_NetDelayVolume = new NetworkVariable<float>(
            0.5f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        NetworkVariable<bool> m_NetDelayActive = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        NetworkVariable<int> m_NetLastTouchedIndex = new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Startpositionen der Bälle
        Vector3[] m_StartPositions;
        bool m_StartPositionsSaved = false;

        // Verhindert Endlosschleife beim Toggle-Update
        bool m_UpdatingToggleUI = false;

        // ── Lifecycle ─────────────────────────────────────────────

        void Start()
        {
            SaveStartPositions();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // NetworkVariable Callbacks
            m_NetDelayVolume.OnValueChanged += OnDelayVolumeChanged;
            m_NetDelayActive.OnValueChanged += OnDelayActiveChanged;
            m_NetLastTouchedIndex.OnValueChanged += OnLastTouchedChanged;

            // UI Events
            if (m_DelayVolumeSlider != null)
                m_DelayVolumeSlider.onValueChanged.AddListener(OnSliderChanged);

            if (m_DelayToggle != null)
                m_DelayToggle.onValueChanged.AddListener(OnToggleChanged);

            if (m_ResetAllButton != null)
                m_ResetAllButton.onClick.AddListener(OnResetAllClicked);

            if (m_ResetLastTouchedButton != null)
                m_ResetLastTouchedButton.onClick.AddListener(OnResetLastTouchedClicked);

            // Initiale Werte anwenden
            ApplyDelayVolume(m_NetDelayVolume.Value);
            ApplyDelayActive(m_NetDelayActive.Value);
            UpdateSliderUI(m_NetDelayVolume.Value);
            UpdateToggleUI(m_NetDelayActive.Value);

            // SoundSpheres für Last-Touched-Tracking registrieren
            RegisterSoundSphereCallbacks();
        }

        public override void OnNetworkDespawn()
        {
            m_NetDelayVolume.OnValueChanged -= OnDelayVolumeChanged;
            m_NetDelayActive.OnValueChanged -= OnDelayActiveChanged;
            m_NetLastTouchedIndex.OnValueChanged -= OnLastTouchedChanged;

            if (m_DelayVolumeSlider != null)
                m_DelayVolumeSlider.onValueChanged.RemoveListener(OnSliderChanged);

            if (m_DelayToggle != null)
                m_DelayToggle.onValueChanged.RemoveListener(OnToggleChanged);

            base.OnNetworkDespawn();
        }

        // ── Startpositionen speichern ─────────────────────────────

        void SaveStartPositions()
        {
            if (m_SoundSpheres == null || m_StartPositionsSaved) return;

            m_StartPositions = new Vector3[m_SoundSpheres.Length];
            for (int i = 0; i < m_SoundSpheres.Length; i++)
            {
                if (m_SoundSpheres[i] != null)
                    m_StartPositions[i] = m_SoundSpheres[i].transform.localPosition;
            }
            m_StartPositionsSaved = true;
        }

        // ── SoundSphere Callbacks registrieren ────────────────────

        void RegisterSoundSphereCallbacks()
        {
            if (m_SoundSpheres == null) return;

            for (int i = 0; i < m_SoundSpheres.Length; i++)
            {
                int index = i;
                var sphere = m_SoundSpheres[i];
                if (sphere == null) continue;

                var grabInteractable = sphere.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                if (grabInteractable != null)
                {
                    grabInteractable.selectEntered.AddListener((args) =>
                    {
                        SetLastTouchedServerRpc(index);
                    });
                }
            }
        }

        // ── UI Event Handler ──────────────────────────────────────

        void OnSliderChanged(float value)
        {
            SetDelayVolumeServerRpc(value);
        }

        void OnToggleChanged(bool value)
        {
            // Wenn wir gerade das UI updaten, nicht nochmal senden
            if (m_UpdatingToggleUI) return;
            SetDelayActiveServerRpc(value);
        }

        void OnResetAllClicked()
        {
            ResetAllServerRpc();
        }

        void OnResetLastTouchedClicked()
        {
            if (m_NetLastTouchedIndex.Value >= 0)
                ResetSphereServerRpc(m_NetLastTouchedIndex.Value);
        }

        // ── Server RPCs ───────────────────────────────────────────

        [ServerRpc(RequireOwnership = false)]
        void SetDelayVolumeServerRpc(float value)
        {
            m_NetDelayVolume.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        void SetDelayActiveServerRpc(bool value)
        {
            m_NetDelayActive.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        void ResetAllServerRpc()
        {
            ResetAllClientRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        void ResetSphereServerRpc(int index)
        {
            ResetSphereClientRpc(index);
        }

        [ServerRpc(RequireOwnership = false)]
        void SetLastTouchedServerRpc(int index)
        {
            m_NetLastTouchedIndex.Value = index;
        }

        // ── Client RPCs ───────────────────────────────────────────

        [ClientRpc]
        void ResetAllClientRpc()
        {
            if (m_SoundSpheres == null) return;

            for (int i = 0; i < m_SoundSpheres.Length; i++)
                ResetSphereLocally(i);

            // FMOD zurücksetzen
            RuntimeManager.StudioSystem.setParameterByName(m_DelayVolumeParameter, 0f);
            RuntimeManager.StudioSystem.setParameterByName(m_DelayActiveParameter, 0f);

            // UI zurücksetzen
            UpdateSliderUI(0f);
            UpdateToggleUI(false);
        }

        [ClientRpc]
        void ResetSphereClientRpc(int index)
        {
            ResetSphereLocally(index);
        }

        // ── Lokale Reset-Logik ────────────────────────────────────

        void ResetSphereLocally(int index)
        {
            if (m_SoundSpheres == null || index >= m_SoundSpheres.Length) return;
            var sphere = m_SoundSpheres[index];
            if (sphere == null) return;

            // Position zurücksetzen
            if (m_StartPositionsSaved)
                sphere.transform.localPosition = m_StartPositions[index];

            // Sound deaktivieren
            sphere.DeactivateFromNetwork();

            // Rigidbody stoppen
            var rb = sphere.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // ── NetworkVariable Callbacks ─────────────────────────────

        void OnDelayVolumeChanged(float previous, float current)
        {
            ApplyDelayVolume(current);
            UpdateSliderUI(current);
        }

        void OnDelayActiveChanged(bool previous, bool current)
        {
            ApplyDelayActive(current);
            UpdateToggleUI(current);
        }

        void OnLastTouchedChanged(int previous, int current)
        {
            UpdateLastTouchedUI(current);
        }

        // ── FMOD anwenden ─────────────────────────────────────────

        void ApplyDelayVolume(float value)
        {
            RuntimeManager.StudioSystem.setParameterByName(m_DelayVolumeParameter, value);
        }

        void ApplyDelayActive(bool active)
        {
            RuntimeManager.StudioSystem.setParameterByName(m_DelayActiveParameter, active ? 1f : 0f);
        }

        // ── UI updaten ────────────────────────────────────────────

        void UpdateSliderUI(float value)
        {
            if (m_DelayVolumeSlider != null)
                m_DelayVolumeSlider.SetValueWithoutNotify(value);
        }

        void UpdateToggleUI(bool active)
        {
            if (m_DelayToggle == null) return;

            // Flag setzen damit OnToggleChanged keinen weiteren ServerRpc schickt
            m_UpdatingToggleUI = true;
            m_DelayToggle.SetIsOnWithoutNotify(active);
            m_UpdatingToggleUI = false;
        }

        void UpdateLastTouchedUI(int index)
        {
            if (m_LastTouchedText == null) return;

            if (index < 0 || m_SoundSpheres == null || index >= m_SoundSpheres.Length)
                m_LastTouchedText.text = "Zuletzt: —";
            else if (m_SoundSpheres[index] != null)
                m_LastTouchedText.text = $"Zuletzt: {m_SoundSpheres[index].gameObject.name}";
        }
    }
}
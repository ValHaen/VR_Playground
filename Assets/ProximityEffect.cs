using UnityEngine;
using Unity.Netcode;
using FMODUnity;
using FMOD.Studio;

namespace XRMultiplayer
{
    /// <summary>
    /// Misst den Abstand zwischen zwei Spielern.
    /// Je näher sie kommen: mehr Funken + lauteres FMOD Ambient.
    /// 
    /// SETUP:
    /// 1. Leeres GameObject in der Spielszene erstellen: "ProximityManager"
    /// 2. NetworkObject Component drauf
    /// 3. Dieses Script drauf
    /// 4. ParticleSystem für Funken erstellen und im Inspector zuweisen
    /// 5. FMOD Event Pfad für Ambient eintragen
    /// </summary>
    public class PlayerProximityEffect : NetworkBehaviour
    {
        [Header("Abstands-Einstellungen")]
        [SerializeField, Tooltip("Ab diesem Abstand (Meter) beginnt der Effekt")]
        float m_MaxDistance = 3f;

        [SerializeField, Tooltip("Unter diesem Abstand ist der Effekt maximal")]
        float m_MinDistance = 0.5f;

        [Header("Funken Particle System")]
        [SerializeField, Tooltip("ParticleSystem für die Funken — zwischen den Spielern")]
        ParticleSystem m_SparkParticles;

        [SerializeField, Tooltip("Maximale Funken pro Sekunde wenn ganz nah")]
        float m_MaxEmissionRate = 80f;

        [Header("FMOD Ambient")]
        [SerializeField, Tooltip("FMOD Event für den Ambient Sound")]
        EventReference m_AmbientEvent;

        [SerializeField, Tooltip("FMOD Parameter Name für die Lautstärke/Intensität")]
        string m_IntensityParameter = "Intensity";

        // NetworkVariable: Owner schreibt Intensität, alle lesen
        NetworkVariable<float> m_NetIntensity = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        // NetworkVariable: Mittelpunkt zwischen den Spielern (für Partikel-Position)
        NetworkVariable<Vector3> m_NetMidpoint = new NetworkVariable<Vector3>(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        // Lokale FMOD Instanz
        EventInstance m_AmbientInstance;
        bool m_AmbientPlaying = false;

        // Update-Rate
        float m_UpdateInterval = 0.05f;
        float m_LastUpdateTime = 0f;

        // ── Lifecycle ─────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_NetIntensity.OnValueChanged += OnIntensityChanged;
            m_NetMidpoint.OnValueChanged += OnMidpointChanged;

            // Partikel initial stoppen
            if (m_SparkParticles != null)
                m_SparkParticles.Stop();
        }

        public override void OnNetworkDespawn()
        {
            m_NetIntensity.OnValueChanged -= OnIntensityChanged;
            m_NetMidpoint.OnValueChanged -= OnMidpointChanged;

            StopAmbient();
            base.OnNetworkDespawn();
        }

        void Update()
        {
            // Nur der Host berechnet den Abstand (hat Zugriff auf alle Spieler)
            if (!IsHost) return;
            if (Time.time - m_LastUpdateTime < m_UpdateInterval) return;
            m_LastUpdateTime = Time.time;

            CalculateProximity();
        }

        // ── Host: Abstand berechnen ───────────────────────────────

        void CalculateProximity()
        {
            // Alle verbundenen Spieler finden
            var players = FindObjectsByType<XRINetworkPlayer>(FindObjectsSortMode.None);

            if (players.Length < 2)
            {
                // Weniger als 2 Spieler — Effekt aus
                if (m_NetIntensity.Value > 0f)
                    m_NetIntensity.Value = 0f;
                return;
            }

            // Nächstes Spielerpaar finden (bei mehr als 2 Spielern)
            float closestDistance = float.MaxValue;
            Vector3 midpoint = Vector3.zero;

            for (int i = 0; i < players.Length; i++)
            {
                for (int j = i + 1; j < players.Length; j++)
                {
                    float dist = Vector3.Distance(
                        players[i].transform.position,
                        players[j].transform.position
                    );

                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        midpoint = (players[i].transform.position + players[j].transform.position) / 2f;
                    }
                }
            }

            // Intensität berechnen: 0 = weit weg, 1 = ganz nah
            float intensity = 0f;
            if (closestDistance <= m_MaxDistance)
            {
                intensity = Mathf.InverseLerp(m_MaxDistance, m_MinDistance, closestDistance);
                intensity = Mathf.Clamp01(intensity);
            }

            // Ans Netzwerk schicken
            m_NetIntensity.Value = intensity;
            if (intensity > 0f)
                m_NetMidpoint.Value = midpoint;
        }

        // ── Callbacks: auf allen Clients ─────────────────────────

        void OnIntensityChanged(float previous, float current)
        {
            ApplyEffects(current);
        }

        void OnMidpointChanged(Vector3 previous, Vector3 current)
        {
            // Partikel-Position zwischen den Spielern updaten
            if (m_SparkParticles != null)
                m_SparkParticles.transform.position = current;
        }

        void ApplyEffects(float intensity)
        {
            ApplyParticleEffect(intensity);
            ApplyFMODEffect(intensity);
        }

        // ── Funken Partikel ───────────────────────────────────────

        void ApplyParticleEffect(float intensity)
        {
            if (m_SparkParticles == null) return;

            if (intensity <= 0f)
            {
                if (m_SparkParticles.isPlaying)
                    m_SparkParticles.Stop();
                return;
            }

            // Emission Rate basierend auf Intensität
            var emission = m_SparkParticles.emission;
            emission.rateOverTime = intensity * m_MaxEmissionRate;

            if (!m_SparkParticles.isPlaying)
                m_SparkParticles.Play();
        }

        // ── FMOD Ambient ──────────────────────────────────────────

        void ApplyFMODEffect(float intensity)
        {
            if (m_AmbientEvent.IsNull) return;

            if (intensity <= 0f)
            {
                StopAmbient();
                return;
            }

            // Ambient starten falls noch nicht läuft
            if (!m_AmbientPlaying)
                StartAmbient();

            // Intensitäts-Parameter setzen
            if (m_AmbientInstance.isValid())
                m_AmbientInstance.setParameterByName(m_IntensityParameter, intensity);
        }

        void StartAmbient()
        {
            if (m_AmbientPlaying) return;
            m_AmbientInstance = RuntimeManager.CreateInstance(m_AmbientEvent);
            m_AmbientInstance.start();
            m_AmbientPlaying = true;
        }

        void StopAmbient()
        {
            if (!m_AmbientPlaying) return;
            m_AmbientInstance.stop(STOP_MODE.ALLOWFADEOUT);
            m_AmbientInstance.release();
            m_AmbientPlaying = false;
        }
    }
}
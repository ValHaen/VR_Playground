using FMOD.Studio;
using FMODUnity;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
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

        // Server schreibt, alle lesen
        NetworkVariable<float> m_NetIntensity = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        NetworkVariable<Vector3> m_NetMidpoint = new NetworkVariable<Vector3>(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        EventInstance m_AmbientInstance;
        bool m_AmbientPlaying = false;

        float m_UpdateInterval = 0.016f; // ~60x pro Sekunde
        float m_LastUpdateTime = 0f;

        // Gecachte Spielerliste — nicht jeden Frame neu suchen
        XRINetworkPlayer[] m_CachedPlayers = new XRINetworkPlayer[0];
        float m_PlayerCacheInterval = 2f; // alle 2 Sekunden neu suchen
        float m_LastPlayerCacheTime = 0f;

        // ── Lifecycle ─────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_NetIntensity.OnValueChanged += OnIntensityChanged;
            m_NetMidpoint.OnValueChanged += OnMidpointChanged;

            if (m_SparkParticles != null)
            {
                // World Space: Partikel bleiben im Raum, fliegen nicht mit
                var main = m_SparkParticles.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                m_SparkParticles.Stop();
            }
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
            // Alle Clients: Partikel-Position jeden Frame direkt setzen
            if (m_SparkParticles != null && m_NetIntensity.Value > 0f)
                m_SparkParticles.transform.position = m_NetMidpoint.Value;

            // Nur Host berechnet Abstand
            if (!IsHost) return;
            if (Time.time - m_LastUpdateTime < m_UpdateInterval) return;
            m_LastUpdateTime = Time.time;

            CalculateProximity();
        }

        // ── Host: Abstand berechnen ───────────────────────────────

        void CalculateProximity()
        {
            // Spielerliste nur alle 2 Sekunden neu laden statt jeden Frame
            if (Time.time - m_LastPlayerCacheTime > m_PlayerCacheInterval)
            {
                m_CachedPlayers = FindObjectsByType<XRINetworkPlayer>(FindObjectsSortMode.None);
                m_LastPlayerCacheTime = Time.time;
            }

            var players = m_CachedPlayers;

            if (players.Length < 2)
            {
                if (m_NetIntensity.Value > 0f)
                    m_NetIntensity.Value = 0f;
                return;
            }

            float closestDistance = float.MaxValue;
            Vector3 midpoint = Vector3.zero;

            for (int i = 0; i < players.Length; i++)
            {
                for (int j = i + 1; j < players.Length; j++)
                {
                    // Kopf-Position für genaueres Tracking
                    Vector3 posA = players[i].head != null
                        ? players[i].head.position
                        : players[i].transform.position;
                    Vector3 posB = players[j].head != null
                        ? players[j].head.position
                        : players[j].transform.position;

                    float dist = Vector3.Distance(posA, posB);

                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        midpoint = (posA + posB) / 2f;
                    }
                }
            }

            float intensity = 0f;
            if (closestDistance <= m_MaxDistance)
                intensity = Mathf.Clamp01(Mathf.InverseLerp(m_MaxDistance, m_MinDistance, closestDistance));

            m_NetIntensity.Value = intensity;
            m_NetMidpoint.Value = midpoint; // immer updaten, nicht nur wenn > 0
        }

        // ── Callbacks ────────────────────────────────────────────

        void OnIntensityChanged(float previous, float current)
        {
            ApplyParticleEffect(current);
            ApplyFMODEffect(current);
        }

        void OnMidpointChanged(Vector3 previous, Vector3 current)
        {
            if (m_SparkParticles != null)
                m_SparkParticles.transform.position = current;
        }

        // ── Partikel ─────────────────────────────────────────────

        void ApplyParticleEffect(float intensity)
        {
            if (m_SparkParticles == null) return;

            if (intensity <= 0f)
            {
                if (m_SparkParticles.isPlaying)
                    m_SparkParticles.Stop();
                return;
            }

            var emission = m_SparkParticles.emission;
            emission.rateOverTime = intensity * m_MaxEmissionRate;

            if (!m_SparkParticles.isPlaying)
                m_SparkParticles.Play();
        }

        // ── FMOD ─────────────────────────────────────────────────

        void ApplyFMODEffect(float intensity)
        {
            if (m_AmbientEvent.IsNull) return;

            if (intensity <= 0f)
            {
                StopAmbient();
                return;
            }

            if (!m_AmbientPlaying)
                StartAmbient();

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
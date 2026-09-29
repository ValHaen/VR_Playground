using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XRMultiplayer
{
    /// <summary>
    /// Manages the network functionality for VR multiplayer.
    /// </summary>
    public class NetworkManagerVRMultiplayer : NetworkManager
    {
        [SerializeField, Tooltip("Set this to control how much logging is generated")]
        LogLevel m_LogLevel;

        [SerializeField, Tooltip("This should almost always be set to true")]
        bool m_RunInBackground = true;

        [SerializeField]
        NetworkConfig m_NetworkConfig;

        ///<inheritdoc/>
        void Awake()
        {
            LogLevel = m_LogLevel;
            RunInBackground = m_RunInBackground;
            NetworkConfig = m_NetworkConfig;
            Utils.s_LogLevel = LogLevel;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkManagerVRMultiplayer))]
    class VRMutliplayerTemplateNetworkManagerEditor : Editor
    {
        // IP-Adresse die im Inspector eingegeben wird
        string m_DebugIP = "127.0.0.1";

        /// <summary>
        /// This function is called when the inspector is drawn.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("── Netzwerk Status ──", EditorStyles.boldLabel);

                switch (XRINetworkGameManager.CurrentConnectionState.Value)
                {
                    case XRINetworkGameManager.ConnectionState.None:
                        GUILayout.Box("Authenticating");
                        break;

                    case XRINetworkGameManager.ConnectionState.Authenticating:
                        GUILayout.Box("Authenticating");
                        break;

                    case XRINetworkGameManager.ConnectionState.Authenticated:
                        // Server starten
                        if (GUILayout.Button("▶ Server starten"))
                        {
                            XRINetworkGameManager.Instance.StartServer();
                        }

                        // Host starten
                        if (GUILayout.Button("▶ Host starten (Server + Spieler)"))
                        {
                            XRINetworkGameManager.Instance.StartHost();
                        }

                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("── Debug Join ──", EditorStyles.boldLabel);

                        // IP Eingabefeld
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Ziel-IP:", GUILayout.Width(55));
                        m_DebugIP = EditorGUILayout.TextField(m_DebugIP);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.HelpBox(
                            "ParrelSync / selber PC: 127.0.0.1\n" +
                            "Anderes Gerät im LAN: z.B. 192.168.1.50",
                            MessageType.Info);

                        // Join Button
                        if (GUILayout.Button("🔗 Als Client joinen"))
                        {
                            var transport = FindFirstObjectByType<UnityTransport>();
                            if (transport != null)
                            {
                                transport.ConnectionData.Address = m_DebugIP.Trim();
                                transport.ConnectionData.Port = 7777;
                                Debug.Log($"[DEBUG JOIN] Verbinde mit {m_DebugIP}:7777");
                                XRINetworkGameManager.Instance.StartCoroutine(
                                    XRINetworkGameManager.Instance.Join()
                                );
                            }
                            else
                            {
                                Debug.LogError("[DEBUG JOIN] UnityTransport nicht gefunden!");
                            }
                        }
                        break;

                    case XRINetworkGameManager.ConnectionState.Connecting:
                        GUILayout.Box("Verbinde...");
                        break;

                    case XRINetworkGameManager.ConnectionState.Connected:
                        EditorGUILayout.HelpBox(
                            $"Verbunden als: {(NetworkManager.Singleton.IsHost ? "Host" : "Client")}\n" +
                            $"Spieler online: {NetworkManager.Singleton.ConnectedClients.Count}",
                            MessageType.None);

                        if (GUILayout.Button("✖ Trennen"))
                        {
                            XRINetworkGameManager.Instance.Disconnect();
                        }
                        break;
                }
            }
            else
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Debug-IP:", GUILayout.Width(65));
                m_DebugIP = EditorGUILayout.TextField(m_DebugIP);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("IP wird beim Joinen im Play-Modus verwendet.", MessageType.None);
                GUILayout.Box("Spiel läuft nicht.");
            }
        }
    }
#endif
}
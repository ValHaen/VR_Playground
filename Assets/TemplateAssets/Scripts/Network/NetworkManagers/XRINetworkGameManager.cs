using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode.Transports.UTP;

namespace XRMultiplayer
{
#if USE_FORCED_BYTE_SERIALIZATION
    /// <summary>
    /// Workaround for a bug introduced in NGO 1.9.1.
    /// </summary>
    /// <remarks> Delete this class once the bug is fixed in NGO.</remarks>
    class ForceByteSerialization : NetworkBehaviour
    {
        NetworkVariable<byte> m_ForceByteSerialization;
    }
#endif

    /// <summary>
    /// Manages the high level connection for a networked game session.
    /// </summary>
    public class XRINetworkGameManager : NetworkBehaviour
    {
        [SerializeField] FadeScreen fadeScreen;
        [SerializeField] GameObject[] DontDestroyObjects;
        GameObject m_JoinTeleportTransformTemplate;

        /// <summary>
        /// Determines the current state of the networked game connection.
        /// </summary>
        ///<remarks>
        /// None: No connection state.
        /// Authenticating: Currently authenticating.
        /// Authenticated: Authenticated.
        /// Connecting: Currently connecting to a lobby.
        /// Connected: Connected to a lobby.
        /// </remarks>
        public enum ConnectionState
        {
            None,
            Authenticating,
            Authenticated,
            Connecting,
            Connected
        }

        /// <summary>
        /// Singleton Reference for access to this manager.
        /// </summary>
        public static XRINetworkGameManager Instance => s_Instance;
        static XRINetworkGameManager s_Instance;

        /// <summary>
        /// OwnerClientId that gets set for the local player when connecting to a game.
        /// </summary>
        public static ulong LocalId;

        /// <summary>
        /// Bindable Variable that gets updated when the local player changes name.
        /// </summary>
        public static BindableVariable<string> LocalPlayerName = new("Player");

        /// <summary>
        /// Bindable Variable that gets updated when the local player changes color.
        /// </summary>
        public static BindableVariable<Color> LocalPlayerColor = new(Color.white);

        /// <summary>
        /// Bindable Variable that gets updated when a player connects or disconnects from a networked game.
        /// </summary>
        public static IReadOnlyBindableVariable<bool> Connected
        {
            get => m_Connected;
        }
        static BindableVariable<bool> m_Connected = new BindableVariable<bool>(false);

        /// <summary>
        /// Bindable Variable that gets updated throughout the authentication and connection process.
        /// See <see cref="ConnectionState"/>
        /// </summary>
        public static IReadOnlyBindableVariable<ConnectionState> CurrentConnectionState
        {
            get => m_ConnectionState;
        }
        static BindableEnum<ConnectionState> m_ConnectionState = new BindableEnum<ConnectionState>(ConnectionState.None);

        /// <summary>
        /// Action for when a player connects or disconnects.
        /// </summary>
        public Action<ulong, bool> playerStateChanged;

        /// <summary>
        /// Action for when connection status is updated.
        /// </summary>
        public Action<string> connectionUpdated;

        /// <summary>
        /// Action for when connection fails.
        /// </summary>
        public Action<string> connectionFailedAction;

        /// <summary>
        /// List that handles all current players by ID.
        /// Useful for getting specific players.
        /// See <see cref="GetPlayerByID"/>
        /// </summary>
        readonly List<ulong> m_CurrentPlayerIDs = new();

        /// <summary>
        /// Flagged whenever the application is in the process of shutting down.
        /// </summary>
        bool m_IsShuttingDown = false;

        const string k_DebugPrepend = "<color=#FAC00C>[Network Game Manager]</color> ";
        public static string deviceId;
        readonly List<string> SpawnClientList = new();
        TeleportationProvider m_LocalPlayerTeleportProvider;
        [HideInInspector]
        public bool IsConnected;
        bool IsLoadingScene = false;
        Transform joinTransform;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Awake()
        {
            // Check for existing singleton reference. If once already exists early out.
            if (s_Instance != null)
            {
                Utils.Log($"{k_DebugPrepend}Duplicate XRINetworkGameManager found, destroying.", 2);
                Destroy(gameObject);
                return;
            }
            s_Instance = this;

            // Initialize bindable variables.
            m_Connected.Value = false;
            // Update connection state.
            m_ConnectionState.Value = ConnectionState.Authenticated;


            if (PlayerPrefs.HasKey("device_id"))
            {
                deviceId = PlayerPrefs.GetString("device_id");
                Utils.Log($"{k_DebugPrepend}Found Device specific ID: " + deviceId);
            }
            else
            {
                deviceId = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("device_id", deviceId);
                PlayerPrefs.Save();
                Utils.Log($"{k_DebugPrepend}New Device specific ID: " + deviceId);
            }

        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Start()
        {
            NetworkManager.Singleton.OnClientStopped += OnLocalClientStopped;
            IsConnected = false;
            m_LocalPlayerTeleportProvider = FindFirstObjectByType<TeleportationProvider>();

            foreach (var o in DontDestroyObjects)
            {
                DontDestroyOnLoad(o);
            }

            SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();

            ShutDown();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        private void OnApplicationQuit()
        {
            ShutDown();
        }

        void ShutDown()
        {
            if (m_IsShuttingDown) return;
            m_IsShuttingDown = true;

            // Remove callbacks
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientStopped -= OnLocalClientStopped;
            }
        }

        /// <summary>
        /// Called from XRINetworkPlayer once they have spawned.
        /// </summary>
        /// <param name="localPlayerId">Sets based on <see cref="NetworkObject.OwnerClientId"/> from the local player</param>
        public virtual void LocalPlayerConnected(ulong localPlayerId)
        {
            m_Connected.Value = true;
            LocalId = localPlayerId;
            PlayerHudNotification.Instance.ShowText($"<b>Status:</b> " + localPlayerId + " Connected");
            m_ConnectionState.Value = ConnectionState.Connected;

            NetworkManager.SceneManager.OnLoadComplete += HandleSceneLoaded;

            fadeScreen.FadeIn();
            
            // Einen Frame warten bevor SpawnHandlingServerRpc aufgerufen wird
            // damit NGO alle NetworkBehaviours registriert hat
            StartCoroutine(DelayedSpawnHandling(localPlayerId));
        }

        IEnumerator DelayedSpawnHandling(ulong localPlayerId)
        {
            yield return null; // einen Frame warten
            SpawnHandlingServerRpc(deviceId);
        }

        /// <summary>
        /// Called when disconnected from any networked game.
        /// </summary>
        /// <param name="id">
        /// Local player id.
        /// </param>
        protected virtual void OnLocalClientStopped(bool id)
        {
            m_Connected.Value = false;
            m_CurrentPlayerIDs.Clear();
            PlayerHudNotification.Instance.ShowText($"<b>Status:</b> Disconnected");
            m_ConnectionState.Value = ConnectionState.None;
        }

        /// <summary>
        /// Finds all <see cref="XRINetworkPlayer"/>'s existing in the scene and gets the <see cref="XRINetworkPlayer"/>
        /// based on <see cref="NetworkObject.OwnerClientId"/> for that player.
        /// </summary>
        /// <param name="id">
        /// <see cref="NetworkObject.OwnerClientId"/> of the player.
        /// </param>
        /// <param name="player">
        /// Out <see cref="XRINetworkPlayer"/>.
        /// </param>
        /// <returns>
        /// Returns true based on whether or not a player with that Id exists.
        /// </returns>
        public virtual bool GetPlayerByID(ulong id, out XRINetworkPlayer player)
        {
            // Find all existing players in scene. This is a workaround until NGO exposes client side player list (2.x I believe - JG).
            XRINetworkPlayer[] allPlayers = FindObjectsByType<XRINetworkPlayer>(FindObjectsSortMode.None);

            //Loops through existing players and returns true if player with id is found.
            foreach (XRINetworkPlayer p in allPlayers)
            {
                if (p.NetworkObject.OwnerClientId == id)
                {
                    player = p;
                    return true;
                }
            }
            player = null;
            return false;
        }

        [ContextMenu("Show All NetworkClients")]
        void ShowAllNetworkClients()
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                Debug.Log($"Client: {client.Key}, {client.Value.PlayerObject.name}");
            }
        }

        /// <summary>
        /// This function will set the player ID in the list <see cref="m_CurrentPlayerIDs"/> and
        /// invokes the callback <see cref="playerStateChanged"/>.
        /// </summary>
        /// <param name="playerID"><see cref="NetworkObject.OwnerClientId"/> of the joined player.</param>
        /// <remarks>Called from <see cref="XRINetworkPlayer.CompleteSetup"/>.</remarks>
        public virtual void PlayerJoined(ulong playerID)
        {
            // If playerID is not already registered, then add.
            if (!m_CurrentPlayerIDs.Contains(playerID))
            {
                m_CurrentPlayerIDs.Add(playerID);
                playerStateChanged?.Invoke(playerID, true);
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Trying to Add a player ID [{playerID}] that already exists", 1);
            }
        }

        /// <summary>
        /// Called from <see cref="XRINetworkPlayer.OnDestroy"/>.
        /// </summary>
        /// <param name="playerID"><see cref="NetworkObject.OwnerClientId"/> of the player who left.</param>
        public virtual void PlayerLeft(ulong playerID)
        {
            // Check to make sure player has been registerd.
            if (m_CurrentPlayerIDs.Contains(playerID))
            {
                m_CurrentPlayerIDs.Remove(playerID);
                playerStateChanged?.Invoke(playerID, false);
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Trying to remove a player ID [{playerID}] that doesn't exist", 1);
            }
        }

        /// <summary>
        /// Called whenever there is a problem with connecting to game or lobby.
        /// </summary>
        /// <param name="reason">Failure message.</param>
        public virtual void ConnectionFailed(string reason)
        {
            connectionFailedAction?.Invoke(reason);
            m_ConnectionState.Value = ConnectionState.None;
        }

        /// <summary>
        /// Called whenever there is an update to connection status.
        /// </summary>
        /// <param name="update">Status update message.</param>
        public virtual void ConnectionUpdated(string update)
        {
            connectionUpdated?.Invoke(update);
        }

        /// <summary>
        /// High Level Disconnect call.
        /// </summary>
        public virtual void Disconnect()
        {
            StartCoroutine(DisconnectAsync());
        }

        /// <summary>
        /// Awaitable Disconnect call, used for Hot Joining.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator DisconnectAsync()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName != "LobbyScene")
            {
                yield return StartCoroutine(HandleSceneLoading("LobbyScene", false));
            }
            m_Connected.Value = false;
            NetworkManager.Shutdown();
            m_ConnectionState.Value = ConnectionState.None;
            IsConnected = false;

            Utils.Log($"{k_DebugPrepend}Disconnected from Game.");
        }


        // ###########################################################################
        // ################################# Own Code ################################
        // ###########################################################################
        

        // ################ Network Manager Code ##################

        public virtual void StartServer()
        {
            bool connected = false;

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                connected = networkManager.StartServer();
            }

            if (connected)
            {
                Utils.Log($"{k_DebugPrepend}Connected to game session.");
                m_ConnectionState.Value = ConnectionState.Connected;
                m_Connected.Value = true;
                IsConnected = true;
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Failed to connect");
            }

        }
        
        public virtual void StartHost()
        {
            StartCoroutine(StartHostCoroutine());
        }

        IEnumerator StartHostCoroutine()
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // Auf allen Interfaces horchen, nicht nur localhost
                transport.ConnectionData.ServerListenAddress = "0.0.0.0";
            }

            bool connected = NetworkManager.Singleton.StartHost();
            yield return null;

            if (connected)
            {
                Utils.Log($"{k_DebugPrepend}Started as Host.");
                m_ConnectionState.Value = ConnectionState.Connected;
                m_Connected.Value = true;
                IsConnected = true;
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Failed to start host.");
                ConnectionFailed("Host konnte nicht gestartet werden.");
            }
        }

        public virtual IEnumerator Join()
        {
            bool connected = false;

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                //fadeScreen.FadeOut();
                //yield return new WaitForSeconds(fadeScreen.fadeDuration);
                connected = networkManager.StartClient();
            }

            if (connected)
            {
                Utils.Log($"{k_DebugPrepend}Connected to game session.");
                IsConnected = true;
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Failed to connect");
            }
            yield break;
        }


        // ################ Spawn Handling Code ##################

        [ServerRpc(RequireOwnership = false)]
        void SpawnHandlingServerRpc(string clientDeviceId)
        {
            m_JoinTeleportTransformTemplate = GameObject.Find("SpawningElements");
            int joinTransformIndex = 0;
            if (SpawnClientList.Contains(clientDeviceId))
            {
                joinTransformIndex = SpawnClientList.IndexOf(clientDeviceId) + 1;
            }
            else
            {
                SpawnClientList.Add(clientDeviceId);
                if (SpawnClientList.Count <= m_JoinTeleportTransformTemplate.GetComponent<SpawningPointsHelper>().m_maxPlayerCount)
                {
                    joinTransformIndex = SpawnClientList.IndexOf(clientDeviceId) + 1;
                }
            }
            SpawnHandlingClientRpc(joinTransformIndex, clientDeviceId);
        }

        [ClientRpc]
        void SpawnHandlingClientRpc(int pointIndex, string clientDeviceId)
        {
            if (clientDeviceId != deviceId) return;

            joinTransform = GameObject.Find("SpawnPoint_" + pointIndex).transform;

            // Teleport client to spawning point based on order of joining
            TeleportRequest teleportRequest = new()
            {
                destinationPosition = joinTransform.position,
                destinationRotation = joinTransform.rotation,
                matchOrientation = MatchOrientation.TargetUpAndForward
            };
            m_LocalPlayerTeleportProvider.QueueTeleportRequest(teleportRequest);
            GameObject sceneArea = GameObject.Find("ReadyUpArea");
            if (sceneArea != null && (pointIndex == 1 || NetworkManager.Singleton.IsHost))
                {
                    sceneArea.GetComponent<SetSceneReadyScript>().ActivateButton();
                }
        }


        // ################ Scene Changing Code ##################

        [ServerRpc(RequireOwnership = false)]
        public void LoadSceneServerRpc(string sceneName)
        {
            if (!IsLoadingScene)
            {
                StartCoroutine(HandleSceneLoading(sceneName, true));
                Utils.Log($"{k_DebugPrepend}Scene {sceneName} loading.");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void BackToLobbyButtonServerRpc()
        {
            if (!IsLoadingScene)
            {
                StartCoroutine(HandleSceneLoading("LobbyScene", true));
            }
        }

        IEnumerator HandleSceneLoading(string sceneName, bool isNetworkLoadScene)
        {
            IsLoadingScene = true;
            if (IsServer)
            {
                FadeTransitionClientRpc();
                // Remove the NetworkPrefabLists from the NetworkManager
                NetworkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            }
            else
            {
                //fadeScreen.FadeOut();
            }
            //yield return new WaitForSeconds(fadeScreen.fadeDuration);

            // Load new Scene
            if (isNetworkLoadScene)
            {
                var status = NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogWarning($"Failed to load {sceneName} " + $"with a {nameof(SceneEventProgressStatus)}: {status}");
                }
            }
            else
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                //fadeScreen.FadeIn();
            }
            IsLoadingScene = false;
            yield break;
        }

        public void HandleSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (IsClient && clientId == NetworkManager.Singleton.LocalClientId)
            {
                Utils.Log($"{k_DebugPrepend}Scene {sceneName} loaded, spawning local player.");

                //fadeScreen.FadeIn();

                var changeSettingsButton = FindFirstObjectByType<ChangeSettingsButton>();
                if (changeSettingsButton && changeSettingsButton != null)
                {
                    if (sceneName == "LobbyScene")
                    {
                        changeSettingsButton.ShowQuitButton();
                    }
                    else
                    {
                        changeSettingsButton.ShowBackToLobbyButton();
                    }
                }
                
                SpawnHandlingServerRpc(deviceId);
            }
        }


        // ################ Other ##################

        [ClientRpc]
        void FadeTransitionClientRpc()
        {
            //fadeScreen.FadeOut();
        }
        
        public void ResetOwnPosition()
        {
            // Teleport client back to spawning point
            TeleportRequest teleportRequest = new()
            {
                destinationPosition = joinTransform.position,
                destinationRotation = joinTransform.rotation,
                matchOrientation = MatchOrientation.TargetUpAndForward
            };
            m_LocalPlayerTeleportProvider.QueueTeleportRequest(teleportRequest);
        }

    }
}

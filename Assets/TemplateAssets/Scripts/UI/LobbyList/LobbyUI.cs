using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;

namespace XRMultiplayer
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Connection Texts")]
        [SerializeField] TMP_Text m_ConnectionUpdatedText;
        [SerializeField] TMP_Text m_ConnectionSuccessText;
        [SerializeField] TMP_Text m_ConnectionFailedText;

        [SerializeField] GameObject[] m_ConnectionSubPanels;

        [Header("IP Input")]
        [SerializeField] TMP_InputField m_IPInputField;

        int m_PlayerCount;


        private void Start()
        {
            m_PlayerCount = 1;

            XRINetworkGameManager.Instance.connectionFailedAction += FailedToConnect;
            XRINetworkGameManager.Instance.connectionUpdated += ConnectedUpdated;
        }

        private void OnEnable()
        {
            CheckInternetAsync();
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Instance.connectionFailedAction -= FailedToConnect;
            XRINetworkGameManager.Instance.connectionUpdated -= ConnectedUpdated;
        }

        public void CheckInternetAsync()
        {
            CheckForInternet();
        }

        void CheckForInternet()
        {
            ToggleConnectionSubPanel(0);
        }

        public void UpdatePlayerCount(int count)
        {
            m_PlayerCount = count;
        }

        public void JoinLobby()
        {
            // transport hier oben deklarieren damit er im ganzen Block sichtbar ist
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (m_IPInputField != null && !string.IsNullOrWhiteSpace(m_IPInputField.text))
            {
                if (transport != null)
                {
                    transport.ConnectionData.Address = m_IPInputField.text.Trim();
                    transport.ConnectionData.Port = 7777;
                }
            }

            Debug.Log($"[JOIN DEBUG] Trying to connect to: {transport?.ConnectionData.Address}:{transport?.ConnectionData.Port}");

            ToggleConnectionSubPanel(2);
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            StartCoroutine(XRINetworkGameManager.Instance.Join());
            m_ConnectionSuccessText.text = "Joining";
        }

        public void StartHost()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.Instance.StartHost();
            ToggleConnectionSubPanel(2);
            m_ConnectionSuccessText.text = "Host wird gestartet...";
        }

        public void ToggleConnectionSubPanel(int panelId)
        {
            for (int i = 0; i < m_ConnectionSubPanels.Length; i++)
            {
                m_ConnectionSubPanels[i].SetActive(i == panelId);
            }
        }

        void OnConnected(bool connected)
        {
            if (connected)
            {
                ToggleConnectionSubPanel(3);
                XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            }
        }

        void ConnectedUpdated(string update)
        {
            m_ConnectionUpdatedText.text = $"<b>Status:</b> {update}";
        }

        public void FailedToConnect(string reason)
        {
            ToggleConnectionSubPanel(4);
            m_ConnectionFailedText.text = $"<b>Error:</b> {reason}";
        }
    }
}
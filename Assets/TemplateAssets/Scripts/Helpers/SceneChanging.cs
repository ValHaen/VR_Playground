using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace XRMultiplayer
{
    /// <summary>
    /// Controls the logic for the Lobby Scene Changer
    /// </summary>
    public class SetSceneReadyScript : NetworkBehaviour
    {
        [SerializeField] string[] m_sceneNames;
        [SerializeField] GameObject[] m_UiElementToActivate;
        private string currentScene;
        private int currentSceneIndex = 0;
        private TextMeshProUGUI textElement;
        void Start()
        {
            textElement = transform.GetComponentInChildren<TextMeshProUGUI>();
            textElement.text = m_sceneNames[currentSceneIndex];
            currentScene = m_sceneNames[currentSceneIndex];
            foreach (var ui in m_UiElementToActivate)
            {
                ui.SetActive(false);
            }
        }

        public void PressSwitchSceneButton()
        {
            XRINetworkGameManager.Instance.LoadSceneServerRpc(currentScene);
        }

        public void PressChangeSceneRightButton()
        {
            if (currentSceneIndex < m_sceneNames.Length - 1)
            {
                currentSceneIndex++;
            }
            else
            {
                currentSceneIndex = 0;
            }
            ChangeSceneNameServerRpc(m_sceneNames[currentSceneIndex]);
        }

        public void PressChangeSceneLeftButton()
        {
            if (currentSceneIndex > 0)
            {
                currentSceneIndex--;
            }
            else
            {
                currentSceneIndex = m_sceneNames.Length - 1;
            }
            ChangeSceneNameServerRpc(m_sceneNames[currentSceneIndex]);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangeSceneNameServerRpc(string sceneName)
        {
            currentScene = sceneName;
            textElement.text = sceneName;
            ChangeSceneNameClientRpc(sceneName);
        }

        [ClientRpc]
        public void ChangeSceneNameClientRpc(string sceneName)
        {
            currentScene = sceneName;
            textElement.text = sceneName;
        }

        public void ActivateButton()
        {
            // Called for only the first player to join
            foreach (var ui in m_UiElementToActivate)
            {
                ui.SetActive(true);
            }
        }
    }
}
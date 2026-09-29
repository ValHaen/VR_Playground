using UnityEngine;

namespace XRMultiplayer
{
    public class ChangeSettingsButton : MonoBehaviour
    {
        [SerializeField] GameObject QuitButton;
        [SerializeField] GameObject BackToLobbyButton;

        // private bool IsShowingReset;
        // private bool IsShowingBackToLobby;

        // void Update()
        // {
        //     // if (IsShowingReset)
        //     // {
        //     //     if (ResetButton.activeSelf) return;
        //     //     ResetButton.SetActive(true);
        //     //     BackToLobbyButton.SetActive(false);
        //     // }
        //     // else if (IsShowingBackToLobby)
        //     // {
        //     //     if (BackToLobbyButton.activeSelf) return;
        //     //     ResetButton.SetActive(false);
        //     //     BackToLobbyButton.SetActive(true);
        //     // }
        // }

        // public void ShowReset()
        // {
        //     IsShowingReset = true;
        //     IsShowingBackToLobby = false;
        // }
        // public void ShowBackToLobby()
        // {
        //     IsShowingReset = false;
        //     IsShowingBackToLobby = true;
        // }

        public void ShowBackToLobbyButton()
        {
            Debug.Log("ShowBack");
            BackToLobbyButton.SetActive(true);
            QuitButton.SetActive(false);
        }

        public void ShowQuitButton()
        {
            Debug.Log("ShowQuit");
            BackToLobbyButton.SetActive(false);
            QuitButton.SetActive(true);
        }
    }
}
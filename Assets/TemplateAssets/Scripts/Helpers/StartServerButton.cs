using UnityEngine;

namespace XRMultiplayer
{
    public class StartServerButtonScript : MonoBehaviour
    {
        public void StartServerButtonClicked()
        {
            XRINetworkGameManager.Instance.StartServer();
            Destroy(this);
        }
    }
}
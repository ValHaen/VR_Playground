using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuitGame : MonoBehaviour
{
    public Text headerText; // UnityEngine.UI.Text
    private int pressCount = 0;

    private readonly string[] messages = {
        "Nahh no way you pressed the button",
        "Bro chill... its the quit button dont...",
        "Valentin will be pissed!",
        "Oh fuck"
    };

    public void ExitGame()
    {
        pressCount++;

        if (pressCount <= messages.Length)
        {
            if (headerText != null)
            {
                headerText.text = messages[pressCount - 1];
                Debug.Log("[QuitGame] Text geändert: " + headerText.text);
            }

            // Wenn wir beim letzten Text angekommen sind
            if (pressCount == messages.Length)
            {
                // Starte Verzögerung zum Beenden
                StartCoroutine(QuitAfterDelay(2f));
            }

            return;
        }
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
        Debug.Log("[QuitGame] Beenden in " + delay + " Sekunden...");
        yield return new WaitForSeconds(delay);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

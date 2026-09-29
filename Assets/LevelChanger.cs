using UnityEngine;
using UnityEngine.InputSystem; // WICHTIG: Erfordert das Input System Package

public class LevelChanger : MonoBehaviour
{
    public string mainMenuSceneName = "MenuScene";
    public SceneFader sceneFader;

    [Header("Pico Input Mapping")]
    // Hier erscheint im Inspector das Feld für den Button
    public InputActionReference menuButtonAction; 

    private void OnEnable() => menuButtonAction.action.Enable();
    private void OnDisable() => menuButtonAction.action.Disable();

    void Update()
    {
        // Prüft, ob der Pico-Button gerade gedrückt wurde
        if (menuButtonAction.action.triggered)
        {
            TriggerBackToMenu();
        }
    }

    public void TriggerBackToMenu()
    {
        if (sceneFader != null)
            sceneFader.FadeToScene(mainMenuSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
}
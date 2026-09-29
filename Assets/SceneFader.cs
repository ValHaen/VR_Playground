using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    public Renderer fadeQuadRenderer;
    public float fadeDuration = 1.0f;
    public string colorPropertyName = "_BaseColor"; 

    // Die Methode, die der MenuManager aufruft
    public IEnumerator PerformFade(float startAlpha, float endAlpha)
    {
        if (fadeQuadRenderer == null) yield break;

        float elapsed = 0f;
        Material mat = fadeQuadRenderer.material;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            Debug.Log("Aktueller Alpha-Wert: " + currentAlpha);
            
            if (mat.HasProperty(colorPropertyName))
            {
                Color c = mat.GetColor(colorPropertyName);
                c.a = currentAlpha;
                mat.SetColor(colorPropertyName, c);
            }
            yield return null;
        }
    }

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(ProcessTransition(sceneName));
    }

    private IEnumerator ProcessTransition(string sceneName)
    {
        yield return StartCoroutine(PerformFade(0, 1));
        SceneManager.LoadScene(sceneName);
    }
}
using UnityEngine;
using System.Collections;

public class MenuIntroController : MonoBehaviour
{
    [Header("Objects")]
    public Renderer logoRenderer;      
    public MenuManager menuManager;    

    [Header("Timings")]
    public float fadeDuration = 1.5f;  
    public float logoHoldTime = 1.5f;  
    public float popDuration = 0.35f;  

    private Material logoMat;
    private Vector3 originalLogoScale; // Speichert die Größe aus dem Editor

    void Start()
    {
        if (logoRenderer == null || menuManager == null)
        {
            Debug.LogError("MenuIntroController: LogoRenderer oder MenuManager nicht zugewiesen!");
            return;
        }

        // WICHTIG: Wir merken uns die Größe, die du im Editor eingestellt hast!
        originalLogoScale = logoRenderer.transform.localScale;

        logoMat = logoRenderer.material;
        SetAlpha(logoMat, 0f);             
        
        // Wir setzen die Scale auf 0, damit der Pop-Effekt funktioniert
        logoRenderer.transform.localScale = Vector3.zero;
        logoRenderer.gameObject.SetActive(false);

        if (menuManager.mainButtons != null) menuManager.mainButtons.SetActive(false);
        if (menuManager.titleText != null) menuManager.titleText.SetActive(false);
        if (menuManager.worldSelect != null) menuManager.worldSelect.SetActive(false);

        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        logoRenderer.gameObject.SetActive(true);
        
        // Wir starten gleichzeitig das Fading und das Aufploppen
        StartCoroutine(ScalePop(logoRenderer.transform, originalLogoScale));
        yield return FadeMaterial(logoMat, 0f, 1f, fadeDuration);

        yield return new WaitForSeconds(logoHoldTime);

        yield return FadeMaterial(logoMat, 1f, 0f, fadeDuration);
        logoRenderer.gameObject.SetActive(false);

        // Hier kannst du jetzt den MenuManager triggern, damit Buttons erscheinen
        menuManager.ShowMain(); 
    }

    IEnumerator ScalePop(Transform target, Vector3 targetScale)
    {
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, t / popDuration);
            // Wir lerpen von Null zur ORIGINALEN Größe
            target.localScale = Vector3.Lerp(Vector3.zero, targetScale, s);
            yield return null;
        }
        target.localScale = targetScale;
    }

    // ... (FadeMaterial und SetAlpha bleiben gleich wie in deinem Script)
    IEnumerator FadeMaterial(Material mat, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            SetAlpha(mat, a);
            yield return null;
        }
        SetAlpha(mat, to);
    }

    void SetAlpha(Material mat, float alpha)
    {
        Color c = mat.color;
        c.a = alpha;
        mat.color = c;
    }
}
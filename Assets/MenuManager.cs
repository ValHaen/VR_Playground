using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class WorldEntry
{
    public string worldName;
    public string sceneName;
}

public class MenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Image logoImage;
    public GameObject titleText;
    public GameObject mainButtons;
    public GameObject worldSelect;
    public GameObject extraImagesGroup;

    [Header("Atmosphere")]
    public Light menuSpotlight; 
    public MeshRenderer menuBox; 
    public float lightFadeDuration = 2.0f;
    public FMODUnity.StudioEventEmitter menuMusicEmitter;

    [Header("Intro Particles (New)")]
    public ParticleSystem introParticles; // Hier dein Particle System reinziehen

    [Header("Fader & Worlds")]
    public SceneFader sceneFader;
    public WorldEntry[] worlds;

    [Header("Animation Timings")]
    public float logoFadeDuration = 1.5f;
    public float logoHoldTime = 1.5f;
    public float popDuration = 0.35f;

    void Start()
    {
        // 1. Alles verstecken
        if (titleText) titleText.SetActive(false);
        if (mainButtons) mainButtons.SetActive(false);
        if (worldSelect) worldSelect.SetActive(false);
        if (extraImagesGroup) extraImagesGroup.SetActive(false);
        
        // 2. Licht & Partikel vorbereiten
        if (menuSpotlight) menuSpotlight.intensity = 0;
        if (menuBox) menuBox.material.DisableKeyword("_EMISSION");
        if (introParticles) introParticles.Stop(); // Sicherstellen, dass sie aus sind

        // 3. FMOD Musik
        if (menuMusicEmitter != null) menuMusicEmitter.Play();

        if (logoImage != null)
            StartCoroutine(LogoIntroSequence());
        else
            ShowMain();
    }

    private IEnumerator LogoIntroSequence()
    {
        // LOGO SEQUENZ (Fade In/Out)
        logoImage.gameObject.SetActive(true);
        Color c = logoImage.color;
        c.a = 0;
        logoImage.color = c;
        logoImage.transform.localScale = Vector3.zero;

        float t = 0;
        while (t < logoFadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0, 1, t / logoFadeDuration);
            logoImage.color = c;
            if (t < popDuration)
                logoImage.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t / popDuration);
            yield return null;
        }

        yield return new WaitForSeconds(logoHoldTime);

        t = 0;
        while (t < logoFadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1, 0, t / logoFadeDuration);
            logoImage.color = c;
            yield return null;
        }
        logoImage.gameObject.SetActive(false);

        // ÜBERGANG ZUM MENÜ
        if (sceneFader != null)
        {
            yield return StartCoroutine(sceneFader.PerformFade(0, 1));
            
            StartCoroutine(FadeInMenuAtmosphere());
            ShowMainImmediate();
            if (introParticles) introParticles.Play(); // PARTIKEL STARTEN

            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(sceneFader.PerformFade(1, 0));
        }
        else
        {
            StartCoroutine(FadeInMenuAtmosphere());
            ShowMain();
            if (introParticles) introParticles.Play(); // PARTIKEL STARTEN
        }
    }

    private IEnumerator FadeInMenuAtmosphere()
    {
        float t = 0;
        float targetIntensity = 2.0f; 
        
        while (t < lightFadeDuration)
        {
            t += Time.deltaTime;
            float lerp = t / lightFadeDuration;
            
            if (menuSpotlight) menuSpotlight.intensity = Mathf.Lerp(0, targetIntensity, lerp);
            
            if (menuBox) {
                menuBox.material.EnableKeyword("_EMISSION");
                Color emissionColor = Color.white * Mathf.Lerp(0, 1, lerp);
                menuBox.material.SetColor("_EmissionColor", emissionColor);
            }
            yield return null;
        }
    }

    // ... (ShowMain, ShowWorlds, ScalePop, QuitGame bleiben identisch)
    
    private void ShowMainImmediate()
    {
        if (titleText) titleText.SetActive(true);
        if (mainButtons) mainButtons.SetActive(true);
        if (worldSelect) worldSelect.SetActive(false);
        if (extraImagesGroup) extraImagesGroup.SetActive(false);
        if (titleText) titleText.transform.localScale = Vector3.one;
        if (mainButtons) mainButtons.transform.localScale = Vector3.one;
    }

    public void ShowMain()
    {
        if (titleText) titleText.SetActive(true);
        if (mainButtons) mainButtons.SetActive(true);
        if (worldSelect) worldSelect.SetActive(false);
        if (extraImagesGroup) extraImagesGroup.SetActive(false);
        if (titleText) StartCoroutine(ScalePop(titleText.transform));
        if (mainButtons) StartCoroutine(ScalePop(mainButtons.transform));
    }

    public void ShowWorlds()
    {
        if (mainButtons) mainButtons.SetActive(false);
        if (worldSelect) worldSelect.SetActive(true);
        if (extraImagesGroup) 
        {
            extraImagesGroup.SetActive(true);
            StartCoroutine(ScalePop(extraImagesGroup.transform));
        }
        if (worldSelect) StartCoroutine(ScalePop(worldSelect.transform));
    }

    private IEnumerator ScalePop(Transform target)
    {
        float elapsed = 0f;
        target.localScale = Vector3.zero;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, Mathf.SmoothStep(0, 1, elapsed / popDuration));
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    public void LoadWorldByIndex(int index)
    {
        if (extraImagesGroup) extraImagesGroup.SetActive(false);
        if (index >= 0 && index < worlds.Length && sceneFader != null)
            sceneFader.FadeToScene(worlds[index].sceneName);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void OnDestroy()
    {
        if (menuMusicEmitter != null) menuMusicEmitter.Stop();
    }
}
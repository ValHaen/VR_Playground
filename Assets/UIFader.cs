using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIFader : MonoBehaviour
{
    public float fadeDuration = 0.5f;

    // Fade In für Image
    public IEnumerator FadeIn(Image img)
    {
        float t = 0f;
        Color c = img.color;
        c.a = 0f;
        img.color = c;
        img.gameObject.SetActive(true);

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            img.color = c;
            yield return null;
        }
        c.a = 1f;
        img.color = c;
    }

    // Fade Out für Image
    public IEnumerator FadeOut(Image img)
    {
        float t = 0f;
        Color c = img.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
            img.color = c;
            yield return null;
        }
        c.a = 0f;
        img.color = c;
        img.gameObject.SetActive(false);
    }

    // Optional: CanvasGroup-Version
    public IEnumerator FadeIn(CanvasGroup cg)
    {
        float t = 0f;
        cg.alpha = 0f;
        cg.gameObject.SetActive(true);

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    public IEnumerator FadeOut(CanvasGroup cg)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }
        cg.alpha = 0f;
        cg.gameObject.SetActive(false);
    }
}

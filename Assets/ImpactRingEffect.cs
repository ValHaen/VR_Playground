using UnityEngine;

public class ImpactRingEffect : MonoBehaviour
{
    public float duration = 0.5f;
    public float startScale = 0.1f;
    public float endScale = 1.5f;
    public float startAlpha = 0.7f;

    private Material mat;
    private float timer = 0;

    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
        transform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        // Scale Animation
        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = Vector3.one * scale;

        // Fade out
        Color c = mat.color;
        c.a = Mathf.Lerp(startAlpha, 0, t);
        mat.color = c;

        if (t >= 1f)
            Destroy(gameObject);
    }
}
using UnityEngine;
using FMODUnity;

public class FMODParameterByPlayerX : MonoBehaviour
{
    [Header("FMOD")]
    public StudioEventEmitter emitter;
    public string parameterName = "Volume X"; // Name deines FMOD Parameters

    [Header("Bezugspunkt (z. B. XR Origin oder Spieler)")]
    public Transform playerTransform;

    [Header("X-Achse Zuordnung")]
    public float minX = -5f;  // X = -5 → FMOD-Parameter = 0
    public float maxX = 5f;   // X = +5 → FMOD-Parameter = 1

    void Update()
    {
        if (emitter == null || playerTransform == null || !emitter.IsPlaying()) return;

        float x = playerTransform.position.x;

        // Mapping von X-Wert auf 0–1
        float value = Mathf.InverseLerp(minX, maxX, x);
        value = Mathf.Clamp01(value);

        emitter.SetParameter(parameterName, value);

        Debug.Log($"Player X: {x} → {parameterName}: {value}");
    }
}

using UnityEngine;
using FMODUnity;

public class FMODPlayOnEvent : MonoBehaviour
{
    public StudioEventEmitter emitter;

    private bool isPlaying = false;

    public void ToggleSound()
    {
        if (emitter == null) return;

        if (!isPlaying)
        {
            emitter.Play();
            isPlaying = true;
        }
        else
        {
            emitter.Stop();
            isPlaying = false;
        }
    }
}
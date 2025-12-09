using UnityEngine;

public class SoundBus3D : MonoBehaviour
{
    public static SoundBus3D Instance { get; private set; }

    public delegate void SoundEventHandler(Vector3 position, float radius, float intensity, GameObject source);
    public event SoundEventHandler OnSoundHeard;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[SoundBus3D] Ready");
    }

    public void EmitSound(Vector3 position, float radius, float intensity, GameObject source)
    {
        string sourceName = source != null ? source.name : "null";
        Debug.Log("[SoundBus3D] Sound at " + position + " radius=" + radius + " intensity=" + intensity + " from=" + sourceName);

        if (OnSoundHeard != null)
        {
            OnSoundHeard(position, radius, intensity, source);
        }
    }
}
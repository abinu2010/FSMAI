using UnityEngine;

public class SoundBus3D : MonoBehaviour
{
    public static SoundBus3D Instance { get; private set; }

    public delegate void SoundEventHandler(Vector3 position, float radius, float intensity, GameObject source);
    public event SoundEventHandler OnSoundEmitted;

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
        Debug.Log("[SoundBus3D] Sound at " + position + " radius=" + radius + " intensity=" + intensity);
        var handler = OnSoundEmitted;
        if (handler != null)
        {
            handler(position, radius, intensity, source);
        }
    }
}

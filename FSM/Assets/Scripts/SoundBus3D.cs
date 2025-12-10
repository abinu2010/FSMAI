using UnityEngine;
using System.Collections.Generic;

public class SoundBus3D : MonoBehaviour
{
    public static SoundBus3D Instance { get; private set; }

    public delegate void SoundEventHandler(Vector3 position, float radius, float intensity, GameObject source);
    public event SoundEventHandler OnSoundEmitted;
    public bool showSoundGizmos = true;
    public float gizmoDisplayTime = 1f;
    private List<SoundGizmoData> recentSounds = new List<SoundGizmoData>();
    struct SoundGizmoData
    {
        public Vector3 position;
        public float radius;
        public float time;
    }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Update()
    {
        recentSounds.RemoveAll(s => Time.time - s.time > gizmoDisplayTime);
    }
    public void EmitSound(Vector3 position, float radius, float intensity, GameObject source)
    {
        string sourceName = source != null ? source.name : "Unknown";
        bool isPlayer = source != null && source.CompareTag("Player");
        if (showSoundGizmos)
        {
            recentSounds.Add(new SoundGizmoData
            {
                position = position,
                radius = radius,
                time = Time.time
            });
        }
        var handler = OnSoundEmitted;
        if (handler != null)
        {
            int listenerCount = handler.GetInvocationList().Length;
            handler(position, radius, intensity, source);
        }
    }
    public int GetListenerCount()
    {
        var handler = OnSoundEmitted;
        return handler != null ? handler.GetInvocationList().Length : 0;
    }
    void OnDrawGizmos()
    {
        if (!showSoundGizmos || !Application.isPlaying) return;

        foreach (var sound in recentSounds)
        {
            float age = Time.time - sound.time;
            float alpha = 1f - (age / gizmoDisplayTime);
            Gizmos.color = new Color(1f, 0.5f, 0f, alpha * 0.3f);
            Gizmos.DrawWireSphere(sound.position, sound.radius);
            Gizmos.color = new Color(1f, 0.5f, 0f, alpha * 0.5f);
            Gizmos.DrawSphere(sound.position, 0.3f);
        }
    }
}
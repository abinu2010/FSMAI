using UnityEngine;
using System.Collections.Generic;

public class SoundBus3D : MonoBehaviour
{
    public static SoundBus3D Instance { get; private set; }

    public delegate void SoundEventHandler(Vector3 position, float radius, float intensity, GameObject source);
    public event SoundEventHandler OnSoundEmitted;

    [Header("Debug")]
    public bool showSoundGizmos = true;
    public float gizmoDisplayTime = 1f;

    // Track recent sounds for visualization
    private List<SoundGizmoData> recentSounds = new List<SoundGizmoData>();

    struct SoundGizmoData
    {
        public Vector3 position;
        public float radius;
        public float time;
    }

    void Awake()
    {
        // Singleton setup - MUST happen in Awake before other scripts Start
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SoundBus3D] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes
        Debug.Log("[SoundBus3D] Instance created and ready");
    }

    void Update()
    {
        // Clean up old gizmo data
        recentSounds.RemoveAll(s => Time.time - s.time > gizmoDisplayTime);
    }

    public void EmitSound(Vector3 position, float radius, float intensity, GameObject source)
    {
        string sourceName = source != null ? source.name : "Unknown";
        bool isPlayer = source != null && source.CompareTag("Player");

        Debug.Log($"[SoundBus3D] SOUND EMITTED at {position} radius={radius:F1} intensity={intensity:F1} source={sourceName} isPlayer={isPlayer}");

        // Store for gizmo
        if (showSoundGizmos)
        {
            recentSounds.Add(new SoundGizmoData
            {
                position = position,
                radius = radius,
                time = Time.time
            });
        }

        // Fire event to all listeners
        var handler = OnSoundEmitted;
        if (handler != null)
        {
            int listenerCount = handler.GetInvocationList().Length;
            Debug.Log($"[SoundBus3D] Notifying {listenerCount} listeners");
            handler(position, radius, intensity, source);
        }
        else
        {
            Debug.LogWarning("[SoundBus3D] No listeners subscribed to OnSoundEmitted!");
        }
    }

    /// <summary>
    /// Check how many guards are listening
    /// </summary>
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
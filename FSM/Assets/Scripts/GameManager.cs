using UnityEngine;

/// <summary>
/// Ensures SoundBus3D and AlertBus3D are initialized before guards.
/// Attach this to a GameObject that loads FIRST (use Script Execution Order or place in scene).
/// </summary>
public class GameManager3D : MonoBehaviour
{
    [Header("Prefabs (Optional - will auto-create if needed)")]
    public SoundBus3D soundBusPrefab;
    public AlertBus3D alertBusPrefab;

    [Header("Debug")]
    public bool showDebugInfo = true;

    void Awake()
    {
        // Ensure SoundBus exists
        if (SoundBus3D.Instance == null)
        {
            if (soundBusPrefab != null)
            {
                Instantiate(soundBusPrefab);
            }
            else
            {
                GameObject soundBusObj = new GameObject("SoundBus3D");
                soundBusObj.AddComponent<SoundBus3D>();
            }
            Debug.Log("[GameManager3D] Created SoundBus3D");
        }

        // Ensure AlertBus exists
        if (AlertBus3D.Instance == null)
        {
            if (alertBusPrefab != null)
            {
                Instantiate(alertBusPrefab);
            }
            else
            {
                GameObject alertBusObj = new GameObject("AlertBus3D");
                alertBusObj.AddComponent<AlertBus3D>();
            }
            Debug.Log("[GameManager3D] Created AlertBus3D");
        }

        Debug.Log("[GameManager3D] Initialization complete");
    }

    void Start()
    {
        if (showDebugInfo)
        {
            LogSystemStatus();
        }
    }

    void LogSystemStatus()
    {
        Debug.Log("=== GAME SYSTEMS STATUS ===");
        Debug.Log($"SoundBus3D: {(SoundBus3D.Instance != null ? "OK" : "MISSING")}");
        Debug.Log($"AlertBus3D: {(AlertBus3D.Instance != null ? "OK" : "MISSING")}");

        if (SoundBus3D.Instance != null)
        {
            Debug.Log($"  - Sound listeners: {SoundBus3D.Instance.GetListenerCount()}");
        }
        if (AlertBus3D.Instance != null)
        {
            Debug.Log($"  - Alert listeners: {AlertBus3D.Instance.GetListenerCount()}");
        }

        GuardAI3D[] guards = FindObjectsOfType<GuardAI3D>();
        Debug.Log($"Guards in scene: {guards.Length}");
        foreach (var guard in guards)
        {
            Debug.Log($"  - {guard.name}");
        }

        Debug.Log($"Active scent nodes: {ScentNode3D.AllNodes.Count}");
        Debug.Log("===========================");
    }

    // Call this to check system status at runtime
    [ContextMenu("Log System Status")]
    public void DebugLogStatus()
    {
        LogSystemStatus();
    }
}
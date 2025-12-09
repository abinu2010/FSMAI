using UnityEngine;
using System.Collections.Generic;

public enum AlertLevel
{
    Low,        // Suspicious sound/smell
    Medium,     // Found evidence (bait, strong smell)
    High,       // Player spotted or strong evidence
    Critical    // Player engaged in combat or confirmed sighting
}

public class AlertBus3D : MonoBehaviour
{
    public static AlertBus3D Instance { get; private set; }

    // Alert event with level
    public delegate void AlertEventHandler(Vector3 position, GameObject sourceGuard, AlertLevel level);
    public event AlertEventHandler OnAlertRaised;

    // Player position update event (for guards to track player during combat)
    public delegate void PlayerPositionHandler(Vector3 position);
    public event PlayerPositionHandler OnPlayerPositionUpdated;

    [Header("Global Alert")]
    public AlertLevel CurrentGlobalAlert { get; private set; } = AlertLevel.Low;
    public float globalAlertDecayRate = 0.08f; // How fast alert level decreases

    [Header("Player Tracking")]
    public Vector3 LastKnownPlayerPosition { get; private set; }
    public float TimeSincePlayerSeen { get; private set; }

    [Header("Debug")]
    public bool showAlertGizmos = true;

    float globalAlertValue = 0f;

    // Track recent alerts for visualization
    private List<AlertGizmoData> recentAlerts = new List<AlertGizmoData>();

    struct AlertGizmoData
    {
        public Vector3 position;
        public AlertLevel level;
        public float time;
    }

    void Awake()
    {
        // Singleton setup - MUST happen in Awake before other scripts Start
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[AlertBus3D] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[AlertBus3D] Instance created and ready");
    }

    void Update()
    {
        // Decay global alert over time
        globalAlertValue = Mathf.Max(0f, globalAlertValue - globalAlertDecayRate * Time.deltaTime);

        // Track time since player was last seen
        TimeSincePlayerSeen += Time.deltaTime;

        // Update alert level based on value
        AlertLevel newLevel = AlertLevel.Low;
        if (globalAlertValue > 3f)
            newLevel = AlertLevel.Critical;
        else if (globalAlertValue > 2f)
            newLevel = AlertLevel.High;
        else if (globalAlertValue > 1f)
            newLevel = AlertLevel.Medium;

        if (newLevel != CurrentGlobalAlert)
        {
            CurrentGlobalAlert = newLevel;
            Debug.Log($"[AlertBus3D] Global alert changed to: {CurrentGlobalAlert} (value: {globalAlertValue:F2})");
        }

        // Clean up old gizmo data
        recentAlerts.RemoveAll(a => Time.time - a.time > 3f);
    }

    /// <summary>
    /// Update all guards about the player's current position (called during combat)
    /// </summary>
    public void UpdatePlayerPosition(Vector3 position)
    {
        LastKnownPlayerPosition = position;
        TimeSincePlayerSeen = 0f;

        // Notify all listening guards
        OnPlayerPositionUpdated?.Invoke(position);
    }

    public void RaiseAlert(Vector3 position, GameObject sourceGuard, AlertLevel level = AlertLevel.Medium)
    {
        // Update last known player position for high-level alerts
        if (level >= AlertLevel.High)
        {
            LastKnownPlayerPosition = position;
            TimeSincePlayerSeen = 0f;
        }

        // Increase global alert value based on level
        float alertIncrease = 0f;
        switch (level)
        {
            case AlertLevel.Low:
                alertIncrease = 0.5f;
                break;
            case AlertLevel.Medium:
                alertIncrease = 1.5f;
                break;
            case AlertLevel.High:
                alertIncrease = 2.5f;
                break;
            case AlertLevel.Critical:
                alertIncrease = 4f;
                break;
        }

        globalAlertValue = Mathf.Min(globalAlertValue + alertIncrease, 5f);

        string guardName = sourceGuard != null ? sourceGuard.name : "Unknown";
        Debug.Log($"[AlertBus3D] ALERT RAISED by {guardName} at {position} - Level: {level} (Global: {globalAlertValue:F2})");

        // Store for gizmo
        if (showAlertGizmos)
        {
            recentAlerts.Add(new AlertGizmoData
            {
                position = position,
                level = level,
                time = Time.time
            });
        }

        // Fire event to all listeners
        var handler = OnAlertRaised;
        if (handler != null)
        {
            int listenerCount = handler.GetInvocationList().Length;
            Debug.Log($"[AlertBus3D] Notifying {listenerCount} guards of {level} alert");
            handler(position, sourceGuard, level);
        }
        else
        {
            Debug.LogWarning("[AlertBus3D] No listeners subscribed to OnAlertRaised!");
        }
    }

    /// <summary>
    /// Check how many guards are listening
    /// </summary>
    public int GetListenerCount()
    {
        var handler = OnAlertRaised;
        return handler != null ? handler.GetInvocationList().Length : 0;
    }

    /// <summary>
    /// Manually set the global alert to a specific level (for scripted events)
    /// </summary>
    public void SetGlobalAlert(AlertLevel level)
    {
        switch (level)
        {
            case AlertLevel.Low:
                globalAlertValue = 0.5f;
                break;
            case AlertLevel.Medium:
                globalAlertValue = 1.5f;
                break;
            case AlertLevel.High:
                globalAlertValue = 2.5f;
                break;
            case AlertLevel.Critical:
                globalAlertValue = 4f;
                break;
        }
        Debug.Log($"[AlertBus3D] Global alert manually set to {level}");
    }

    /// <summary>
    /// Clear all alerts (e.g., when player escapes or dies)
    /// </summary>
    public void ClearAllAlerts()
    {
        globalAlertValue = 0f;
        CurrentGlobalAlert = AlertLevel.Low;
        TimeSincePlayerSeen = 999f;
        Debug.Log("[AlertBus3D] All alerts cleared");
    }

    void OnDrawGizmos()
    {
        if (!showAlertGizmos || !Application.isPlaying) return;

        foreach (var alert in recentAlerts)
        {
            float age = Time.time - alert.time;
            float alpha = 1f - (age / 3f);

            // Color based on alert level
            Color col = Color.white;
            switch (alert.level)
            {
                case AlertLevel.Low:
                    col = Color.yellow;
                    break;
                case AlertLevel.Medium:
                    col = new Color(1f, 0.5f, 0f); // Orange
                    break;
                case AlertLevel.High:
                    col = new Color(1f, 0.2f, 0f); // Red-orange
                    break;
                case AlertLevel.Critical:
                    col = Color.red;
                    break;
            }

            col.a = alpha * 0.5f;
            Gizmos.color = col;
            Gizmos.DrawWireSphere(alert.position, 2f + (int)alert.level);
        }

        // Draw last known player position
        if (TimeSincePlayerSeen < 10f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
            Gizmos.DrawWireCube(LastKnownPlayerPosition, Vector3.one * 0.5f);
        }
    }
}
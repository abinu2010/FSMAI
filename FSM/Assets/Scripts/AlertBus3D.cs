using UnityEngine;

public enum AlertLevel
{
    Low,        // Suspicious sound/smell
    Medium,     // Found evidence
    High,       // Player spotted
    Critical    // Player engaged in combat
}

public class AlertBus3D : MonoBehaviour
{
    public static AlertBus3D Instance { get; private set; }

    // Different alert types with different urgency
    public delegate void AlertEventHandler(Vector3 position, GameObject sourceGuard, AlertLevel level);
    public event AlertEventHandler OnAlertRaised;

    // Global alert state
    public AlertLevel CurrentGlobalAlert { get; private set; } = AlertLevel.Low;
    public float globalAlertDecayRate = 0.1f; // How fast alert level decreases
    float globalAlertValue = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[AlertBus3D] Ready");
    }

    void Update()
    {
        // Decay global alert over time
        globalAlertValue = Mathf.Max(0f, globalAlertValue - globalAlertDecayRate * Time.deltaTime);

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
            Debug.Log($"[AlertBus3D] Global alert level: {CurrentGlobalAlert}");
        }
    }

    public void RaiseAlert(Vector3 position, GameObject sourceGuard, AlertLevel level = AlertLevel.Medium)
    {
        // Increase global alert value
        switch (level)
        {
            case AlertLevel.Low:
                globalAlertValue += 0.5f;
                break;
            case AlertLevel.Medium:
                globalAlertValue += 1.5f;
                break;
            case AlertLevel.High:
                globalAlertValue += 2.5f;
                break;
            case AlertLevel.Critical:
                globalAlertValue += 4f;
                break;
        }

        globalAlertValue = Mathf.Min(globalAlertValue, 5f); // Cap at 5

        Debug.Log($"[AlertBus3D] Alert from {sourceGuard.name} at {position} - Level: {level} (Global: {globalAlertValue:F2})");

        var handler = OnAlertRaised;
        if (handler != null)
        {
            handler(position, sourceGuard, level);
        }
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
    }

    /// <summary>
    /// Clear all alerts (e.g., when player escapes or dies)
    /// </summary>
    public void ClearAllAlerts()
    {
        globalAlertValue = 0f;
        CurrentGlobalAlert = AlertLevel.Low;
        Debug.Log("[AlertBus3D] All alerts cleared");
    }
}

// ==================== UPDATE GuardAI3D.cs to use alert levels ====================
// In your GuardAI3D.cs, update the OnAlertReceived method signature:
/*
void OnAlertReceived(Vector3 position, GameObject sourceGuard, AlertLevel level)
{
    if (sourceGuard == gameObject)
    {
        return;
    }

    investigateTarget = position;
    
    // React more urgently to higher alert levels
    switch (level)
    {
        case AlertLevel.Low:
            currentSuspicionLevel += 0.5f;
            break;
        case AlertLevel.Medium:
            currentSuspicionLevel += 1f;
            break;
        case AlertLevel.High:
            currentSuspicionLevel += 2f;
            break;
        case AlertLevel.Critical:
            currentSuspicionLevel += 3f;
            hasDirectEvidence = true; // Treat as if we saw something
            break;
    }

    Debug.Log($"[GuardAI3D] {name} received {level} alert from {sourceGuard.name}");

    // Only respond if not in high-priority state
    if (currentState != Guard3DState.Chase && currentState != Guard3DState.Attack)
    {
        if (level >= AlertLevel.High)
        {
            SetState(Guard3DState.Search); // Skip to search for high alerts
        }
        else
        {
            SetState(Guard3DState.Assist);
        }
    }
}
*/
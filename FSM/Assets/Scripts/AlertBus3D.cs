using UnityEngine;
using System.Collections.Generic;

public enum AlertLevel
{
    Low,        // Suspicious sound/smell
    Medium,     // Found evidence (bait, strong smell)
    High,       // Player spotted or Bait
    Critical    // Player engaged in combat or confirmed sight
}

public class AlertBus3D : MonoBehaviour
{
    public static AlertBus3D Instance { get; private set; }
    public delegate void AlertEventHandler(Vector3 position, GameObject sourceGuard, AlertLevel level);
    public event AlertEventHandler OnAlertRaised;
    public delegate void PlayerPositionHandler(Vector3 position);
    public event PlayerPositionHandler OnPlayerPositionUpdated;
    public AlertLevel CurrentGlobalAlert { get; private set; } = AlertLevel.Low;
    public float globalAlertDecayRate = 0.08f; // How fast alert level decreases
    public Vector3 LastKnownPlayerPosition { get; private set; }
    public float TimeSincePlayerSeen { get; private set; }
    public bool showAlertGizmos = true;
    float globalAlertValue = 0f;
    private List<AlertGizmoData> recentAlerts = new List<AlertGizmoData>();
    struct AlertGizmoData
    {
        public Vector3 position;
        public AlertLevel level;
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
        }
        recentAlerts.RemoveAll(a => Time.time - a.time > 3f);
    }
    public void UpdatePlayerPosition(Vector3 position)
    {
        LastKnownPlayerPosition = position;
        TimeSincePlayerSeen = 0f;
        OnPlayerPositionUpdated?.Invoke(position);
    }
    public void RaiseAlert(Vector3 position, GameObject sourceGuard, AlertLevel level = AlertLevel.Medium)
    {
        if (level >= AlertLevel.High)
        {
            LastKnownPlayerPosition = position;
            TimeSincePlayerSeen = 0f;
        }
        float alertIncrease = 0f;
        switch (level)
        {
            case AlertLevel.Low: alertIncrease = 0.5f;break;
            case AlertLevel.Medium:alertIncrease = 1.5f;break;
            case AlertLevel.High:alertIncrease = 2.5f;break;
            case AlertLevel.Critical:alertIncrease = 4f;break;
        }

        globalAlertValue = Mathf.Min(globalAlertValue + alertIncrease, 5f);

        string guardName = sourceGuard != null ? sourceGuard.name : "Unknown";
        if (showAlertGizmos)
        {
            recentAlerts.Add(new AlertGizmoData
            {
                position = position,
                level = level,
                time = Time.time
            });
        }
        var handler = OnAlertRaised;
        if (handler != null)
        {
            int listenerCount = handler.GetInvocationList().Length;
            handler(position, sourceGuard, level);
        }
    }
    public int GetListenerCount()
    {
        var handler = OnAlertRaised;
        return handler != null ? handler.GetInvocationList().Length : 0;
    }
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
     public void ClearAllAlerts()
    {
        globalAlertValue = 0f;
        CurrentGlobalAlert = AlertLevel.Low;
        TimeSincePlayerSeen = 999f;
    }
    void OnDrawGizmos()
    {
        if (!showAlertGizmos || !Application.isPlaying) return;

        foreach (var alert in recentAlerts)
        {
            float age = Time.time - alert.time;
            float alpha = 1f - (age / 3f);

            Color col = Color.white;
            switch (alert.level)
            {
                case AlertLevel.Low:
                    col = Color.yellow;
                    break;
                case AlertLevel.Medium:
                    col = new Color(1f, 0.5f, 0f);
                    break;
                case AlertLevel.High:
                    col = new Color(1f, 0.2f, 0f); 
                    break;
                case AlertLevel.Critical:
                    col = Color.red;
                    break;
            }
            col.a = alpha * 0.5f;
            Gizmos.color = col;
            Gizmos.DrawWireSphere(alert.position, 2f + (int)alert.level);
        }
        if (TimeSincePlayerSeen < 10f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
            Gizmos.DrawWireCube(LastKnownPlayerPosition, Vector3.one * 0.5f);
        }
    }
}
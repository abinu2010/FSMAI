using UnityEngine;

public enum AlertLevel
{
    Unknown,
    Low,
    Medium,
    High,
    Critical
}

public class AlertBus3D : MonoBehaviour
{
    public static AlertBus3D Instance { get; private set; }

    public delegate void AlertEventHandler(Vector3 position, AlertLevel level, GameObject sourceGuard);
    public event AlertEventHandler OnAlertRaised;

    public AlertLevel CurrentGlobalAlert { get; private set; } = AlertLevel.Low;
    public float globalAlertDecayRate = 0.1f;
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
        globalAlertValue = Mathf.Max(0f, globalAlertValue - globalAlertDecayRate * Time.deltaTime);

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
            Debug.Log("[AlertBus3D] Global alert level: " + CurrentGlobalAlert);
        }
    }

    public void RaiseAlert(Vector3 position, AlertLevel level, GameObject sourceGuard)
    {
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

        globalAlertValue = Mathf.Min(globalAlertValue, 5f);

        Debug.Log("[AlertBus3D] Alert from " + sourceGuard.name + " at " + position + " - Level: " + level + " (Global: " + globalAlertValue.ToString("F2") + ")");

        var handler = OnAlertRaised;
        if (handler != null)
        {
            handler(position, level, sourceGuard);
        }
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
        Debug.Log("[AlertBus3D] All alerts cleared");
    }
}
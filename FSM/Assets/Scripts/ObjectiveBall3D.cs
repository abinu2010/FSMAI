using UnityEngine;

public class ObjectiveBall3D : MonoBehaviour
{
    [Header("Debug")]
    public bool logEvents = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (logEvents)
        {
            Debug.Log("[ObjectiveBall3D] Player collected the ball");
        }

        // Optional: clear alerts
        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.ClearAllAlerts();
        }

        // TODO for you: trigger win UI or load next scene
        // For now just destroy the ball
        Destroy(gameObject);
    }
}

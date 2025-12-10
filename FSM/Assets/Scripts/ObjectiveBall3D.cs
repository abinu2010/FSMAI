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

        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.ClearAllAlerts();
        }

        if (AudioManager3D.Instance != null)
        {
            AudioManager3D.Instance.PlayObjectivePickup();
        }

        if (PlayerHealthUI3D.Instance != null)
        {
            PlayerHealthUI3D.Instance.ShowWin();
        }

        Destroy(gameObject);
    }
}

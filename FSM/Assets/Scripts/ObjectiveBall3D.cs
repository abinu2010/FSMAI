using UnityEngine;

public class ObjectiveBall3D : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
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

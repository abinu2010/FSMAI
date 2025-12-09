using UnityEngine;

public class BaitThrower3D : MonoBehaviour
{
    public Bait3D baitPrefab;
    public Transform throwOrigin;
    public float throwForce = 10f;
    public float throwCooldown = 2f;

    float cooldownTimer;

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(1) && cooldownTimer <= 0f)
        {
            ThrowBait();
        }
    }

    void ThrowBait()
    {
        if (baitPrefab == null || throwOrigin == null)
        {
            Debug.LogWarning("[BaitThrower3D] Missing prefab or origin");
            return;
        }

        Bait3D bait = Instantiate(baitPrefab, throwOrigin.position, throwOrigin.rotation);
        Rigidbody rb = bait.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(throwOrigin.forward * throwForce, ForceMode.VelocityChange);
        }

        Debug.Log("[BaitThrower3D] Threw bait");
        cooldownTimer = throwCooldown;
    }
}

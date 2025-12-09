using UnityEngine;

public class BaitThrower3D : MonoBehaviour
{
    public Bait3D baitPrefab;
    public Transform throwOrigin;
    public Transform cameraTransform; // ADD: Reference to camera for aiming
    public float throwForce = 15f; // INCREASED: Was 10, too weak
    public float throwCooldown = 2f;
    public float arcAngle = 15f; // ADD: Slight upward arc for better throw

    [Header("Debug")]
    public bool showThrowPreview = true;

    float cooldownTimer;

    void Start()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (throwOrigin == null)
        {
            throwOrigin = transform;
            Debug.LogWarning("[BaitThrower3D] No throw origin assigned, using transform");
        }

        Debug.Log("[BaitThrower3D] Ready");
    }

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
        if (baitPrefab == null)
        {
            Debug.LogWarning("[BaitThrower3D] Missing bait prefab!");
            return;
        }

        // Calculate throw direction based on camera look direction
        Vector3 throwDirection;
        if (cameraTransform != null)
        {
            // Use camera forward with slight upward arc
            throwDirection = cameraTransform.forward;
            throwDirection = Quaternion.AngleAxis(-arcAngle, cameraTransform.right) * throwDirection;
        }
        else
        {
            // Fallback to player forward
            throwDirection = transform.forward + Vector3.up * 0.3f;
        }
        throwDirection.Normalize();

        // Spawn position
        Vector3 spawnPos = throwOrigin != null ? throwOrigin.position : transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;

        // Instantiate bait
        Bait3D bait = Instantiate(baitPrefab, spawnPos, Quaternion.identity);

        // Ensure it has a Rigidbody
        Rigidbody rb = bait.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bait.gameObject.AddComponent<Rigidbody>();
            Debug.Log("[BaitThrower3D] Added Rigidbody to bait");
        }

        // Apply throw force
        rb.linearVelocity = Vector3.zero; // Reset any existing velocity
        rb.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);

        Debug.Log($"[BaitThrower3D] Threw bait in direction {throwDirection} with force {throwForce}");
        cooldownTimer = throwCooldown;
    }

    void OnDrawGizmos()
    {
        if (!showThrowPreview || !Application.isPlaying) return;

        if (cameraTransform != null && throwOrigin != null)
        {
            Vector3 throwDir = cameraTransform.forward;
            throwDir = Quaternion.AngleAxis(-arcAngle, cameraTransform.right) * throwDir;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(throwOrigin.position, throwDir * 5f);
        }
    }
}
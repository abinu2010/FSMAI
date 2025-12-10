using UnityEngine;

public class BaitThrower3D : MonoBehaviour
{
    public Bait3D baitPrefab;
    public Transform throwOrigin;
    public Transform cameraTransform; 
    public float throwForce = 15f;
    public float throwCooldown = 2f;
    public float arcAngle = 15f; 
    public bool showThrowPreview = true;
    float cooldownTimer;
    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (throwOrigin == null)
        {
            throwOrigin = transform;
        }
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
        Vector3 throwDirection;
        if (cameraTransform != null)
        {
            throwDirection = cameraTransform.forward;
            throwDirection = Quaternion.AngleAxis(-arcAngle, cameraTransform.right) * throwDirection;
        }
        else
        {
            throwDirection = transform.forward + Vector3.up * 0.3f;
        }
        throwDirection.Normalize();
        Vector3 spawnPos = throwOrigin != null ? throwOrigin.position : transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
        Bait3D bait = Instantiate(baitPrefab, spawnPos, Quaternion.identity);
        Rigidbody rb = bait.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bait.gameObject.AddComponent<Rigidbody>();
        }
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);
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
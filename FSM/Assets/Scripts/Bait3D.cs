using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bait3D : MonoBehaviour
{
    public float soundRadius = 12f;
    public float soundIntensity = 1.5f;
    public float landingSoundDelay = 0.1f;
    public float smellStrength = 2f;
    public ScentNode3D scentNodePrefab;
    public float lifetime = 30f;
    public float activationDelay = 0.2f;
    public float groundCheckDelay = 0.5f;
    public bool showDebugRadius = true;
    public bool showDebugLogs = true;
    bool activated;
    float spawnTime;
    Rigidbody rb;
    bool hasLoggedMissingScentPrefab;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = true;
        }
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.2f;
            col = sphere;
        }
        col.isTrigger = false;
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            Physics.IgnoreLayerCollision(gameObject.layer, playerLayer, true);
        }
    }
    void Start()
    {
        InvokeRepeating(nameof(CheckIfGrounded), groundCheckDelay, 0.2f);
    }
    void Update()
    {
        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (!activated && Time.time - spawnTime > activationDelay && rb != null && !rb.isKinematic)
        {
            if (rb.linearVelocity.magnitude < 0.1f)
            {
                ActivateBait("velocity stopped");
            }
        }
    }
    void CheckIfGrounded()
    {
        if (activated) return;
        if (Time.time - spawnTime < activationDelay) return;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 0.3f))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                ActivateBait($"raycast hit {hit.collider.name}");
            }
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Bait3D] OnCollisionEnter with {collision.gameObject.name} tag={collision.gameObject.tag}");
        }

        ActivateBait($"collision with {collision.gameObject.name}");
    }
    void OnTriggerEnter(Collider other)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Bait3D] OnTriggerEnter with {other.gameObject.name} tag={other.gameObject.tag}");
        }
        if (activated) return;
        if (other.CompareTag("Player"))
        {
            if (showDebugLogs) Debug.Log("[Bait3D] Trigger with player ignored");
            return;
        }
        if (other.CompareTag("Guard"))
        {
            if (showDebugLogs) Debug.Log("[Bait3D] Triggered by guard collider");
        }
        ActivateBait($"trigger with {other.gameObject.name}");
    }
    void ActivateBait(string reason)
    {
        if (activated) return;
        activated = true;
        CancelInvoke(nameof(CheckIfGrounded));
        Vector3 landPos = transform.position;
        if (showDebugLogs)
        {
            Debug.Log($"[Bait3D] Activated at {landPos} reason={reason}");
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        Invoke(nameof(EmitLandingSound), landingSoundDelay);
        CreateSmellNode();
    }
    void EmitLandingSound()
    {
        if (SoundBus3D.Instance != null)
        {
            SoundBus3D.Instance.EmitSound(transform.position, soundRadius, soundIntensity, gameObject);
            if (showDebugLogs)
            {
                Debug.Log($"[Bait3D] Sound emitted radius={soundRadius} intensity={soundIntensity}");
            }
        }
        else
        {
            Debug.LogWarning("[Bait3D] No SoundBus3D Instance");
        }
    }
    void CreateSmellNode()
    {
        if (scentNodePrefab != null)
        {
            ScentNode3D node = Instantiate(scentNodePrefab, transform.position, Quaternion.identity);
            node.Initialize(smellStrength, 0.03f, 90f);
            if (showDebugLogs)
            {
                Debug.Log($"[Bait3D] Smell node created strength={smellStrength}");
            }
        }
        else
        {
            if (!hasLoggedMissingScentPrefab)
            {
                Debug.LogWarning("[Bait3D] No scentNodePrefab set");
                hasLoggedMissingScentPrefab = true;
            }
        }
    }
    void OnDrawGizmos()
    {
        if (!showDebugRadius) return;

        if (activated)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, soundRadius);

            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
        else
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}

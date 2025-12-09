using UnityEngine;

public class ScentTrail3D : MonoBehaviour
{
    [Header("Scent Node Settings")]
    public ScentNode3D scentNodePrefab;
    public float spawnInterval = 0.8f;
    public float minDistance = 0.3f;
    public float baseStrength = 1f;

    [Header("Dynamic Strength")]
    public bool useSpeedModifier = true;
    public float walkStrength = 1f;
    public float sprintStrength = 2.5f;
    public float crouchStrength = 0.2f;

    [Header("Conditional Spawning")]
    public bool onlyWhenMoving = true;
    public bool reduceWhenCrouching = true;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showTrailGizmos = true;
    public int maxGizmoNodes = 20;

    private float timeSinceLastSpawn;
    private Vector3 lastSpawnPos;
    private Vector3 lastFramePos;
    private PlayerController3D playerController;
    private Vector3[] recentPositions;
    private int positionIndex;
    private bool hasLoggedMissingPrefab = false;
    private int totalNodesSpawned = 0;

    void Awake()
    {
        recentPositions = new Vector3[maxGizmoNodes];
    }

    void Start()
    {
        lastSpawnPos = transform.position;
        lastFramePos = transform.position;
        playerController = GetComponent<PlayerController3D>();

        if (playerController == null)
        {
            Debug.LogWarning("[ScentTrail3D] No PlayerController3D found! Using velocity-based movement detection.");
        }
        else
        {
            Debug.Log("[ScentTrail3D] PlayerController3D found");
        }

        if (scentNodePrefab == null)
        {
            Debug.LogError("[ScentTrail3D] NO SCENT NODE PREFAB ASSIGNED! Create a ScentNode3D prefab and assign it.");
        }
        else
        {
            Debug.Log($"[ScentTrail3D] Prefab assigned: {scentNodePrefab.name}");
        }

        Debug.Log("[ScentTrail3D] Ready");
    }

    void Update()
    {
        // Check prefab
        if (scentNodePrefab == null)
        {
            if (!hasLoggedMissingPrefab)
            {
                Debug.LogError("[ScentTrail3D] scentNodePrefab is NULL - cannot spawn scent nodes!");
                hasLoggedMissingPrefab = true;
            }
            return;
        }

        // Track movement via position delta (works even if PlayerController doesn't have IsMoving)
        timeSinceLastSpawn += Time.deltaTime;
        float frameVelocity = Vector3.Distance(transform.position, lastFramePos) / Mathf.Max(Time.deltaTime, 0.001f);
        lastFramePos = transform.position;

        // Check if moving
        bool isMoving = IsPlayerMoving(frameVelocity);

        if (onlyWhenMoving && !isMoving)
        {
            return;
        }

        float distFromLastSpawn = Vector3.Distance(transform.position, lastSpawnPos);

        if (timeSinceLastSpawn >= spawnInterval && distFromLastSpawn >= minDistance)
        {
            SpawnScentNode();
        }
    }

    bool IsPlayerMoving(float calculatedVelocity)
    {
        // Primary check: calculated velocity from position delta
        if (calculatedVelocity > 0.5f)
        {
            return true;
        }

        // Secondary check: PlayerController if available
        if (playerController != null)
        {
            // Use reflection-safe check
            var type = playerController.GetType();
            var method = type.GetMethod("IsMoving");
            if (method != null)
            {
                try
                {
                    return (bool)method.Invoke(playerController, null);
                }
                catch { }
            }
        }

        return false;
    }

    void SpawnScentNode()
    {
        float strength = CalculateScentStrength();

        // Skip if strength is too low
        if (strength < 0.05f)
        {
            return;
        }

        ScentNode3D node = Instantiate(scentNodePrefab, transform.position, Quaternion.identity);

        // Determine decay rate based on movement type
        float decay;
        float lifetime;

        if (strength >= sprintStrength * 0.9f)
        {
            decay = 0.08f;
            lifetime = 30f;
        }
        else if (strength <= crouchStrength * 1.1f)
        {
            decay = 0.04f;
            lifetime = 20f;
        }
        else
        {
            decay = 0.06f;
            lifetime = 30f;
        }

        // Use Initialize to properly set values BEFORE Update runs
        node.Initialize(strength, decay, lifetime);

        totalNodesSpawned++;

        // Store for gizmo drawing
        recentPositions[positionIndex % maxGizmoNodes] = transform.position;
        positionIndex++;

        if (showDebugLogs)
        {
            Debug.Log($"[ScentTrail3D] Node #{totalNodesSpawned} at {transform.position} strength={strength:F2} decay={decay}");
        }

        lastSpawnPos = transform.position;
        timeSinceLastSpawn = 0f;
    }

    float CalculateScentStrength()
    {
        if (!useSpeedModifier || playerController == null)
        {
            return baseStrength;
        }

        // Check crouching/sprinting via properties (with null safety)
        bool isCrouching = false;
        bool isSprinting = false;

        var type = playerController.GetType();

        var crouchProp = type.GetProperty("IsCrouching");
        if (crouchProp != null)
        {
            try { isCrouching = (bool)crouchProp.GetValue(playerController); } catch { }
        }

        var sprintProp = type.GetProperty("IsSprinting");
        if (sprintProp != null)
        {
            try { isSprinting = (bool)sprintProp.GetValue(playerController); } catch { }
        }

        if (isCrouching)
        {
            return reduceWhenCrouching ? crouchStrength : baseStrength;
        }
        else if (isSprinting)
        {
            return sprintStrength;
        }
        else
        {
            return walkStrength;
        }
    }

    void OnDrawGizmos()
    {
        if (!showTrailGizmos || !Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, lastSpawnPos);

        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        for (int i = 0; i < maxGizmoNodes && i < positionIndex; i++)
        {
            if (recentPositions[i] != Vector3.zero)
            {
                Gizmos.DrawWireSphere(recentPositions[i], 0.2f);
            }
        }
    }
}
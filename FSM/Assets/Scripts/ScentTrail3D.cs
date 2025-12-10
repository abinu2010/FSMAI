using UnityEngine;

public class ScentTrail3D : MonoBehaviour
{
    public ScentNode3D scentNodePrefab;
    public float spawnInterval = 0.8f;
    public float minDistance = 0.3f;
    public float baseStrength = 1f;
    public bool useSpeedModifier = true;
    public float walkStrength = 1f;
    public float sprintStrength = 2.5f;
    public float crouchStrength = 0.2f;
    public bool onlyWhenMoving = true;
    public bool reduceWhenCrouching = true;
    public bool showDebugLogs = true;
    public bool showTrailGizmos = true;
    public int maxGizmoNodes = 20;
    private float timeSinceLastSpawn;
    private Vector3 lastSpawnPos;
    private Vector3 lastFramePos;
    private PlayerController3D playerController;
    private Vector3[] recentPositions;
    private int positionIndex;
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
    }
    void Update()
    {
        timeSinceLastSpawn += Time.deltaTime;
        float frameVelocity = Vector3.Distance(transform.position, lastFramePos) / Mathf.Max(Time.deltaTime, 0.001f);
        lastFramePos = transform.position;
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
        if (calculatedVelocity > 0.5f)
        {
            return true;
        }
        if (playerController != null)
        {
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
        if (strength < 0.05f)
        {
            return;
        }
        ScentNode3D node = Instantiate(scentNodePrefab, transform.position, Quaternion.identity);
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
        node.Initialize(strength, decay, lifetime);
        totalNodesSpawned++;
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
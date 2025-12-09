using UnityEngine;

public class ScentTrail3D : MonoBehaviour
{
    [Header("Scent Node Settings")]
    public ScentNode3D scentNodePrefab;
    public float spawnInterval = 0.5f;
    public float minDistance = 0.5f;
    public float baseStrength = 1f;

    [Header("Dynamic Strength")]
    public bool useSpeedModifier = true;
    public float walkStrength = 1f;
    public float sprintStrength = 2f;
    public float crouchStrength = 0.3f;

    [Header("Conditional Spawning")]
    public bool onlyWhenMoving = true;
    public bool disableWhenCrouching = true;

    private float timeSinceLastSpawn;
    private Vector3 lastSpawnPos;
    private PlayerController3D playerController;

    void Start()
    {
        lastSpawnPos = transform.position;
        playerController = GetComponent<PlayerController3D>();
    }

    void Update()
    {
        if (playerController == null)
        {
            return;
        }

        if (disableWhenCrouching && playerController.IsCrouching)
        {
            return;
        }

        if (onlyWhenMoving && !playerController.IsMoving())
        {
            return;
        }

        timeSinceLastSpawn += Time.deltaTime;
        float moved = Vector3.Distance(transform.position, lastSpawnPos);

        if (timeSinceLastSpawn >= spawnInterval && moved >= minDistance)
        {
            if (scentNodePrefab != null)
            {
                float strength = CalculateScentStrength();

                ScentNode3D node = Instantiate(scentNodePrefab, transform.position, Quaternion.identity);
                node.strength = strength;

                Debug.Log("[ScentTrail3D] Node at " + transform.position + " strength=" + strength);
            }

            lastSpawnPos = transform.position;
            timeSinceLastSpawn = 0f;
        }
    }

    float CalculateScentStrength()
    {
        if (!useSpeedModifier)
        {
            return baseStrength;
        }

        if (playerController.IsSprinting)
        {
            return sprintStrength;
        }
        else if (playerController.IsCrouching)
        {
            return crouchStrength;
        }
        else
        {
            return walkStrength;
        }
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, lastSpawnPos);
        }
    }
}
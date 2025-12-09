using System.Collections.Generic;
using UnityEngine;

public class ScentNode3D : MonoBehaviour
{
    public static readonly List<ScentNode3D> AllNodes = new List<ScentNode3D>();

    public float strength = 1f;
    public float decayRate = 0.2f;
    public float maxLifetime = 30f; // NEW: Max time before forced cleanup

    [Header("Visual Debug")]
    public bool showDebugSphere = true;
    public Color debugColor = new Color(0, 1, 0, 0.3f);

    float age = 0f;

    void OnEnable()
    {
        AllNodes.Add(this);
        Debug.Log($"[ScentNode3D] Spawned at {transform.position} strength={strength:F2}");
    }

    void OnDisable()
    {
        AllNodes.Remove(this);
    }

    void Update()
    {
        age += Time.deltaTime;
        strength -= decayRate * Time.deltaTime;

        // Cleanup conditions
        if (strength <= 0f || age >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnDrawGizmos()
    {
        if (showDebugSphere && Application.isPlaying)
        {
            Gizmos.color = debugColor * strength; // Fade as strength decreases
            Gizmos.DrawSphere(transform.position, 0.3f * strength);
        }
    }

    /// <summary>
    /// Get the effective strength at a given position
    /// </summary>
    public float GetStrengthAtPosition(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        // Strength falls off with distance
        return strength / (1f + distance);
    }
}
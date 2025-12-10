using System.Collections.Generic;
using UnityEngine;

public class ScentNode3D : MonoBehaviour
{
    public static readonly List<ScentNode3D> AllNodes = new List<ScentNode3D>();
    [Header("Scent Properties")]
    public float strength = 1f;
    public float decayRate = 0.025f; // Slow decay lasts seconds at strength 1.0
    public float maxLifetime = 60f; // Max lifetime in seconds
    [Header("Visual Debug")]
    public bool showDebugSphere = true;
    public Color debugColor = new Color(0.2f, 1f, 0.2f, 0.4f);
    float age = 0f;
    float initialStrength;
    bool initialized = false;
    public void Initialize(float newStrength, float newDecayRate, float newMaxLifetime = 60f)
    {
        strength = newStrength;
        decayRate = newDecayRate;
        maxLifetime = newMaxLifetime;
        initialStrength = newStrength;
        age = 0f;
        initialized = true;
    }
    void Awake()
    {
        if (!initialized)
        {
            initialStrength = strength;
        }
    }
    void OnEnable()
    {
        if (!AllNodes.Contains(this))
        {
            AllNodes.Add(this);
        }
    }
    void OnDisable()
    {
        AllNodes.Remove(this);
        Debug.Log($"[ScentNode3D] Removed (Remaining nodes: {AllNodes.Count})");
    }
    void OnDestroy()
    {
        AllNodes.Remove(this);
    }
    void Update()
    {
        if (!initialized)
        {
            initialized = true;
            initialStrength = strength;
            Debug.Log($"[ScentNode3D] REGISTERED at {transform.position} strength={strength:F2} decay={decayRate} (Total nodes: {AllNodes.Count})");
        }
        age += Time.deltaTime;
        strength -= decayRate * Time.deltaTime;
        strength = Mathf.Max(strength, 0.05f); // Keep minimum detectability until lifetime expires
        if (age >= maxLifetime)
        {
            Debug.Log($"[ScentNode3D] Expired at {transform.position} (age={age:F1}s, remaining strength={strength:F2})");
            Destroy(gameObject);
        }
    }
    public float GetStrengthAtPosition(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        return strength / (1f + distance * distance * 0.1f);
    }
    public void AddStrength(float amount)
    {
        strength += amount;
        Debug.Log($"[ScentNode3D] Strength boosted to {strength:F2}");
    }
    void OnDrawGizmos()
    {
        if (!showDebugSphere || !Application.isPlaying) return;
        float ratio = Mathf.Clamp01(strength / Mathf.Max(initialStrength, 0.1f));
        Color col = debugColor;
        col.a = ratio * 0.5f;
        Gizmos.color = col;
        float size = 0.2f + (strength * 0.3f);
        Gizmos.DrawSphere(transform.position, size);
        Gizmos.color = new Color(col.r, col.g, col.b, 0.2f);
        Gizmos.DrawWireSphere(transform.position, size * 2f);
    }
    public static ScentNode3D FindStrongestNear(Vector3 position, float maxDistance)
    {
        ScentNode3D strongest = null;
        float bestScore = 0f;
        foreach (var node in AllNodes)
        {
            if (node == null) continue;
            float dist = Vector3.Distance(position, node.transform.position);
            if (dist > maxDistance) continue;
            float score = node.strength / (1f + dist);
            if (score > bestScore)
            {
                bestScore = score;
                strongest = node;
            }
        }
        return strongest;
    }
    public static float GetTotalStrengthAt(Vector3 position, float maxDistance)
    {
        float total = 0f;

        foreach (var node in AllNodes)
        {
            if (node == null) continue;

            float dist = Vector3.Distance(position, node.transform.position);
            if (dist <= maxDistance)
            {
                total += node.GetStrengthAtPosition(position);
            }
        }

        return total;
    }
}
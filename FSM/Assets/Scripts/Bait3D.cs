using UnityEngine;

public class Bait3D : MonoBehaviour
{
    public float soundRadius = 10f;
    public float soundIntensity = 1f;
    public float smellStrength = 1.5f;
    public ScentNode3D scentNodePrefab;

    bool activated;

    void OnCollisionEnter(Collision collision)
    {
        if (activated)
        {
            return;
        }

        activated = true;
        Debug.Log("[Bait3D] Landed at " + transform.position);

        if (SoundBus3D.Instance != null)
        {
            SoundBus3D.Instance.EmitSound(transform.position, soundRadius, soundIntensity, gameObject);
        }

        if (scentNodePrefab != null)
        {
            ScentNode3D node = Instantiate(scentNodePrefab, transform.position, Quaternion.identity);
            node.strength = smellStrength;
            Debug.Log("[Bait3D] Smell node at " + transform.position);
        }
    }
}

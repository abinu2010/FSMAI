using UnityEngine;

[RequireComponent(typeof(PlayerController3D))]
public class PlayerNoiseEmitter3D : MonoBehaviour
{
    [Header("Movement Sound")]
    public float stepInterval = 0.6f;
    public float baseMoveSoundRadius = 6f;
    public float sprintMultiplier = 1.5f;
    public float crouchMultiplier = 0.3f; // NEW: Crouch = quieter
    public float minSpeedForSound = 0.1f;

    [Header("Surface Types")]
    public LayerMask noisySurfaces; // NEW: Metal, wood = louder
    public float noisySurfaceMultiplier = 1.5f;

    [Header("Audio Feedback")]
    public AudioSource footstepAudio;
    public AudioClip[] footstepSounds;
    public AudioClip[] sprintSounds;

    float stepTimer;
    Vector3 lastPos;
    CharacterController controller;

    void Awake()
    {
        lastPos = transform.position;
        controller = GetComponent<CharacterController>();
        Debug.Log("[PlayerNoiseEmitter3D] Ready");
    }

    void Update()
    {
        Vector3 currentPos = transform.position;
        float distance = Vector3.Distance(currentPos, lastPos);
        float speed = Time.deltaTime > 0f ? distance / Time.deltaTime : 0f;

        lastPos = currentPos;

        if (speed >= minSpeedForSound)
        {
            stepTimer += Time.deltaTime;

            // Adjust step interval based on speed
            float adjustedInterval = stepInterval;
            if (Input.GetKey(KeyCode.LeftShift)) // Sprint
            {
                adjustedInterval *= 0.7f; // Faster steps
            }
            else if (Input.GetKey(KeyCode.LeftControl)) // Crouch
            {
                adjustedInterval *= 1.5f; // Slower steps
            }

            if (stepTimer >= adjustedInterval)
            {
                float radius = CalculateNoiseRadius();
                EmitMoveSound(radius);
                PlayFootstepAudio();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    float CalculateNoiseRadius()
    {
        float radius = baseMoveSoundRadius;

        // Sprint modifier
        if (Input.GetKey(KeyCode.LeftShift))
        {
            radius *= sprintMultiplier;
        }
        // Crouch modifier
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            radius *= crouchMultiplier;
        }

        // Check surface type below player
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
        {
            if (((1 << hit.collider.gameObject.layer) & noisySurfaces) != 0)
            {
                radius *= noisySurfaceMultiplier;
                Debug.Log("[PlayerNoiseEmitter3D] On noisy surface!");
            }
        }

        return radius;
    }

    void EmitMoveSound(float radius)
    {
        if (SoundBus3D.Instance == null)
        {
            return;
        }

        Debug.Log($"[PlayerNoiseEmitter3D] Footstep radius={radius:F1}");
        SoundBus3D.Instance.EmitSound(transform.position, radius, 1f, gameObject);
    }

    void PlayFootstepAudio()
    {
        if (footstepAudio == null) return;

        AudioClip[] clips = Input.GetKey(KeyCode.LeftShift) ? sprintSounds : footstepSounds;

        if (clips != null && clips.Length > 0)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            footstepAudio.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Call this when player performs a loud action (breaking object, etc)
    /// </summary>
    public void EmitActionSound(float radius, float intensity)
    {
        if (SoundBus3D.Instance == null)
        {
            return;
        }

        Debug.Log($"[PlayerNoiseEmitter3D] Action sound radius={radius:F1} intensity={intensity:F1}");
        SoundBus3D.Instance.EmitSound(transform.position, radius, intensity, gameObject);
    }

    /// <summary>
    /// Call this when landing from a jump/fall
    /// </summary>
    public void EmitLandingSound(float fallSpeed)
    {
        float radius = baseMoveSoundRadius * Mathf.Clamp(fallSpeed / 5f, 0.5f, 3f);
        EmitActionSound(radius, 1.5f);
    }
}
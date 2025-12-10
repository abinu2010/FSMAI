using UnityEngine;

[RequireComponent(typeof(PlayerController3D))]
public class PlayerNoiseEmitter3D : MonoBehaviour
{
    [Header("Movement Sound")]
    public float stepInterval = 0.6f;
    public float baseMoveSoundRadius = 6f;
    public float sprintMultiplier = 1.5f;
    public float crouchMultiplier = 0.3f; 
    public float minSpeedForSound = 0.1f;
    [Header("Surface Types")]
    public LayerMask noisySurfaces; 
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
            float adjustedInterval = stepInterval;
            if (Input.GetKey(KeyCode.LeftShift)) // Sprint
            {
                adjustedInterval *= 0.7f;
            }
            else if (Input.GetKey(KeyCode.LeftControl)) // Crouch
            {
                adjustedInterval *= 1.5f;
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
        if (Input.GetKey(KeyCode.LeftShift))
        {
            radius *= sprintMultiplier;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            radius *= crouchMultiplier;
        }
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
        {
            if (((1 << hit.collider.gameObject.layer) & noisySurfaces) != 0)
            {
                radius *= noisySurfaceMultiplier;
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
    public void EmitActionSound(float radius, float intensity)
    {
        if (SoundBus3D.Instance == null)
        {
            return;
        }
        SoundBus3D.Instance.EmitSound(transform.position, radius, intensity, gameObject);
    }
    public void EmitLandingSound(float fallSpeed)
    {
        float radius = baseMoveSoundRadius * Mathf.Clamp(fallSpeed / 5f, 0.5f, 3f);
        EmitActionSound(radius, 1.5f);
    }
}
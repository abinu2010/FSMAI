using UnityEngine;

public class AudioManager3D : MonoBehaviour
{
    public static AudioManager3D Instance { get; private set; }

    public AudioSource sfxSource;

    public AudioClip objectivePickupClip;
    public AudioClip playerHurtClip;
    public AudioClip playerDeathClip;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayObjectivePickup()
    {
        if (sfxSource == null || objectivePickupClip == null) return;
        sfxSource.PlayOneShot(objectivePickupClip);
    }

    public void PlayPlayerHurt()
    {
        if (sfxSource == null || playerHurtClip == null) return;
        sfxSource.PlayOneShot(playerHurtClip);
    }

    public void PlayPlayerDeath()
    {
        if (sfxSource == null || playerDeathClip == null) return;
        sfxSource.PlayOneShot(playerDeathClip);
    }
}

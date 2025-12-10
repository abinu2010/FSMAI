using UnityEngine;

public class PlayerHealth3D : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Debug")]
    public bool logEvents = true;

    float currentHealth;
    bool isDead;

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;

        if (logEvents)
        {
            Debug.Log("[PlayerHealth3D] Ready with health " + currentHealth);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        if (amount < 0f)
        {
            amount = 0f;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (logEvents)
        {
            Debug.Log("[PlayerHealth3D] Took damage " + amount + " current " + currentHealth);
        }

        if (AudioManager3D.Instance != null)
        {
            AudioManager3D.Instance.PlayPlayerHurt();
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        if (amount < 0f)
        {
            amount = 0f;
        }

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        if (logEvents)
        {
            Debug.Log("[PlayerHealth3D] Healed " + amount + " current " + currentHealth);
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (logEvents)
        {
            Debug.Log("[PlayerHealth3D] Player died");
        }

        if (AudioManager3D.Instance != null)
        {
            AudioManager3D.Instance.PlayPlayerDeath();
        }

        var controller = GetComponent<PlayerController3D>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        var look = GetComponentInChildren<PlayerLook3D>();
        if (look != null)
        {
            look.enabled = false;
        }

        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.ClearAllAlerts();
        }

        if (PlayerHealthUI3D.Instance != null)
        {
            PlayerHealthUI3D.Instance.ShowLose();
        }
    }
}

using UnityEngine;

public class PlayerHealth3D : MonoBehaviour
{
    public float maxHealth = 100f;
    float currentHealth;
    bool isDead;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
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
    }
    void Die()
    {
        if (isDead) return;
        isDead = true;
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

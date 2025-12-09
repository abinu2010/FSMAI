using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI3D : MonoBehaviour
{
    public PlayerHealth3D playerHealth;
    public Slider healthSlider;

    void Start()
    {
        if (playerHealth == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth3D>();
            }
        }

        if (playerHealth != null && healthSlider != null)
        {
            healthSlider.maxValue = playerHealth.CurrentHealth;
            healthSlider.value = playerHealth.CurrentHealth;
        }
    }

    void Update()
    {
        if (playerHealth == null || healthSlider == null) return;

        healthSlider.value = playerHealth.CurrentHealth;
    }
}

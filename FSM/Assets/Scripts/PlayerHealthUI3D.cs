using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI3D : MonoBehaviour
{
    public PlayerHealth3D playerHealth;
    public Slider healthSlider;
    public TMP_Text healthText;
    public GameObject losePanel;
    public GameObject winPanel;

    static PlayerHealthUI3D instance;

    public static PlayerHealthUI3D Instance => instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

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

        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        UpdateHealthText();
    }

    void Update()
    {
        if (playerHealth == null) return;

        if (healthSlider != null)
        {
            healthSlider.value = playerHealth.CurrentHealth;
        }

        UpdateHealthText();
    }

    void UpdateHealthText()
    {
        if (healthText == null || playerHealth == null) return;

        int hp = Mathf.CeilToInt(playerHealth.CurrentHealth);
        healthText.text = "HP " + hp;
    }

    public void ShowLose()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);
        }

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        Time.timeScale = 0f;
    }

    public void ShowWin()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }

        Time.timeScale = 0f;
    }
}

// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI coinCounterText;
    [SerializeField] private TextMeshProUGUI waveCounterText;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Player References")]
    [SerializeField] private HealthSystem playerHealth;

    // This will hold the specific instance of WaveManager for this scene.
    private WaveManager currentWaveManager;

    public void Initialize(WaveManager waveManager)
    {
        this.currentWaveManager = waveManager;
        if (this.currentWaveManager != null)
        {
            this.currentWaveManager.OnNewWaveStarted += UpdateWaveCounter;
        }
    }
    
    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
        }
        // Assuming PlayerWallet is still static. If not, it needs the same treatment.
        PlayerWallet.OnMoneyChanged += UpdateCoinCounter;
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
        PlayerWallet.OnMoneyChanged -= UpdateCoinCounter;

        // We also need to unsubscribe from the WaveManager event.
        if (this.currentWaveManager != null)
        {
            this.currentWaveManager.OnNewWaveStarted -= UpdateWaveCounter;
        }
    }

    private void UpdateWaveCounter(string waveName)
    {
        if (waveCounterText != null)
        {
            waveCounterText.text = waveName;
        }
    }

    // --- No other changes are needed below this line ---

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
        if (healthText != null)
        {
            healthText.text = Mathf.Ceil(currentHealth).ToString() + " / " + Mathf.Ceil(maxHealth).ToString();
        }
    }

    private void UpdateCoinCounter(int newTotal)
    {
        if (coinCounterText != null)
        {
            coinCounterText.text = "Moedas: " + newTotal.ToString();
        }
    }
}
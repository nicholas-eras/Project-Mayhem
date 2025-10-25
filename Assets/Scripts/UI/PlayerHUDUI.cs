// PlayerHUDUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUDUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI coinCounterText;
    [SerializeField] private TextMeshProUGUI waveCounterText;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Player References")]
    [SerializeField] private HealthSystem playerHealth;
    private PlayerWallet playerWallet; // <--- Referência de instância

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

    // NOVO MÉTODO PARA O AgentManager CHAMAR
    public void ConnectWallet(PlayerWallet wallet)
    {
        // Limpeza: Desinscreve-se da carteira antiga, se houver
        if (playerWallet != null)
        {
            playerWallet.OnMoneyChanged -= UpdateCoinCounter;
        }

        // Assina a nova carteira
        this.playerWallet = wallet;
        if (this.playerWallet != null)
        {
            this.playerWallet.OnMoneyChanged += UpdateCoinCounter;

            // Atualiza o contador imediatamente
            UpdateCoinCounter(this.playerWallet.CurrentMoney);
        }
    }

    
    void OnDestroy() // Usa OnDestroy para garantir a limpeza
    {
        if (this.currentWaveManager != null)
        {
            this.currentWaveManager.OnNewWaveStarted -= UpdateWaveCounter;
        }
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
        // --- LIMPA A INSCRIÇÃO DA CARTEIRA ---
        if (playerWallet != null)
        {
            playerWallet.OnMoneyChanged -= UpdateCoinCounter;
        }
        // --- FIM DA LIMPEZA ---
    }    

    private void UpdateWaveCounter(string waveName)
    {
        if (waveCounterText != null)
        {
            waveCounterText.text = waveName;
        }
    }

    // Conecta a vida (você pode mover a lógica de vida para um método de conexão também)
    public void ConnectHealth(HealthSystem health)
    {
        if (playerHealth != null)
        {
             playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
        this.playerHealth = health;
        if (this.playerHealth != null)
        {
             this.playerHealth.OnHealthChanged += UpdateHealthBar;
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
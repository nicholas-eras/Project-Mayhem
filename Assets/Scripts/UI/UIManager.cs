using UnityEngine;
using UnityEngine.UI; // Necessário para o Slider
using TMPro; // Necessário para o TextMeshPro

public class UIManager : MonoBehaviour
{
    [Header("Referências da UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI coinCounterText;

    [Header("Referências do Jogador")]
    [SerializeField] private HealthSystem playerHealth;
    [SerializeField] private TextMeshProUGUI healthText; // <<<--- NOVA LINHA

    [SerializeField] private TextMeshProUGUI waveCounterText; // <<<--- NOVA LINHA

    void Start()
    {
        // Força uma atualização inicial da UI assim que o jogo começa,
        // usando os dados que pegamos diretamente do HealthSystem.
        if (playerHealth != null)
        {
            UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    // OnEnable é chamado quando o objeto é ativado
    void OnEnable()
    {
        // Se inscreve para ouvir os avisos
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
        }
        PlayerWallet.OnMoneyChanged += UpdateCoinCounter;
        WaveManager.OnNewWaveStarted += UpdateWaveCounter;
    }

    // OnDisable é chamado quando o objeto é desativado
    void OnDisable()
    {
        // Cancela a inscrição para evitar erros
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
        PlayerWallet.OnMoneyChanged -= UpdateCoinCounter;
        WaveManager.OnNewWaveStarted -= UpdateWaveCounter;
    }

    // --- FUNÇÃO ATUALIZADA ---
    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        // Atualiza a barra de preenchimento (Slider)
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }

        // Atualiza o texto com os valores numéricos
        if (healthText != null)
        {
            // Mathf.Ceil arredonda o número para cima, para evitar "99.5" de vida.
            // .ToString() converte o número para texto.
            healthText.text = Mathf.Ceil(currentHealth).ToString() + " / " + Mathf.Ceil(maxHealth).ToString();
        }
    }

    // Esta função é chamada pelo evento OnMoneyChanged
    private void UpdateCoinCounter(int newTotal)
    {
        if (coinCounterText != null)
        {
            coinCounterText.text = "Moedas: " + newTotal.ToString();
        }
    }

    private void UpdateWaveCounter(string waveName)
    {
        if (waveCounterText != null)
        {
            // Formata o texto para ficar mais bonito, ex: "Onda: 1"
            waveCounterText.text = "Onda: " + waveName;
        }
    }
}
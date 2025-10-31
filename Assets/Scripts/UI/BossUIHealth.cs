using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossUIHealth : MonoBehaviour
{
    [Header("Referências da UI")]
    [Tooltip("O Slider (barra de progresso) para mostrar a vida atual.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("O TextMeshPro ou Text para mostrar o nome do Boss.")]
    [SerializeField] private TMP_Text bossNameText;

    [Tooltip("O TextMeshPro ou Text para mostrar o valor da vida (ex: 500/1000).")]
    [SerializeField] private TMP_Text healthValueText;

    [Tooltip("O GameObject que contém todos os elementos da UI da vida.")]
    [SerializeField] private GameObject healthUIRoot;
    
    private BossHealthLinker bossLinker; // Referência ao boss atual

    void OnEnable()
    {
        // Log para quando o WaveManager ativa este GameObject
        Debug.Log($"[BossUIHealth] !! OnEnable() !! O GameObject '{this.name}' foi ATIVADO.");
        // Não fazemos mais nada, esperamos o WaveManager chamar Initialize()
    }

    void Start()
    {
        Debug.Log("[BossUIHealth] Start() chamado.");
        // 1. Configura o nome do Boss (pode ser fixo)
        if (bossNameText != null)
        {
            bossNameText.text = "Wall of Flesh"; 
        }
        
        // 2. O WaveManager é quem controla se o root está ativo ou não.
        // O HideHealthUI() cuidará de desativá-lo se o boss morrer.
    }

    void OnDestroy()
    {
        // Limpa eventos se formos destruídos enquanto conectados
        if (bossLinker != null)
        {
            Debug.Log($"[BossUIHealth] OnDestroy: Limpando eventos do Linker (ID: {bossLinker.GetInstanceID()})");
            bossLinker.OnBossHealthChanged -= UpdateHealthUI;
            bossLinker.OnBossDefeated.RemoveListener(HideHealthUI);
        }
    }
    
    // O método Update() foi removido.
    // O método OnShopClosed() foi removido.
    
    /// <summary>
    /// Método PÚBLICO chamado pelo WaveManager para forçar a conexão
    /// com um BossHealthLinker específico.
    /// </summary>
    public void Initialize(BossHealthLinker linkerToConnect)
    {
        if (linkerToConnect == null)
        {
            Debug.LogError("[BossUIHealth] Initialize foi chamado com um Linker NULO!");
            return;
        }

        // Se já estávamos conectados a um boss anterior (de um retry falho), limpa os eventos
        if (bossLinker != null)
        {
            bossLinker.OnBossHealthChanged -= UpdateHealthUI;
            bossLinker.OnBossDefeated.RemoveListener(HideHealthUI);
        }

        // Conecta ao novo boss
        bossLinker = linkerToConnect;
        
        Debug.Log($"[BossUIHealth] ==== InitializeUI CONECTANDO (ID: {bossLinker.GetInstanceID()}) ====");
        
        if (healthUIRoot == null)
        {
            Debug.LogError("[BossUIHealth] O campo 'Health UI Root' NÃO está atribuído!");
            return;
        }

        // Garante que a UI esteja visível (caso o WaveManager já a tenha ativado)
        healthUIRoot.SetActive(true);
        
        // 1. Se inscreve no evento de mudança de vida
        bossLinker.OnBossHealthChanged += UpdateHealthUI;
        
        // 2. Se inscreve no evento de derrota para ocultar a UI
        bossLinker.OnBossDefeated.AddListener(HideHealthUI);
        
        // 3. Define a vida inicial (para preencher a barra no início)
        Debug.Log($"[BossUIHealth] ...Definindo vida inicial (da UI) para {bossLinker.CurrentHealth} / {bossLinker.initialTotalHealth}");
        UpdateHealthUI(bossLinker.CurrentHealth, bossLinker.initialTotalHealth);
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            // O Slider de vida lida com o valor normalizado (0 a 1)
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthValueText != null)
        {
            // Atualiza o texto com o valor formatado (ex: 1000 / 5000)
            healthValueText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }

    private void HideHealthUI()
    {
        Debug.Log($"[BossUIHealth] HideHealthUI() chamado pelo evento OnBossDefeated.");
        // Oculta o painel de vida quando o Boss é derrotado.
        healthUIRoot?.SetActive(false);

        // Quando o boss morre, paramos de ouvir para evitar erros
        if (bossLinker != null)
        {
            bossLinker.OnBossHealthChanged -= UpdateHealthUI;
            bossLinker.OnBossDefeated.RemoveListener(HideHealthUI);
            bossLinker = null; // Limpa a referência
        }
    }
}
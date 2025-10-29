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
    
    private BossHealthLinker bossLinker;
    private bool isInitialized = false;

    void Start()
    {
        // 1. Configura o nome do Boss
        if (bossNameText != null)
        {
            bossNameText.text = "Greater Boss"; 
        }
        
        // 2. Oculta o Root por padrão até encontrar o Linker.
        healthUIRoot?.SetActive(false); 
        
        // 3. INSCREVE-SE NO EVENTO DE FECHAMENTO DA LOJA
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnShopClosed += OnShopClosed;
        }
    }

    void OnDestroy()
    {
        // Limpa a inscrição
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnShopClosed -= OnShopClosed;
        }
        
        if (bossLinker != null)
        {
            bossLinker.OnBossHealthChanged -= UpdateHealthUI;
            bossLinker.OnBossDefeated.RemoveListener(HideHealthUI);
        }
    }
    
    void Update()
    {
        // 1. Se já estiver inicializado, não faça nada.
        if (isInitialized)
        {
            return;
        }                        

        // 2. Tenta encontrar o Linker (o Boss já pode ter sido instanciado pelo WaveManager)
        bossLinker = FindObjectOfType<BossHealthLinker>();

        if (bossLinker != null)
        {
            // Se encontrou, inicializa a UI
            InitializeUI();
        }
    }
    
    // =================================================================
    // NOVO MÉTODO: Chamado quando o Shop fecha, para reiniciar a busca.
    // =================================================================
    private void OnShopClosed()
    {
        // Quando a loja fecha, a Wave está prestes a começar ou já começou.
        // O Boss pode ser instanciado aqui. Forçamos a flag de inicialização para falso
        // (caso tivéssemos inicializado antes, o que é improvável aqui) e garantimos
        // que o Update() continue procurando o novo Boss Linker da nova Wave.
        isInitialized = false;
        
        // NOVO: Chamamos o Update manualmente uma vez para tentar encontrar o Boss
        // imediatamente, caso ele tenha sido instanciado no mesmo frame do fechamento
        // da loja.
        Update(); 
    }
    
    /// <summary>
    /// Conecta a UI aos eventos do BossHealthLinker.
    /// Chamado APENAS QUANDO O LINKER É ENCONTRADO NO UPDATE().
    /// </summary>
    private void InitializeUI()
    {
        if (bossLinker == null || isInitialized) return;
        
        // VERIFICAÇÃO DE ERRO: Garante que a referência do root está no Inspector
        if (healthUIRoot == null)
        {
            Debug.LogError("[BossUIHealth] O campo 'Health UI Root' NÃO está atribuído no Inspector! Não é possível exibir a vida.");
            isInitialized = true; // Para de tentar conectar
            return;
        }

        // Garante que a UI esteja visível
        healthUIRoot.SetActive(true);
        
        // 1. Se inscreve no evento de mudança de vida
        bossLinker.OnBossHealthChanged += UpdateHealthUI;
        
        // 2. Se inscreve no evento de derrota para ocultar a UI
        bossLinker.OnBossDefeated.AddListener(HideHealthUI);
        
        // 3. Define a vida inicial (para preencher a barra no início)
        UpdateHealthUI(bossLinker.CurrentHealth, bossLinker.initialTotalHealth);
        
        isInitialized = true;
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
        // Oculta o painel de vida quando o Boss é derrotado.
        healthUIRoot?.SetActive(false);
    }
}

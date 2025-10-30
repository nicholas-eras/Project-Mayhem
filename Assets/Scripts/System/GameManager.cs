using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode; // Necessário para a lógica de rede (Host/Client)

/// <summary>
/// Gerenciador principal do estado do jogo. Controla o fluxo de Game Over,
/// Vitória, reinício de fase/onda e transições de cena.
/// É um Singleton que persiste entre as cenas (DontDestroyOnLoad).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // Singleton Instance

    [Header("Configuração de Cenas")]
    [Tooltip("O nome exato da sua cena de seleção de mapas/lobby.")]
    [SerializeField] private string mapSelectSceneName = "MapSelectScene";

    // --- Variáveis de UI ---
    // Estas referências são procuradas e preenchidas automaticamente
    // pela lógica OnSceneLoaded() quando uma cena de jogo carrega.
    private GameObject gameOverPanel;
    private GameObject retryButton;

    // --- Memória Persistente do Lobby ---
    public SlotState[] LastLobbySlotStates { get; set; } = null;
    public int[] LastLobbySkinIDs { get; set; } = null;
    public ulong[] LastLobbyClientIds { get; set; } = null;

    private bool isGameOver = false; // Flag interna para evitar múltiplas chamadas

    
    void Awake()
    {
        // Configuração padrão do Singleton DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Garante que o GameManager persista
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // Destrói duplicatas se já existir um GameManager
        }
    }

    // --- LÓGICA DE PROCURA DE UI (NOVA) ---

    /// <summary>
    /// Subscreve ao evento de cenas carregadas quando o GameManager é ativado.
    /// </summary>
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Limpa a subscrição quando o GameManager é desativado.
    /// </summary>
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Chamado automaticamente pelo Unity sempre que uma nova cena termina de carregar.
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Se voltámos ao menu, limpa as referências da UI do jogo.
        if (scene.name == mapSelectSceneName)
        {
            gameOverPanel = null;
            retryButton = null;
            isGameOver = false; // Garante que o estado de G.O. é resetado
        }
        else // Se carregámos QUALQUER outra cena (Jungle, Cyberpunk, etc.)
        {
            // Procura o painel de Game Over nessa nova cena.
            FindAndAssignGameOverPanel(scene);
        }
    }

    // Em GameManager.cs

    /// <summary>
    /// Procura o painel de Game Over e o botão de Retry na cena atual, USANDO TAGS.
    /// </summary>
    private void FindAndAssignGameOverPanel(Scene scene)
    {
        // !! IMPORTANTE !!
        // Garanta que criou esta Tag e a atribuiu nos seus prefabs de UI
        string panelTag = "GameOverPanel";

        // Nomes dos botões (isto ainda é por nome, o que é OK se forem filhos)
        string retryButtonName = "RetryButton";

        // Usamos Resources.FindObjectsOfTypeAll para encontrar objetos INATIVOS
        var allTransforms = Resources.FindObjectsOfTypeAll<RectTransform>();
        foreach (var t in allTransforms)
        {
            // Verifica se está na cena E tem a Tag correta
            if (t.gameObject.scene.name == scene.name && t.CompareTag(panelTag))
            {
                gameOverPanel = t.gameObject;
                Debug.Log($"[GameManager] Painel com Tag '{panelTag}' encontrado na cena '{scene.name}'.");

                // Procura o botão de retry DENTRO do painel
                Transform retryBtnTransform = gameOverPanel.transform.Find(retryButtonName);
                if (retryBtnTransform != null)
                {
                    retryButton = retryBtnTransform.gameObject;
                }
                else
                {
                    Debug.LogWarning($"[GameManager] Encontrou o painel, mas não o botão '{retryButtonName}' lá dentro.");
                }

                gameOverPanel.SetActive(false);
                return; // Encontrámos!
            }
        }

        // Se o loop terminar e não encontrámos...
        Debug.LogWarning($"[GameManager] Carreguei '{scene.name}' mas não encontrei NENHUM objeto com Tag '{panelTag}'. O fallback de delay será usado.");
        gameOverPanel = null;
    }
    
    // --- LÓGICA DE ESTADO DE JOGO (GAME OVER / VITÓRIA) ---

    /// <summary>
    /// Chamado EXCLUSIVAMENTE pelo PlayerManager quando o último jogador/bot morre.
    /// </summary>
    public void TriggerGameOverFromPlayerManager()
    {
        if (isGameOver) return; 
        isGameOver = true;
        Time.timeScale = 0f;

        // Verifica se a onda era um "Greater Boss Wave"
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        bool allowRetry = (waveManager != null && waveManager.IsCurrentWaveGreaterBoss);

        // Ativa a UI (se a encontrou na cena)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true); 
            if (retryButton != null)
            {
                retryButton.SetActive(allowRetry); 
            }
        }
        else
        {
            // Fallback: Se não encontrou o painel, volta ao menu após um delay
            Debug.Log("[GameManager] gameOverPanel é nulo. A usar fallback de delay.");
            StartCoroutine(LoadMapSelectAfterDelay(2.0f)); // <-- Pode ajustar este delay
        }
    }

     /// <summary>
     /// Chamado pelo WaveManager quando todas as ondas são completadas (Vitória).
     /// </summary>
    public void TriggerVictory()
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f; 

        // TODO: Mostrar um painel de Vitória?

        // Por enquanto, apenas volta ao menu após um delay
        StartCoroutine(LoadMapSelectAfterDelay(5.0f));
    }


    // --- FUNÇÕES CHAMADAS PELOS BOTÕES DA UI ---

    /// <summary>
    /// Chamado pelo botão "Voltar ao Menu" ou similar.
    /// </summary>
    public void OnButtonQuitToMenu()
    {
        // Se somos o Host (ou offline), carregamos a cena
        // (o Host levará todos junto)
        CleanupAndLoadScene(mapSelectSceneName);
    }

    /// <summary>
    /// Chamado pelo botão "Reiniciar Fase". Recarrega a cena atual do zero.
    /// </summary>
    public void OnButtonRestartScene()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Cliente tentou reiniciar a cena. Apenas o Host pode fazer isso.");
            return;
        }
        CleanupAndLoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Chamado pelo botão "Tentar Novamente". Reinicia apenas a wave atual.
    /// </summary>
    public void OnButtonRetryWave()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Cliente tentou dar Retry na wave. Apenas o Host pode fazer isso.");
            return; 
        }

        WaveManager waveManager = FindObjectOfType<WaveManager>();

        if (waveManager != null)
        {
            // Esconde o painel e restaura o estado do jogo
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            isGameOver = false; 
            Time.timeScale = 1.0f; 

            // "Revive" todos
            PlayerTargetable[] playersToReset = FindObjectsOfType<PlayerTargetable>(true);
            foreach(var playerTarget in playersToReset)
            {
                if (!playerTarget.gameObject.activeSelf)
                {
                    playerTarget.gameObject.SetActive(true);
                }
                HealthSystem hs = playerTarget.GetComponent<HealthSystem>();
                if (hs != null)
                {
                    hs.ResetForRetry(); 
                }
            }
            
            waveManager.RetryCurrentWave();
        }
        else
        {
            Debug.LogError("[GameManager] Não foi possível Tentar Novamente. WaveManager não encontrado! Reiniciando a cena...");
            OnButtonRestartScene();
        }
    }

    // --- LÓGICA DE TRANSIÇÃO DE CENA ---

    /// <summary>
    /// Corrotina para carregar a cena do menu após um delay (usando tempo real).
    /// </summary>
    private IEnumerator LoadMapSelectAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); 
        CleanupAndLoadScene(mapSelectSceneName);
    }

    /// <summary>
    /// Função centralizada para lidar com o carregamento de cenas.
    /// </summary>
    private void CleanupAndLoadScene(string sceneName)
    {
        Time.timeScale = 1.0f;
        isGameOver = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                // Host usa o SceneManager da rede para carregar a cena para todos.
                Debug.Log($"[GameManager] Host a carregar a cena '{sceneName}' para todos.");
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                // Cliente só pode voltar ao menu, o que o desconecta.
                if (sceneName == mapSelectSceneName)
                {
                    Debug.Log("[GameManager] Cliente a voltar ao menu. A desligar (Shutdown).");
                    NetworkManager.Singleton.Shutdown(); 
                    StartCoroutine(LoadSceneLocalAfterShutdown(sceneName));
                }
                else
                {
                    Debug.LogWarning($"[GameManager - Cliente] Tentativa de carregar cena '{sceneName}' ignorada.");
                }
            }
        }
        else // Modo Offline
        {
            if (CanLoadScene(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }   

    /// <summary>
    /// Corrotina auxiliar para garantir que a cena local seja carregada após o Shutdown do cliente.
    /// </summary>
    private IEnumerator LoadSceneLocalAfterShutdown(string sceneName)
    {
        yield return new WaitForSecondsRealtime(0.1f);
        if (CanLoadScene(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Verifica se uma cena pode ser carregada (existe nas Build Settings).
    /// </summary>
    private bool CanLoadScene(string sceneName)
    {
       if (SceneUtility.GetBuildIndexByScenePath(sceneName) < 0 && SceneManager.GetSceneByName(sceneName).buildIndex < 0)
       {
           Debug.LogError($"[GameManager] FALHA AO CARREGAR: A cena '{sceneName}' não está nas Build Settings ou o nome está incorreto!");
           return false;
       }
       return true;
    }
}
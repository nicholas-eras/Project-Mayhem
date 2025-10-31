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
    private GameObject gameOverPanel;
    private GameObject retryButton;

    // --- Memória Persistente do Lobby ---
    public SlotState[] LastLobbySlotStates { get; set; } = null;
    public int[] LastLobbySkinIDs { get; set; } = null;
    public ulong[] LastLobbyClientIds { get; set; } = null;

    private bool isGameOver = false; // Flag interna para evitar múltiplas chamadas
    private bool isRetrying = false; // Flag de segurança para evitar re-trigger do Game Over
    private WaveManager currentWaveManager; // Referência ao WaveManager da cena

    
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
        // Não precisamos mais limpar o evento do WaveManager aqui
    }

    /// <summary>
    /// Chamado automaticamente pelo Unity sempre que uma nova cena termina de carregar.
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Cena '{scene.name}' carregada.");

        // Reseta as flags de estado
        isGameOver = false;
        isRetrying = false; 

        if (scene.name == mapSelectSceneName)
        {
            // Se voltamos ao menu, limpa as referências da UI do jogo.
            gameOverPanel = null;
            retryButton = null;
            currentWaveManager = null; // Limpa a referência
        }
        else // Se carregámos QUALQUER outra cena (Jungle, Cyberpunk, etc.)
        {
            // Procura o painel de Game Over nessa nova cena.
            FindAndAssignGameOverPanel(scene);

            // Apenas encontra o WaveManager, não se inscreve mais em eventos dele
            currentWaveManager = FindObjectOfType<WaveManager>();
            if (currentWaveManager != null)
            {
                Debug.Log("[GameManager] WaveManager encontrado.");
            }
            else
            {
                Debug.LogWarning("[GameManager] Não encontrou WaveManager na cena.");
            }
        }
    }

    /// <summary>
    /// Procura o painel de Game Over e o botão de Retry na cena atual, USANDO TAGS.
    /// </summary>
    private void FindAndAssignGameOverPanel(Scene scene)
    {
        string panelTag = "GameOverPanel";
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
        Debug.LogWarning($"[GameManager] Carreguei '{scene.name}' mas não encontrei NENHUM objeto com Tag '{panelTag}'.");
        gameOverPanel = null;
    }

    // --- LÓGICA DE ESTADO DE JOGO (GAME OVER / VITÓRIA) ---

    /// <summary>
    /// Chamado EXCLUSIVAMENTE pelo PlayerManager quando o último jogador/bot morre.
    /// Decide se a opção de Tentar Novamente deve ser mostrada.
    /// </summary>
    public void TriggerGameOverFromPlayerManager()
    {
        Debug.Log($"[GameManager] !! TriggerGameOverFromPlayerManager CHAMADO !! -> isGameOver={isGameOver}, isRetrying={isRetrying}");

        if (isGameOver || isRetrying)
        {
            Debug.LogWarning("[GameManager] ...Chamada de Game Over IGNORADA.");
            return;
        }
        isGameOver = true;

        // 1. Pausa o jogo
        Time.timeScale = 0f;

        // 2. Verifica se existe um objeto ATIVO na cena com a Tag "GreaterBoss"
        bool isBossLevel = (GameObject.FindGameObjectWithTag("GreaterBoss") != null);
        Debug.Log($"[GameManager] ...É um nível de Boss? {isBossLevel}");

        // 3. Ativa a UI de Game Over
        if (gameOverPanel != null && isBossLevel)
        {
            Debug.Log("[GameManager] ...Mostrando painel de Game Over.");
            gameOverPanel.SetActive(true);

            if (retryButton != null)
            {
                retryButton.SetActive(true);
            }
        }
        else
        {
            Debug.Log("[GameManager] ...Nível normal ou UI nula. Voltando ao menu (delay).");
            StartCoroutine(LoadMapSelectAfterDelay(2.0f));
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

        StartCoroutine(LoadMapSelectAfterDelay(5.0f));
    }


    // --- FUNÇÕES CHAMADAS PELOS BOTÕES DA UI ---

    /// <summary>
    /// Chamado pelo botão "Voltar ao Menu" ou similar.
    /// </summary>
    public void OnButtonQuitToMenu()
    {
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
        Debug.Log("[GameManager] ==== BOTÃO 'TENTAR NOVAMENTE' PRESSIONADO ====");

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Cliente tentou dar Retry na wave. Apenas o Host pode fazer isso.");
            return; 
        }

        // --- INÍCIO DA CORREÇÃO ---
        // 1. Tenta usar a referência cacheada (que pegamos no OnSceneLoaded)
        if (currentWaveManager == null)
        {
            // 2. Se for nula (devido à Race Condition do OnSceneLoaded),
            //    procuramos DE NOVO, mas desta vez incluindo objetos INATIVOS.
            Debug.LogWarning("[GameManager] currentWaveManager estava nulo. Tentando busca profunda com FindObjectOfType<WaveManager>(true)...");
            currentWaveManager = FindObjectOfType<WaveManager>(true);
        }
        // --- FIM DA CORREÇÃO ---


        // 3. Agora checamos DE NOVO. Se AINDA for nulo, o WaveManager realmente não existe.
        if (currentWaveManager != null)
        {
            Debug.Log("[GameManager] WaveManager encontrado. Escondendo painel e restaurando o tempo.");
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            isGameOver = false; 
            Debug.Log("[GameManager] !! SETANDO isRetrying = true !!");
            isRetrying = true; 
            Time.timeScale = 1.0f; 

            // Inicia um timer para limpar a flag de retry.
            StartCoroutine(ClearRetryFlagAfterDelay(0.5f));

            // "Revive" todos
            Debug.Log("[GameManager] Ressuscitando jogadores...");
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
            
            Debug.Log("[GameManager] Chamando waveManager.RetryCurrentWave()...");
            currentWaveManager.RetryCurrentWave(); // Usa a variável de classe
            Debug.Log("[GameManager] ...Chamada para RetryCurrentWave() retornou.");
        }
        else
        {
            // Se chegamos aqui, a busca profunda (true) também falhou.
            Debug.LogError("[GameManager] Não foi possível Tentar Novamente. A busca profunda também falhou. WaveManager não encontrado! Reiniciando a cena...");
            OnButtonRestartScene();
        }
    }
    /// <summary>
    /// Limpa a flag 'isRetrying' após um delay fixo, em tempo real.
    /// Isso cria uma "janela de imunidade" contra o TriggerGameOver
    /// enquanto a nova onda está sendo processada.
    /// </summary>
    private IEnumerator ClearRetryFlagAfterDelay(float delay)
    {
        // Espera em TEMPO REAL, caso o Time.timeScale ainda esteja 0
        yield return new WaitForSecondsRealtime(delay);
        
        Debug.LogWarning($"[GameManager] !! FIM DO DELAY DE SEGURANÇA. Setando isRetrying = false. O jogo pode dar Game Over agora. !!");
        isRetrying = false;
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
                Debug.Log($"[GameManager] Host a carregar a cena '{sceneName}' para todos.");
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
            else if (NetworkManager.Singleton.IsClient)
            {
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
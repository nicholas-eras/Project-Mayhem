using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode; // Necessário para a lógica de rede (Host/Client)

/// <summary>
/// Gerenciador principal do estado do jogo. Controla o fluxo de Game Over,
/// Vitória, reinício de fase/onda e transições de cena.
/// É um Singleton que persiste entre as cenas.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // Singleton Instance

    [Header("Configuração de Cenas")]
    [Tooltip("O nome exato da sua cena de seleção de mapas/lobby.")]
    [SerializeField] private string mapSelectSceneName = "MapSelectScene";

    [Header("UI de Fim de Jogo (Opcional)")]
    [Tooltip("O painel principal que é ativado no Game Over (Pode ser nulo).")]
    [SerializeField] private GameObject gameOverPanel;
    [Tooltip("O GameObject do botão 'Tentar Novamente' (DEVE ser filho do gameOverPanel, pode ser nulo).")]
    [SerializeField] private GameObject retryButton;

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

    /// <summary>
    /// Chamado EXCLUSIVAMENTE pelo PlayerManager quando o último jogador/bot morre.
    /// Decide se a opção de Tentar Novamente deve ser mostrada.
    /// </summary>
    public void TriggerGameOverFromPlayerManager()
    {
        if (isGameOver) return; // Se já estamos em Game Over, não faz nada
        isGameOver = true;

        // 1. Pausa o jogo (usando tempo real para WaitForSecondsRealtime funcionar)
        Time.timeScale = 0f;

        // 2. Verifica se a onda que acabou era uma "Greater Boss Wave"
        WaveManager waveManager = FindObjectOfType<WaveManager>(); // Encontra o WaveManager na cena atual
        bool allowRetry = (waveManager != null && waveManager.IsCurrentWaveGreaterBoss);

        // 3. Ativa a UI de Game Over e configura o botão Retry
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true); // Mostra o painel principal
            if (retryButton != null)
            {
                retryButton.SetActive(allowRetry); // Mostra ou esconde o botão "Tentar Novamente"
            }
        }
        else
        {
            // Fallback: Se não há painel de UI, apenas volta ao menu após um delay
            Debug.LogWarning("Painel de Game Over (gameOverPanel) não configurado no GameManager. Voltando ao menu...");
            StartCoroutine(LoadMapSelectAfterDelay(3.0f));
        }
    }

     /// <summary>
     /// Chamado pelo WaveManager quando todas as ondas são completadas (Vitória).
     /// </summary>
    public void TriggerVictory()
    {
        if (isGameOver) return; // Não pode vencer se já perdeu
        isGameOver = true;
        Time.timeScale = 0f; // Pausa o jogo

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
        CleanupAndLoadScene(mapSelectSceneName);
    }

    /// <summary>
    /// Chamado pelo botão "Reiniciar Fase". Recarrega a cena atual do zero.
    /// Apenas o Host pode executar esta ação em um jogo de rede.
    /// </summary>
    public void OnButtonRestartScene()
    {
        // Em rede, apenas o Host pode reiniciar a cena para todos
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsHost)
        {
             Debug.LogWarning("Cliente tentou reiniciar a cena. Apenas o Host pode fazer isso.");
             return; // Clientes não fazem nada
        }
        CleanupAndLoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Chamado pelo botão "Tentar Novamente". Reinicia apenas a wave atual.
    /// Apenas o Host pode executar esta ação em um jogo de rede.
    /// </summary>
    public void OnButtonRetryWave()
    {
        // Em rede, apenas o Host pode reiniciar a wave
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsHost)
        {
             Debug.LogWarning("Cliente tentou dar Retry na wave. Apenas o Host pode fazer isso.");
             return; // Clientes não fazem nada
        }


        // Encontra os managers necessários na cena atual
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        // PlayerManager playerManager = PlayerManager.Instance; // Usando Singleton se disponível

        if (waveManager != null /*&& playerManager != null*/) // Verifica se encontrou o WaveManager
        {
            // Esconde o painel de Game Over e restaura o estado do jogo
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            isGameOver = false; // Permite que o jogo continue
            Time.timeScale = 1.0f; // Despausa o jogo

            // "Revive" todos os jogadores/bots
            // A forma mais segura é iterar pelos HealthSystems registrados no PlayerManager,
            // mas FindObjectsOfType é um fallback funcional (especialmente no Host).
            PlayerTargetable[] playersToReset = FindObjectsOfType<PlayerTargetable>(true); // 'true' para incluir inativos
            foreach(var playerTarget in playersToReset)
            {
                 // Reativa o GameObject caso o Die() o tenha desativado
                 if (!playerTarget.gameObject.activeSelf)
                 {
                     playerTarget.gameObject.SetActive(true);
                 }
                 HealthSystem hs = playerTarget.GetComponent<HealthSystem>();
                 if (hs != null)
                 {
                     hs.ResetForRetry(); // Chama a função de "reviver"
                 }
            }

            // Manda o WaveManager reiniciar a wave atual
            waveManager.RetryCurrentWave();
        }
        else
        {
            Debug.LogError("[GameManager] Não foi possível Tentar Novamente. WaveManager não encontrado! Reiniciando a cena como fallback...");
            // Se algo deu muito errado, apenas reinicia a cena inteira.
            OnButtonRestartScene();
        }
    }

    // --- LÓGICA DE TRANSIÇÃO DE CENA ---

    /// <summary>
    /// Corrotina para carregar a cena do menu após um delay (usando tempo real).
    /// </summary>
    private IEnumerator LoadMapSelectAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Espera mesmo com Time.timeScale = 0
        CleanupAndLoadScene(mapSelectSceneName);
    }

    /// <summary>
    /// Função centralizada para lidar com o carregamento de cenas,
    /// considerando se estamos offline, Host ou Cliente.
    /// </summary>
    private void CleanupAndLoadScene(string sceneName)
    {
        // Restaura o estado do jogo antes de mudar de cena
        Time.timeScale = 1.0f;
        isGameOver = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false); // Garante que a UI suma

        // Verifica o estado da rede
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // Estamos em uma sessão de rede ativa
            if (NetworkManager.Singleton.IsHost)
            {
                // Host usa o SceneManager da rede para carregar a cena para todos
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                // Cliente voltando para o menu principal?
                 if (sceneName == mapSelectSceneName)
                 {
                      NetworkManager.Singleton.Shutdown(); // Cliente se desconecta
                      // Carrega a cena do menu localmente APÓS desconectar
                      // (Necessário porque o Shutdown pode levar um frame)
                      StartCoroutine(LoadSceneLocalAfterShutdown(sceneName));
                 }
                 else
                 {
                      // Cliente NUNCA deve tentar carregar uma cena de jogo diretamente
                      Debug.LogWarning($"[GameManager - Cliente] Tentativa de carregar cena '{sceneName}' ignorada. Apenas o Host pode mudar a cena do jogo.");
                 }
            }
        }
        else // Modo Offline (Single Player)
        {
            // Usa o SceneManager normal do Unity
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
        // Espera um pequeno tempo para o Shutdown processar
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
         // Application.CanStreamedLevelBeLoaded foi descontinuado.
         // A forma correta é tentar carregar e capturar a exceção,
         // ou verificar se o índice da cena é válido.
         // Solução simples: Apenas logar erro se falhar.
         if (SceneUtility.GetBuildIndexByScenePath(sceneName) < 0 && SceneManager.GetSceneByName(sceneName).buildIndex < 0)
         {
              Debug.LogError($"[GameManager] FALHA AO CARREGAR: A cena '{sceneName}' não está nas Build Settings ou o nome está incorreto!");
              return false;
         }
         return true;
    }
}
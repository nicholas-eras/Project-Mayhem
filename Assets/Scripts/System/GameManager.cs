using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string mapSelectSceneName = "MapSelectScene";
    
    // --- NOVO: Referência para o painel de "Tentar Novamente" ---
    [Header("UI de Fim de Jogo")]
    [Tooltip("Painel com opções 'Reiniciar' e 'Tentar Novamente' para boss")]
    [SerializeField] private GameObject gameOverOptionsPanel; 
    
    // --- NOVO: Tag para encontrar o jogador ---
    [SerializeField] private string playerTag = "Player";

    public static GameManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- MUDANÇA: GameOver agora recebe um parâmetro ---
    public void GameOver(bool wasGreaterBossWave)
    {
        Time.timeScale = 0f;

        // Esconde o painel de opções por padrão, caso esteja vindo de outra tela
        if (gameOverOptionsPanel != null)
        {
            gameOverOptionsPanel.SetActive(false);
        }
        
        if (wasGreaterBossWave)
        {
            // É uma wave de boss! Mostra as opções.
            if (gameOverOptionsPanel != null)
            {
                gameOverOptionsPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("[GM] Painel 'gameOverOptionsPanel' não foi configurado no Inspector!");
                // Se o painel não existe, usa a lógica antiga como fallback
                StartCoroutine(LoadMapSelectAfterDelay(3.0f));
            }
        }
        else
        {
            // Morte normal (não-boss), volta para o menu.
            StartCoroutine(LoadMapSelectAfterDelay(3.0f));
        }
    }

    private IEnumerator LoadMapSelectAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); 

        // Limpa os managers específicos da cena
        DestroySceneManagers();
        
        Time.timeScale = 1.0f;

        bool canLoad = Application.CanStreamedLevelBeLoaded(mapSelectSceneName);

        if (canLoad)
        {
            SceneManager.LoadScene(mapSelectSceneName);
        }
        else
        {
            Debug.LogError($"[GM] FALHA: a cena '{mapSelectSceneName}' não pode ser carregada.");
        }
    }
    
    // --- NOVO: Método para o botão "Reiniciar" (Carrega a cena do zero) ---
    public void OnButtonRestartScene()
    {
        // Garante que o painel suma
        if (gameOverOptionsPanel != null)
        {
            gameOverOptionsPanel.SetActive(false);
        }

        // Limpa os managers de cena (WaveManager, UpgradeManager)
        DestroySceneManagers();
        
        Time.timeScale = 1.0f;
        
        // Recarrega a cena atual
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- NOVO: Método para o botão "Tentar Novamente" (Repete a wave) ---
    public void OnButtonRetryWave()
    {
        // 1. Encontra os componentes necessários na cena
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        HealthSystem playerHealth = null;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<HealthSystem>();
        }

        // 2. Verifica se encontrou tudo
        if (waveManager != null && playerHealth != null)
        {
            // 3. Esconde o painel e restaura o tempo
            if (gameOverOptionsPanel != null)
            {
                gameOverOptionsPanel.SetActive(false);
            }
            Time.timeScale = 1.0f;

            // 4. Manda os sistemas se resetarem
            // O HealthSystem do jogador precisa de um método para "reviver"
            playerHealth.ResetForRetry(); 
            
            // O WaveManager precisa de um método para reiniciar a wave atual
            waveManager.RetryCurrentWave(); 
        }
        else
        {
            Debug.LogError("[GM] Não foi possível Tentar Novamente. Player ou WaveManager não encontrados! Reiniciando a cena...");
            // Fallback: se algo der errado, apenas reinicia a cena.
            OnButtonRestartScene();
        }
    }
    
    // --- NOVO: Lógica de limpeza refatorada ---
    private void DestroySceneManagers()
    {
        // Encontra e destrói os managers que SÓ devem existir na cena de jogo
        UpgradeManager upgradeManager = FindObjectOfType<UpgradeManager>();
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        
        if (upgradeManager != null)
        {
            Destroy(upgradeManager.gameObject);
        }
        if (waveManager != null)
        {
            Destroy(waveManager.gameObject);
        }
    }
}
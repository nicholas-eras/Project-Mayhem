using UnityEngine;
using UnityEngine.UI;

// Coloque este script no seu 'gameOverPanel' (o objeto pai da UI de Game Over)
public class GameOverUIConnector : MonoBehaviour
{
    [Header("Referências (Arraste os botões aqui)")]
    [Tooltip("O botão 'Tentar Novamente'")]
    [SerializeField] private Button retryButton;
    
    [Tooltip("O botão 'Voltar ao Menu'")]
    [SerializeField] private Button quitToMenuButton;

    [SerializeField] private Button restartSceneButton;

    void Start()
    {
        // Garante que os botões estão ouvindo os eventos corretos
        // do Singleton do GameManager (que veio da outra cena)
        if (GameManager.Instance == null)
        {
            Debug.LogError("[GameOverUIConnector] Não foi possível encontrar o GameManager.Instance! Os botões de Game Over não vão funcionar.");
            return;
        }

        // Conecta os botões programaticamente
        if (retryButton != null)
        {
            // Limpa qualquer listener antigo (do Inspector) e adiciona o correto
            retryButton.onClick.RemoveAllListeners(); 
            retryButton.onClick.AddListener(GameManager.Instance.OnButtonRetryWave);
        }

        if (quitToMenuButton != null)
        {
            quitToMenuButton.onClick.RemoveAllListeners();
            quitToMenuButton.onClick.AddListener(GameManager.Instance.OnButtonQuitToMenu);
        }
        
        if (restartSceneButton != null)
        {
            restartSceneButton.onClick.RemoveAllListeners();
            restartSceneButton.onClick.AddListener(GameManager.Instance.OnButtonRestartScene);
        }
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSelectionManager : MonoBehaviour
{
    [Header("Configuração de Dados")]
    [SerializeField] private MapDatabase mapDatabase;

    [Header("Configuração de UI")]
    [SerializeField] private GameObject mapCardPrefab;
    [SerializeField] private Transform cardContainer;

    [Header("Painéis de UI")]
    [SerializeField] private GameObject modeSelectionPanel;
    [SerializeField] private GameObject mapSelectionPanel;
    
    // --- NOVAS VARIÁVEIS ---
    [Header("Painéis de UI Multiplayer")] // <-- NOVO
    [SerializeField] private GameObject multiplayerPanel; // <-- NOVO
    [SerializeField] private GameObject lobbyPanel; // <-- NOVO
    // --- FIM DAS NOVAS VARIÁVEIS ---

    void Start()
    {
        if (mapDatabase == null)
        {
            Debug.LogError("Map Database não está configurado!");
            return;
        }

        // Prepara o carrossel de mapas, mesmo que escondido
        PopulateMapCarousel();

        if (modeSelectionPanel != null)
        {
            // Começa mostrando APENAS a seleção de modo
            modeSelectionPanel.SetActive(true); // <-- GARANTIDO
            mapSelectionPanel.SetActive(false);
            
            // Garante que os novos painéis também comecem desligados
            if (multiplayerPanel != null) multiplayerPanel.SetActive(false); // <-- NOVO
            if (lobbyPanel != null) lobbyPanel.SetActive(false); // <-- NOVO
        }
    }

    // --- LÓGICA DE BOTÕES ---

    /// <summary>
    /// Chamado pelo botão "Um Jogador".
    /// </summary>
    public void OnSinglePlayerSelected()
    {
        // Define o modo de jogo (se você tiver o GameModeManager)
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetGameMode(GameMode.SinglePlayer);
        }

        // Mostra o carrossel de mapas
        modeSelectionPanel.SetActive(false);
        mapSelectionPanel.SetActive(true);
    }

    /// <summary>
    /// Chamado pelo botão "Multijogador".
    /// </summary>
    public void OnMultiplayerSelected() // <-- LÓGICA MODIFICADA
    {
        // Define o modo de jogo
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetGameMode(GameMode.Multiplayer);
        }
        
        // NÃO mostra mais o carrossel. Mostra o painel "Hospedar/Entrar".
        modeSelectionPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }

    /// <summary>
    /// Chamado pelo botão "Iniciar Jogo" (de dentro do Lobby).
    /// </summary>
    public void OnStartFromLobbyButtonSelected() // <-- NOVO
    {
        // Agora sim, o Host (que está no lobby) verá o carrossel para escolher o mapa
        lobbyPanel.SetActive(false);
        mapSelectionPanel.SetActive(true);
        
        // (Aqui, no futuro, o Host avisará os Clientes sobre qual mapa carregar)
    }


    // --- LÓGICA DE BOTÕES "VOLTAR" ---

    /// <summary>
    /// Chamado pelo botão "Voltar" do painel Hospedar/Entrar.
    /// </summary>
    public void BackToModeSelection() // <-- NOME MUDADO (era BackToModeSelection)
    {
        multiplayerPanel.SetActive(false);
        mapSelectionPanel.SetActive(false); // Garante que tudo está desligado
        lobbyPanel.SetActive(false);
        
        modeSelectionPanel.SetActive(true); // Volta para a tela inicial
    }

    /// <summary>
    /// Chamado pelo botão "Voltar" do Lobby.
    /// </summary>
    public void BackToMultiplayerPanel() // <-- NOVO
    {
        lobbyPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }
    
    public void QuitGame()
    {
        // 1. Comando principal para fechar a aplicação (funciona em builds)
        Application.Quit();

        // 2. Comando para parar a execução SE você estiver no Editor da Unity.
        // Isso facilita o teste do botão.
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    private void PopulateMapCarousel()
    {
        // Limpa qualquer UI antiga
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var entry in mapDatabase.maps)
        {
            // 1. Cria o objeto MapData (temporário) para a carta
            MapData mapData = ScriptableObject.CreateInstance<MapData>();
            mapData.mapName = entry.mapName;
            mapData.gameScenePath = entry.scenePath;
            mapData.mapSprite = entry.mapImage;
            
            // 2. Instancia a carta de UI
            GameObject cardInstance = Instantiate(mapCardPrefab, cardContainer);
            
            // 3. Configura o cartão
            MapSelectorUI cardUI = cardInstance.GetComponent<MapSelectorUI>();
            if (cardUI != null)
            {
                cardUI.Setup(mapData);
            }
            else
            {
                Debug.LogError("O Prefab do Map Card não tem o script MapSelectorUI!");
            }
        }
    }

    // Função para ser ligada ao botão "Voltar" (Opcional)
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // Altere para o nome da sua cena de menu
    }
}
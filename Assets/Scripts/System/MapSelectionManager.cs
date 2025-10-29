using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode; // <-- Adicione isto

public class MapSelectionManager : MonoBehaviour
{
    [Header("Configuração de Dados")]
    [SerializeField] private MapDatabase mapDatabase;

    [Header("Configuração de UI")]
    [SerializeField] private GameObject mapCardPrefab;
    [SerializeField] private Transform cardContainer;

    [Header("Painéis de UI (Todos)")]
    [SerializeField] private GameObject modeSelectionPanel;
    [SerializeField] private GameObject mapSelectionPanel;
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject waitingPanel; // <-- Painel "Aguardando..."

    [Header("Managers")]
    [SerializeField] private LobbyManager lobbyManager; // <-- Referência ao Lobby de rede

    void Awake()
    {
        // Tenta encontrar o LobbyManager de rede
        if (lobbyManager == null)
        {
            lobbyManager = FindObjectOfType<LobbyManager>();
        }
    }

    // --- ESTE É O NOVO MÉTODO START() COMBINADO ---
    void Start()
    {
        // 1. Prepara o carrossel (sempre)
        PopulateMapCarousel();

        // 2. Verifica o estado da rede (Lógica do NetworkConnect)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                // ESTÁ A VOLTAR COMO HOST
                Debug.Log("MapSelectionManager: Voltando ao Lobby como Host.");
                modeSelectionPanel.SetActive(false);
                mapSelectionPanel.SetActive(false);
                multiplayerPanel.SetActive(false);
                if (waitingPanel != null) waitingPanel.SetActive(false);
                
                lobbyPanel.SetActive(true); // Mostra o Lobby diretamente
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                // ESTÁ A VOLTAR COMO CLIENTE
                Debug.Log("MapSelectionManager: Voltando ao Lobby como Cliente.");
                modeSelectionPanel.SetActive(false);
                mapSelectionPanel.SetActive(false);
                multiplayerPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                
                if (waitingPanel != null) waitingPanel.SetActive(true);
            }
        }
        else
        {
            // 3. ESTADO OFFLINE (Lógica do MapSelectionManager original)
            Debug.Log("MapSelectionManager: Iniciando offline.");
            modeSelectionPanel.SetActive(true); // <-- Mostra o painel inicial
            mapSelectionPanel.SetActive(false);
            multiplayerPanel.SetActive(false);
            lobbyPanel.SetActive(false);
            if (waitingPanel != null) waitingPanel.SetActive(false);
        }
    }

    // --- LÓGICA DE BOTÕES (Modo) ---

    public void OnSinglePlayerSelected()
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetGameMode(GameMode.SinglePlayer);
        }
        modeSelectionPanel.SetActive(false);
        mapSelectionPanel.SetActive(true);
    }

    public void OnMultiplayerSelected()
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetGameMode(GameMode.Multiplayer);
        }
        modeSelectionPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }
    
    // --- LÓGICA DE BOTÕES (Rede - do NetworkConnect) ---
    
    public void OnClickHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            multiplayerPanel.SetActive(false);
            lobbyPanel.SetActive(true); 
        }
        else
        {
            Debug.LogError("Falha ao iniciar Host!");
            NetworkManager.Singleton.Shutdown(); 
            multiplayerPanel.SetActive(true);
            lobbyPanel.SetActive(false);
        }
    }
    
    public void OnClickJoin()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            multiplayerPanel.SetActive(false); 
            if (waitingPanel != null)
            {
                waitingPanel.SetActive(true);
            }
        }
        else
        {
            Debug.LogError("Falha ao conectar como Cliente!");
        }
    }

    // --- LÓGICA DE BOTÕES (Lobby) ---

    public void OnStartFromLobbyButtonSelected()
    {
        lobbyPanel.SetActive(false);
        mapSelectionPanel.SetActive(true);
    }

    // --- LÓGICA DE BOTÕES "VOLTAR" ---

    public void BackToModeSelection()
    {
        multiplayerPanel.SetActive(false);
        mapSelectionPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void BackToMultiplayerPanel()
    {
        lobbyPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }

    // --- O RESTO DO SEU SCRIPT (QuitGame, PopulateMapCarousel, etc.) ---

    public void QuitGame()
    {
        Application.Quit();
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
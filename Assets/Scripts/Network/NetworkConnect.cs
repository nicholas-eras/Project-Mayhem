using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkConnect : MonoBehaviour
{
    [Header("Painéis de UI")]
    [SerializeField] private GameObject multiplayerPanel; // O menu "Hospedar/Entrar"
    [SerializeField] private GameObject lobbyPanel; // O lobby com os 4 slots
    [SerializeField] private GameObject mapSelectionPanel; 
    
    // --- NOVO: Adicione este painel ---
    [Tooltip("Painel 'Aguardando Host...' para clientes que voltam ao lobby")]
    [SerializeField] private GameObject waitingPanel; 

    [Header("Managers")]
    [SerializeField] private LobbyManager lobbyManager; 

    void Awake() 
    {
        if (lobbyManager == null)
        {
            // Tenta encontrar o LobbyManager_Networked
            lobbyManager = FindObjectOfType<LobbyManager>(); 
        }
    }
    
    // --- ESTA É A FUNÇÃO QUE FALTA ---
    void Start()
    {
        // Verifica se já estamos conectados quando esta cena (Menu/Lobby) carrega
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                // Estamos a voltar ao Lobby como Host
                Debug.Log("NetworkConnect: Voltando ao Lobby como Host.");
                multiplayerPanel.SetActive(false);
                mapSelectionPanel.SetActive(false); 
                if (waitingPanel != null) waitingPanel.SetActive(false);
                
                lobbyPanel.SetActive(true); // Mostra o Lobby diretamente
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                // Estamos a voltar ao Lobby como Cliente
                Debug.Log("NetworkConnect: Voltando ao Lobby como Cliente.");
                multiplayerPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                mapSelectionPanel.SetActive(false);
                
                // Mostra um painel "Aguardando Host..."
                if (waitingPanel != null)
                {
                    waitingPanel.SetActive(true); 
                }
            }
        }
        else
        {
            // Estado padrão (offline) - Mostra o menu inicial
            multiplayerPanel.SetActive(true);
            lobbyPanel.SetActive(false);
            mapSelectionPanel.SetActive(false);
            if (waitingPanel != null) waitingPanel.SetActive(false);
        }
    }
    // --- FIM DA FUNÇÃO START ---
    
    
    /// <summary>
    /// Chamado pelo botão "Hospedar"
    /// </summary>
    public void OnClickHost()
    {
        // Esta função agora só será chamada se estivermos offline
        if (NetworkManager.Singleton.StartHost())
        {
            multiplayerPanel.SetActive(false);
            lobbyPanel.SetActive(true); 
            // Não precisamos mais do SetupLobbyForHost() aqui,
            // o OnNetworkSpawn do LobbyManager trata disso.
        }
        else
        {
            Debug.LogError("Falha ao iniciar Host!");
            NetworkManager.Singleton.Shutdown(); 
            multiplayerPanel.SetActive(true);
            lobbyPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Chamado pelo botão "Entrar"
    /// </summary>
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

    /// <summary>
    /// Chamado pelo botão "Iniciar Jogo" (do Lobby)
    /// </summary>
    public void OnClickStartFromLobby()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        
        lobbyPanel.SetActive(false);
        mapSelectionPanel.SetActive(true);
    }
}
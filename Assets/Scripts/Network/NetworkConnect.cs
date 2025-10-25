using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkConnect : MonoBehaviour
{
    [Header("Painéis de UI")]
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject mapSelectionPanel; // <-- NOVO


    // --- NOVA REFERÊNCIA ---
    [Header("Managers")]
    [SerializeField] private LobbyManager lobbyManager; // <-- Arraste o LobbyPanel aqui
    // --- FIM ---

    void Awake() // <-- Adicione Awake para pegar o LobbyManager se não arrastou
    {
        if (lobbyManager == null)
        {
            lobbyManager = FindObjectOfType<LobbyManager>(); // Assume que só há um
        }
    }
    
    /// <summary>
    /// Chamado pelo botão "Hospedar"
    /// </summary>
    public void OnClickHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            multiplayerPanel.SetActive(false);
            lobbyPanel.SetActive(true); // Mostra o Lobby (com os slots)
            // --- AQUI ESTÁ A MUDANÇA ---
            // Diz ao LobbyManager para configurar o estado inicial AGORA
            if (lobbyManager != null)
            {
                lobbyManager.SetupLobbyForHost();
            }
            else
            {
                 Debug.LogError("LobbyManager não encontrado! A UI do Lobby não será configurada.");
            }
            // --- FIM DA MUDANÇA ---
        }
        else
        {
            Debug.LogError("Falha ao iniciar Host!");
        }
    }

    /// <summary>
    /// Chamado pelo botão "Entrar"
    /// </summary>
    public void OnClickJoin()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Cliente conectado!");
            // O cliente não faz nada, ele espera o Host mudar a cena.
            // Poderíamos mostrar um painel "Conectando..."
            multiplayerPanel.SetActive(false); 
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
        // Só o Host pode fazer isso
        if (!NetworkManager.Singleton.IsHost) return;
        
        // Esconde o lobby e mostra a seleção de mapa PARA O HOST
        lobbyPanel.SetActive(false);
        mapSelectionPanel.SetActive(true);
    }
    
    // (O MapSelectorUI.cs, que é clicado no final, precisa ser modificado)
}
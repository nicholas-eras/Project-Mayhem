using UnityEngine;
using Unity.Netcode;

public class ConnectionManager : MonoBehaviour
{
    private NetworkManager manager;

    void Start()
    {
        manager = GetComponent<NetworkManager>();
        if (manager == null)
        {
            Debug.LogError("Este script precisa estar no mesmo objeto do NetworkManager!");
            return;
        }
        
        // "Escuta" o evento de um novo cliente tentando se conectar
        manager.ConnectionApprovalCallback += ApproveConnection;
    }

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Esta função é chamada no Host sempre que um Cliente tenta entrar.

        // Lógica de aprovação (ex: checar senha, checar se o lobby está cheio)
        // Por enquanto, vamos aprovar todo mundo.
        
        response.Approved = true;       // Sim, o jogador pode entrar.
        response.CreatePlayerObject = false;  // NÃO, não crie o 'Player Prefab' ainda!
                                            // Nós vamos spawná-lo manualmente DEPOIS que a cena do jogo carregar.
        
        response.Pending = false;       // Finaliza a aprovação.
    }
}
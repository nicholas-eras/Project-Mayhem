using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class GameSceneManager : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Arraste seu 'PlayerAgent_Prefab' aqui.")]
    [SerializeField] private GameObject playerAgentPrefab; // Usado para HOST e BOTS
    private GameObject mapSpecificWeapon;
    private List<Transform> spawnPoints = new List<Transform>();
    // Removido: private int nextSpawnIndex = 0;
    // Removido: private int nextBotSpawnIndex = 1;
    // Usaremos o slotIndex para determinar o spawn point

    void Awake()
    {
        // --- PEGA A ARMA DO MAPA (JÁ DEVE ESTAR AQUI) ---
        if (MapDataManager.Instance != null)
        {
            mapSpecificWeapon = MapDataManager.Instance.startingWeaponPrefab;
        }
        else
        {
            Debug.LogError("[GameSceneManager] MapDataManager.Instance não encontrado!");
        }

        // Encontra e ordena os pontos de spawn P1, P2, P3, P4...
        PlayerSpawn[] foundPoints = FindObjectsOfType<PlayerSpawn>();
        spawnPoints = foundPoints
            .OrderBy(p => p.name) // Ordena alfabeticamente (P1, P2...)
            .Select(p => p.transform)
            .ToList();

        if (spawnPoints.Count == 0) Debug.LogError("Nenhum 'PlayerSpawn' (script marcador) encontrado nesta cena!");
    }

    void Start()
    {
        // Apenas o Host/Servidor executa a lógica de spawn
        if (NetworkManager.Singleton.IsServer)
        {
            if (LobbyData.SlotStates == null)
            {
                Debug.LogError("GameSceneManager: LobbyData.SlotStates é nulo! Dados do lobby não foram passados. Abortando spawn.");
                return;
            }
            if (playerAgentPrefab == null)
            {
                Debug.LogError("GameSceneManager: Player Agent Prefab não configurado!");
                return;
            }

            // Chama a nova função principal de spawn
            SpawnPlayersAndBots();
            // StartCoroutine(StartWaveAfterDelay(0.2f)); // 0.2s é geralmente suficiente
        }
    }

    /// <summary>
    /// (APENAS HOST) Spawna o Jogador Host e quaisquer Bots definidos no LobbyData.
    /// </summary>
    private void SpawnPlayersAndBots()
    {
        // Itera pelos slots definidos no LobbyData
        for (int i = 0; i < LobbyData.SlotStates.Length; i++)
        {
            SlotState state = LobbyData.SlotStates[i];
            int skinId = LobbyData.SkinIDs[i];

            // Pega o ponto de spawn correspondente ao slot (P1=índice 0, P2=índice 1, ...)
            Transform spawnPoint = GetSpawnPointForIndex(i);
            if (spawnPoint == null)
            {
                Debug.LogError($"Não foi possível encontrar um Spawn Point para o Slot {i}. Pulando.");
                continue;
            }

            // Decide o que fazer com base no estado do slot
            switch (state)
            {
                case SlotState.Human:
                    // --- SPAWN DO JOGADOR HUMANO ---
                    // No seu caso (Host jogando sozinho), só o i=0 entrará aqui.
                    // Precisamos saber qual Client ID corresponde a este slot.
                    // Para o Host, o Client ID é sempre NetworkManager.Singleton.LocalClientId
                    // TODO: Para múltiplos jogadores, precisaremos mapear Slot Index -> Client ID
                    if (i == 0) // Assume Host
                    {
                        ulong hostClientId = NetworkManager.Singleton.LocalClientId;
                        // --- PASSA O ÍNDICE DO SLOT PARA RENOMEAR ---
                        SpawnPlayerObject(hostClientId, skinId, spawnPoint, i);
                    }
                    else
                    {
                        // Lógica para encontrar o Client ID do jogador no slot 'i' (requer mais info do lobby)
                    }
                    break;

                case SlotState.Bot:
                    // --- SPAWN DO BOT ---
                    SpawnBotObject(skinId, spawnPoint, i);
                    break;

                case SlotState.Empty:
                    // Não faz nada
                    break;
            }
        }
    }

    /// <summary>
    /// (APENAS HOST) Spawna o NetworkObject para um jogador humano específico.
    /// </summary>
    private void SpawnPlayerObject(ulong clientId, int skinID, Transform spawnPoint, int slotIndex)
    {
         GameObject playerGO = Instantiate(playerAgentPrefab, spawnPoint.position, spawnPoint.rotation);
         NetworkObject netObj = playerGO.GetComponent<NetworkObject>();

         if (netObj != null)
         {
              // --- RENOMEIA O OBJETO ---
              playerGO.name = $"Player_Client[{clientId}]_Slot[{slotIndex}]";
              // --- FIM DA RENOMEAÇÃO ---

              netObj.SpawnAsPlayerObject(clientId, true);

              AgentManager agent = playerGO.GetComponent<AgentManager>();
              if (agent != null)
              {
                   agent.InitializeAgent(isBot: false, skinID: skinID);
              }
         }
         else
         {
              Debug.LogError("PlayerAgent_Prefab não tem NetworkObject!");
              Destroy(playerGO);
         }
    }

/// <summary>
    /// (APENAS HOST) Spawna um Bot na rede.
    /// </summary>
    private void SpawnBotObject(int skinID, Transform spawnPoint, int slotIndex)
    {
        GameObject botGO = Instantiate(playerAgentPrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkObject netObj = botGO.GetComponent<NetworkObject>();

        if (netObj != null)
        {
             botGO.name = $"Player_Bot_Slot[{slotIndex}]";
             
             // --- NOVO PASSO: SETAR ESTADO INICIAL DO BOT NO AGENTMANAGER ---
             AgentManager agent = botGO.GetComponent<AgentManager>();
             if (agent != null)
             {
                 // CHAMA A FUNÇÃO DE INICIALIZAÇÃO AGORA
                 // Isso define o estado IsBot ANTES que o OnNetworkSpawn rode
                 // (Embora não use a NetworkVariable, a lógica de OnNetworkSpawn pode ler)
                 // NOTE: Isto é um hack de Netcode.
                 agent.SetupBotInitialState(skinID); // <-- NOVO MÉTODO NO AGENTMANAGER
             }

             netObj.Spawn(true); // Spawna o objeto
             
             // A chamada InitializeAgent é agora redundante/incorreta (deve ser chamada APENAS no Bot)
             // Remova a chamada subsequente ao InitializeAgent
             // agent.InitializeAgent(isBot: true, skinID: skinID); 
        }
        else { /* Error */ return; }

        // --- CORREÇÃO: Forçar o IsBot no OnNetworkSpawn do BOT ---
        // Isso é complexo. Vamos usar a lógica mais simples:
        
        // NOVO: Chamada de setup de arma movida para o SpawnPlayerObject para o Bot também
        PlayerWeaponManager botWeaponManager = botGO.GetComponent<PlayerWeaponManager>();
        if (botWeaponManager != null)
        {
            botWeaponManager.InitializeStartingWeapon(mapSpecificWeapon);
        }
    }

    /// <summary>
    /// Retorna o Transform do ponto de spawn correspondente ao índice do slot.
    /// </summary>
    private Transform GetSpawnPointForIndex(int index)
    {
        if (spawnPoints.Count == 0) return null; // Nenhum ponto encontrado

        // Garante que o índice seja válido e use um fallback se não houver pontos suficientes
        int spawnIndex = Mathf.Clamp(index, 0, spawnPoints.Count - 1);
        return spawnPoints[spawnIndex];
    }
}
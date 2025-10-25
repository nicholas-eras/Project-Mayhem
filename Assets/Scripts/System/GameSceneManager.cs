using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class GameSceneManager : MonoBehaviour
{
    public static System.Action OnAllPlayersSpawned; // <- NOVO EVENTO

    [Header("Configuração")]
    [SerializeField] private GameObject playerAgentPrefab;
    private GameObject mapSpecificWeapon;
    private List<Transform> spawnPoints = new List<Transform>();

    void Awake()
    {
        // --- PEGA A ARMA DO MAPA ---
        if (MapDataManager.Instance != null)
        {
            mapSpecificWeapon = MapDataManager.Instance.startingWeaponPrefab;
        }
        else
        {
            Debug.LogError("[GameSceneManager] MapDataManager.Instance não encontrado!");
        }

        // Encontra e ordena os pontos de spawn
        PlayerSpawn[] foundPoints = FindObjectsOfType<PlayerSpawn>();
        spawnPoints = foundPoints
            .OrderBy(p => p.name)
            .Select(p => p.transform)
            .ToList();

        if (spawnPoints.Count == 0) Debug.LogError("Nenhum 'PlayerSpawn' encontrado nesta cena!");
    }

    void Start()
    {
        // Apenas o Host/Servidor executa a lógica de spawn
        if (NetworkManager.Singleton.IsServer)
        {
            if (LobbyData.SlotStates == null)
            {
                Debug.LogError("GameSceneManager: LobbyData.SlotStates é nulo! Abortando spawn.");
                return;
            }
            if (playerAgentPrefab == null)
            {
                Debug.LogError("GameSceneManager: Player Agent Prefab não configurado!");
                return;
            }

            SpawnPlayersAndBots();
            
            // *** NOVO: Espera um frame e notifica que terminou o spawn ***
            StartCoroutine(NotifyPlayersSpawned());
        }
    }

    // *** NOVO: Corrotina para notificar que todos os jogadores foram spawnados ***
    private IEnumerator NotifyPlayersSpawned()
    {
        // Espera um frame para garantir que todos os NetworkObjects foram spawnados
        yield return null;
        
        OnAllPlayersSpawned?.Invoke();
    }

    /// <summary>
    /// (APENAS HOST) Spawna o Jogador Host e quaisquer Bots definidos no LobbyData.
    /// </summary>
    private void SpawnPlayersAndBots()
    {
        for (int i = 0; i < LobbyData.SlotStates.Length; i++)
        {
            SlotState state = LobbyData.SlotStates[i];
            int skinId = LobbyData.SkinIDs[i];

            Transform spawnPoint = GetSpawnPointForIndex(i);
            if (spawnPoint == null)
            {
                Debug.LogError($"Não foi possível encontrar um Spawn Point para o Slot {i}. Pulando.");
                continue;
            }

            switch (state)
            {
                case SlotState.Human:
                    if (i == 0) // Assume Host
                    {
                        ulong hostClientId = NetworkManager.Singleton.LocalClientId;
                        SpawnPlayerObject(hostClientId, skinId, spawnPoint, i);
                    }
                    break;

                case SlotState.Bot:
                    SpawnBotObject(skinId, spawnPoint, i);
                    break;

                case SlotState.Empty:
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
              playerGO.name = $"Player_Client[{clientId}]_Slot[{slotIndex}]";
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

    // No GameSceneManager, modifique o SpawnBotObject:

// No GameSceneManager, modifique o SpawnBotObject:

private void SpawnBotObject(int skinID, Transform spawnPoint, int slotIndex)
{
    GameObject botGO = Instantiate(playerAgentPrefab, spawnPoint.position, spawnPoint.rotation);
    NetworkObject netObj = botGO.GetComponent<NetworkObject>();

    if (netObj != null)
    {
        botGO.name = $"Player_Bot_Slot[{slotIndex}]";
        
        // *** CONFIGURAÇÃO DO BOT ANTES DO SPAWN ***
        AgentManager agent = botGO.GetComponent<AgentManager>();
        if (agent != null)
        {
            // Marca como bot ANTES do spawn (usando método temporário)
            agent.SetupBotInitialState(skinID);
        }

        // *** SPAWNA O BOT ***
        netObj.Spawn(true);
        
        // *** CONFIGURAÇÃO PÓS-SPAWN ***
        if (agent != null && NetworkManager.Singleton.IsServer) // <- CORREÇÃO AQUI
        {
            // Agora que o NetworkObject foi spawnado, podemos configurar as NetworkVariables
            StartCoroutine(ConfigureBotAfterSpawn(agent, skinID));
        }

        // Configura arma do bot
        PlayerWeaponManager botWeaponManager = botGO.GetComponent<PlayerWeaponManager>();
        if (botWeaponManager != null)
        {
            botWeaponManager.InitializeStartingWeapon(mapSpecificWeapon);
        }
    }
    else 
    { 
        Debug.LogError("Falha ao spawnar bot: NetworkObject nulo");
        Destroy(botGO);
    }
}

    // *** NOVO: Corrotina para configurar bot após spawn ***
    private IEnumerator ConfigureBotAfterSpawn(AgentManager agent, int skinID)
    {
        // Espera um frame para garantir que o NetworkObject está completamente spawnado
        yield return null;

        // Agora podemos configurar as NetworkVariables com segurança
        if (agent != null)
        {
            agent.InitializeAgent(isBot: true, skinID: skinID);
        }
    }

    /// <summary>
    /// Retorna o Transform do ponto de spawn correspondente ao índice do slot.
    /// </summary>
    private Transform GetSpawnPointForIndex(int index)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("Nenhum spawn point encontrado!");
            return null;
        }

        // Se temos spawn points suficientes, usa o correspondente ao índice
        if (index < spawnPoints.Count)
        {
            Debug.Log($"[GameSceneManager] Usando spawn point {spawnPoints[index].name} para slot {index}");
            return spawnPoints[index];
        }
        else
        {
            // Se não há spawn point suficiente, usa o primeiro disponível
            Debug.LogWarning($"[GameSceneManager] Slot {index} não tem spawn point específico. Usando {spawnPoints[0].name}");
            return spawnPoints[0];
        }
    }
}
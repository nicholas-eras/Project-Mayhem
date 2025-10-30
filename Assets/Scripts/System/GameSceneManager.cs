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
    public static bool AllPlayersSpawned { get; private set; }
    
    // --- ADICIONE ESTAS DUAS LINHAS ---
    [Header("UI (Barras de Vida)")]
    [Tooltip("Arraste aqui o seu Prefab da Barra de Vida (o que tem o script FollowTargetUI).")]
    [SerializeField] private GameObject healthBarPrefab;
    [Tooltip("Arraste aqui o seu Canvas principal da cena de jogo.")]
    [SerializeField] private Transform healthBarContainer; // (O Canvas)
    // --- FIM DA ADIÇÃO ---

    void Awake()
    {
        AllPlayersSpawned = false;
        OnAllPlayersSpawned = null;

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

    // Em GameSceneManager.cs

    void Start()
    {
        Debug.Log("[GameSceneManager] Start() foi chamado.");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GameSceneManager] NetworkManager.Singleton é NULO. Abortando.");
            return;
        }
        
        Debug.Log($"[GameSceneManager] IsServer: {NetworkManager.Singleton.IsServer}");

        // Apenas o Host/Servidor executa a lógica de spawn
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[GameSceneManager] É o Servidor. A verificar LobbyData...");

            if (LobbyData.SlotStates == null)
            {
                Debug.LogError("[GameSceneManager] LobbyData.SlotStates é nulo! Abortando spawn.");
                // Se o LobbyData não foi guardado corretamente na cena anterior,
                // o sinalizador AllPlayersSpawned nunca será ativado.
                return; // <-- PONTO DE FALHA SILENCIOSA #1
            }
            
            Debug.Log($"[GameSceneManager] LobbyData encontrado. {LobbyData.SlotStates.Length} slots.");

            if (playerAgentPrefab == null)
            {
                Debug.LogError("[GameSceneManager] Player Agent Prefab não configurado! Abortando spawn.");
                return; // <-- PONTO DE FALHA SILENCIOSA #2
            }
            
            Debug.Log("[GameSceneManager] Prefab OK. A chamar SpawnPlayersAndBots().");
            SpawnPlayersAndBots();
            
            Debug.Log("[GameSceneManager] A chamar NotifyPlayersSpawned() (que ativará o sinalizador).");
            StartCoroutine(NotifyPlayersSpawned());
        }
        else
        {
            Debug.Log("[GameSceneManager] Não é o Servidor. A saltar a lógica de spawn.");
            
            // --- CORRECÇÃO IMPORTANTE PARA CLIENTES ---
            // Se formos um Cliente, não somos responsáveis por spawnar,
            // mas as ondas (no Host) dependem de sabermos que os jogadores
            // (neste caso, o Host) terminaram de spawnar.
            // Para um Cliente, o "spawn" está "terminado" por defeito,
            // pois ele não faz nada.
            // ... MAS, o WaveManager do Cliente NÃO deve correr.
            
            // O problema é que se for Cliente, o sinalizador NUNCA fica true.
            // Vamos corrigir isto no WaveManager.
        }
    }

    // *** NOVO: Corrotina para notificar que todos os jogadores foram spawnados ***
    private IEnumerator NotifyPlayersSpawned()
    {
        AllPlayersSpawned = true;
        Debug.Log("[GameSceneManager] SINALIZADOR 'AllPlayersSpawned' ATIVADO.");
        yield return null;
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

    // Em GameSceneManager.cs (adicione este novo método)

    /// <summary>
    /// Instancia uma barra de vida e a configura para seguir um alvo.
    /// </summary>
    /// <param name="targetHealth">O HealthSystem do alvo (jogador ou bot).</param>
    public void SpawnHealthBarFor(HealthSystem targetHealth)
    {
        // Verifica se temos tudo o que é preciso
        if (healthBarPrefab == null || healthBarContainer == null)
        {
            Debug.LogWarning("GameSceneManager: Prefab da barra de vida ou Container (Canvas) não configurado!");
            return;
        }

        // 1. Instancia (cria) o prefab da barra de vida
        //    e o coloca como filho do Canvas (healthBarContainer)
        GameObject barInstance = Instantiate(healthBarPrefab, healthBarContainer);

        // 2. Pega no script FollowTargetUI que está nessa barra de vida
        FollowTargetUI followScript = barInstance.GetComponent<FollowTargetUI>();

        // 3. Chama o Setup() para ligar a barra de vida ao seu alvo
        if (followScript != null)
        {
            followScript.Setup(targetHealth);
        }
        else
        {
            Debug.LogError("O prefab da barra de vida não tem o script FollowTargetUI!");
        }
    }


    /// <summary>
    /// (APENAS HOST) Spawna o NetworkObject para um jogador humano específico.
    /// </summary>
    // Em GameSceneManager.cs

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

            // --- LÓGICA DA BARRA DE VIDA (MODIFICADA) ---
            HealthSystem hs = playerGO.GetComponent<HealthSystem>();
            if (hs != null)
            {
                // Verifica se o ID do jogador que estamos a spawnar
                // é DIFERENTE do ID local do Servidor (o Host).
                if (clientId != NetworkManager.Singleton.LocalClientId)
                {
                    // É um Cliente. Spawna a barra de vida para o Host ver.
                    SpawnHealthBarFor(hs);
                }
                // Se (clientId == NetworkManager.Singleton.LocalClientId),
                // é o próprio Host. Não fazemos nada,
                // porque o Host usa o seu HUD estático (PlayerHUDUI).
            }
            // --- FIM DA MODIFICAÇÃO ---
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
    
    HealthSystem hs = botGO.GetComponent<HealthSystem>();
        if (hs != null)
        {
            SpawnHealthBarFor(hs); // <-- Chama a nova função
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
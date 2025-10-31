using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using Unity.Netcode; // <-- Adicione esta linha

public class WaveManager : MonoBehaviour
{
    public event System.Action<string> OnNewWaveStarted;

    [System.Serializable]
    public class EnemyGroup
    {
        public GameObject enemyPrefab;
        public int count;
        public float spawnInterval;
        public float initialDelay;

        [Tooltip("Marque se este grupo deve ser tratado como um Boss (spawn único em posição fixa).")]
        public bool isBossGroup = false;

        [Tooltip("Se true, multiplica a VIDA dos inimigos ao invés de multiplicar a quantidade.")]
        public bool scaleHealthInsteadOfQuantity = false;

        [Tooltip("Marque se a morte para este boss deve dar a opção de 'Tentar Novamente'.")]
        public bool isGreaterBoss = false;
    }

    [System.Serializable]
    public class Wave
    {
        [Tooltip("Nome da onda (ex: 'Onda 1', 'Chefe Final').")]
        public string waveName;
        [Tooltip("Lista dos grupos de inimigos que compõem esta onda.")]
        public List<EnemyGroup> enemyGroups;
        [Header("Configuração de Arena")]
        [Tooltip("Se true, o jogador principal será movido para o ponto de spawn de boss no início desta onda.")]
        public bool useBossPlayerSpawn = false;
        [Tooltip("A posição exata onde um inimigo marcado como 'isBossGroup' irá spawnar.")]
        public Vector3 bossFixedSpawnPosition = Vector3.zero;
    }

    [Header("Configuração das Ondas")]
    [SerializeField] private List<Wave> waves;
    [SerializeField] private int startOnWave = 0;

    [Header("Referências")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Arraste o GameObject da UI de vida do Greater Boss aqui.")]
    [SerializeField] private GameObject bossHealthUI;

    [Header("Configuração de Coleta")]
    [SerializeField] private float coinCollectionTime = 1.5f;

    [Header("Configuração de Dificuldade Dinâmica")]
    [SerializeField] private int extraEnemiesPerPlayer = 1;
    [SerializeField] private float healthMultiplierPerPlayer = 0.1f;

    private int currentWaveIndex = -1;
    private Transform playerInstance;
    private Vector3 playerStandardSpawnPosition = Vector3.zero;
    private Vector3 playerBossSpawnPosition = Vector3.zero;
    private GameObject currentBossInstance = null;
    private GameSceneManager gameSceneManager; 
    private BossUIHealth bossUIHealthScript; // <-- Referência direta para o script da UI

    
    void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[WaveManager] Eu sou um Cliente. A desativar a lógica de spawn.");
            this.enabled = false; // Desativa o script (para de correr Updates, etc.)
            return; // Para a execução do Start()
        }

        currentWaveIndex = Mathf.Clamp(startOnWave - 1, -1, waves.Count - 1);
        FindAndSetPlayerSpawnPoints();
        gameSceneManager = FindObjectOfType<GameSceneManager>();

        if (UpgradeManager.Instance == null)
            Debug.LogError("WaveManager: UpgradeManager não encontrado na cena!");

        // --- ADICIONADO: Encontra a referência do script da UI ---
        if (bossHealthUI != null)
        {
            bossUIHealthScript = bossHealthUI.GetComponent<BossUIHealth>();
            if (bossUIHealthScript == null)
            {
                Debug.LogError("[WaveManager] O GameObject 'Boss Health UI' foi arrastado, mas falta o script 'BossUIHealth.cs' nele!");
            }
        }
        
        HandleShopClosed();
    }

    private void FindAndSetPlayerSpawnPoints()
    {
        PlayerSpawn[] spawns = FindObjectsOfType<PlayerSpawn>();

        if (spawns.Length == 0)
        {
            Debug.LogWarning("WaveManager: Nenhum PlayerSpawn.cs encontrado.");
            return;
        }

        bool standardFound = false;
        bool bossFound = false;
        foreach (PlayerSpawn spawn in spawns)
        {
            if (spawn.isBossSpawnPoint)
            {
                playerBossSpawnPosition = spawn.transform.position;
                bossFound = true;
            }
            else
            {
                playerStandardSpawnPosition = spawn.transform.position;
                standardFound = true;
            }
        }
        if (!standardFound) Debug.LogWarning("WaveManager: Nenhum PlayerSpawn padrão encontrado.");
        if (!bossFound) Debug.LogWarning("WaveManager: Nenhum PlayerSpawn de boss encontrado.");
    }

    void OnEnable()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnShopClosed += HandleShopClosed;
        }
    }

    void OnDisable()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnShopClosed -= HandleShopClosed;
        }
        StopAllCoroutines();
    }

    public void RetryCurrentWave()
    {
        Debug.Log("[WaveManager] ==== RetryCurrentWave CHAMADO ====");
        
        if (currentWaveIndex < 0 || currentWaveIndex >= waves.Count)
        {
             Debug.LogError("[WaveManager] Tentando Retry, mas o índice da onda é inválido.");
             return;
        }

        // Para todas as corrotinas (spawners, etc)
        StopAllCoroutines();

        // 1. Destrói todos os "minions" (EnemyController)
        Debug.Log("[WaveManager] Procurando e destruindo EnemyControllers...");
        var enemies = FindObjectsOfType<EnemyController>();
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        Debug.Log($"[WaveManager] {enemies.Length} EnemyControllers marcados para Destroy().");


        // 2. Encontra e destrói o Linker de Vida
        Debug.Log("[WaveManager] Procurando BossHealthLinker...");
        BossHealthLinker oldLinker = FindObjectOfType<BossHealthLinker>();
        if (oldLinker != null)
        {
            Debug.LogWarning($"[WaveManager] !! Encontrou BossHealthLinker ANTIGO: {oldLinker.name} (ID: {oldLinker.GetInstanceID()}). Marcando para Destroy().. !!");
            Destroy(oldLinker.gameObject);
        }
        else
        {
            Debug.Log("[WaveManager] ...Nenhum BossHealthLinker encontrado para destruir.");
        }

        // 3. Destrói as partes do Boss (Paredes)
        Debug.Log("[WaveManager] Procurando e destruindo WallBossControllers...");
        WallBossController[] walls = FindObjectsOfType<WallBossController>();
        foreach (WallBossController wall in walls)
        {
            if (wall != null)
            {
                Destroy(wall.gameObject);
            }
        }
        Debug.Log($"[WaveManager] {walls.Length} WallBossControllers marcados para Destroy().");   

        // 5. Limpa a referência interna
        currentBossInstance = null;

        // 6. *** MUDANÇA CRÍTICA: Inicia a CORROTINA DE ESPERA ***
        Debug.Log("[WaveManager] Chamando Corrotina de espera (WaitAndStartWave_Internal) para permitir que Destroy() termine.");
        StartCoroutine(WaitAndStartWave_Internal());
    }

    /// <summary>
    /// Espera 1 frame para garantir que todos os objetos marcados com Destroy()
    /// sejam removidos da cena antes de iniciar a próxima onda.
    /// Isso previne a BossUIHealth de se conectar ao Linker antigo.
    /// </summary>
    private IEnumerator WaitAndStartWave_Internal()
    {
        // Espera UM frame. Isto dá ao Unity tempo para processar
        // todas as chamadas 'Destroy()' feitas no RetryCurrentWave.
        Debug.Log("[WaveManager_Internal] ...A aguardar 1 frame para 'Destroy' completar...");
        yield return null; 
        
        // Agora que os objetos antigos (Linker, Walls, Zones) foram 100% destruídos...
        // Podemos iniciar a onda com segurança.
        Debug.Log("[WaveManager_Internal] ...Frame aguardado. A chamar StartNextWave(false).");
        StartCoroutine(StartNextWave(false));
    }

    public bool IsCurrentWaveGreaterBoss
    {
        get
        {
            if (currentWaveIndex < 0 || currentWaveIndex >= waves.Count)
            {
                return false;
            }

            Wave currentWave = waves[currentWaveIndex];
            return currentWave.enemyGroups.Any(group => group.isGreaterBoss);
        }
    }

    private void ResurrectAllPlayers()
    {
        PlayerTargetable[] playersToReset = FindObjectsOfType<PlayerTargetable>(true);

        foreach (var playerTarget in playersToReset)
        {
            if (!playerTarget.gameObject.activeSelf)
            {
                playerTarget.gameObject.SetActive(true);
            }

            HealthSystem hs = playerTarget.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.ResetForRetry();
                if (gameSceneManager != null)
                {
                    AgentManager agent = playerTarget.GetComponent<AgentManager>();
                    if (agent != null)
                    {
                        if (agent.IsPlayerBot() || (agent.IsOwner == false))
                        {
                            gameSceneManager.SpawnHealthBarFor(hs);
                        }
                    }
                }           
            }
        }
    }

    IEnumerator StartNextWave(bool advanceWaveIndex = true)
    {
        Debug.Log($"[WaveManager] ==== StartNextWave INICIADO (advanceWaveIndex={advanceWaveIndex}) ====");

        Debug.Log("[WaveManager] ...A aguardar sinal do GameSceneManager.AllPlayersSpawned...");
        yield return new WaitUntil(() => GameSceneManager.AllPlayersSpawned);
        Debug.Log("[WaveManager] ...Sinal recebido! A continuar a onda.");

        if (advanceWaveIndex)
        {
            currentWaveIndex++;
        }

        if (currentWaveIndex >= waves.Count)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerVictory();
            }
            yield break;
        }
        
        ResurrectAllPlayers();

        Wave currentWave = waves[currentWaveIndex];

        // Busca o jogador local
        if (playerInstance == null)
        {
            playerInstance = FindLocalPlayer();
        }

        if (playerInstance != null)
        {
            Vector3 targetPosition = currentWave.useBossPlayerSpawn ? playerBossSpawnPosition : playerStandardSpawnPosition;
            playerInstance.position = targetPosition;
        }
        else
        {
            Debug.LogWarning("Não foi possível encontrar o jogador local para posicionar.");
        }

        // --- LÓGICA DA UI ATUALIZADA ---
        bool isGreaterBossWave = currentWave.enemyGroups.Any(group => group.isGreaterBoss);
        Debug.Log($"[WaveManager] ...Esta é uma onda de Greater Boss? {isGreaterBossWave}");
        if (bossHealthUI != null)
        {
            // Apenas ativa o GameObject. A UI não fará nada até que entreguemos o Linker.
            Debug.Log($"[WaveManager] ...Ativando o GameObject bossHealthUI ({isGreaterBossWave})");
            bossHealthUI.SetActive(isGreaterBossWave);
        }
        else if (isGreaterBossWave)
        {
            Debug.LogWarning("[WaveManager] Esta é uma onda de Greater Boss, mas a referência 'Boss Health UI' não foi definida no Inspector!");
        }
        // --- FIM DA LÓGICA DA UI ---


        int playerCount = 1;
        if (PlayerManager.Instance != null)
        {
            playerCount = PlayerManager.Instance.GetActivePlayerCount();
            playerCount = Mathf.Max(1, playerCount);
        }
        
       
        Debug.Log($"[WaveManager] ...Invocando evento OnNewWaveStarted (Onda: {currentWave.waveName})");
        OnNewWaveStarted?.Invoke(currentWave.waveName ?? $"Onda {currentWaveIndex + 1}");
        
        List<Coroutine> runningSpawners = new List<Coroutine>();
        foreach (var group in currentWave.enemyGroups)
        {
            Coroutine spawner = StartCoroutine(SpawnEnemyGroup(group, playerCount, group.scaleHealthInsteadOfQuantity ? playerCount : 1));
            runningSpawners.Add(spawner);
        }

        foreach (var spawner in runningSpawners)
        {
            yield return spawner;
        }

        while (FindObjectsOfType<EnemyController>().Length > 0)
        {
            yield return null;
        }
        currentBossInstance = null;
        Debug.Log("[WaveManager] ...Todos os EnemyControllers foram destruídos. Onda terminada.");

        Coin[] remainingCoins = FindObjectsOfType<Coin>();
        if (remainingCoins.Length > 0)
        {
            foreach (Coin coin in remainingCoins)
            {
                if (coin != null) coin.ForceAttract();
            }

            yield return new WaitForSeconds(coinCollectionTime);

            Coin[] coinsToDestroy = FindObjectsOfType<Coin>();

            foreach (Coin coin in coinsToDestroy)
            {
                if (coin != null) Destroy(coin.gameObject);
            }
        }

        if (UpgradeManager.Instance != null)
        {
            Debug.Log("[WaveManager] ...Mostrando tela de Upgrade.");
            UpgradeManager.Instance.ShowUpgradeScreen();
        }
        else
        {
            Debug.LogError("UpgradeManager.Instance é nulo. Pulando tela de Upgrade.");
            HandleShopClosed();
        }
    }

    private Transform FindLocalPlayer()
    {
        var agents = FindObjectsOfType<AgentManager>();
        foreach (var agent in agents)
        {
            if (agent != null && agent.IsOwner && !agent.IsPlayerBot())
            {
                return agent.transform;
            }
        }
        Debug.LogWarning("[WaveManager] Não foi possível encontrar o jogador local!");
        return null;
    }

    private void HandleShopClosed()
    {
        Debug.Log("[WaveManager] HandleShopClosed() chamado. Iniciando próxima onda.");
        StartCoroutine(StartNextWave(true));
    }

    IEnumerator SpawnEnemyGroup(EnemyGroup group, int playerCount, float baseHealthMultiplier)
    {
        if (group.initialDelay > 0)
        {
            yield return new WaitForSeconds(group.initialDelay);
        }

        Vector3 spawnPosition = Vector3.zero;

        if (group.isBossGroup)
        {
            Debug.Log($"[WaveManager] !! SPAWNANDO GRUPO DE BOSS: {group.enemyPrefab.name} !!");
            spawnPosition = waves[currentWaveIndex].bossFixedSpawnPosition;
            currentBossInstance = SpawnEnemy(group.enemyPrefab, spawnPosition, baseHealthMultiplier); 
            yield break;
        }

        bool isInfiniteSpawn = group.count <= 0;
        int baseCount = group.count;
        int scaledCount = baseCount;
        float groupHealthMultiplier = baseHealthMultiplier;

        if (!isInfiniteSpawn)
        {
            if (group.scaleHealthInsteadOfQuantity)
            {
                scaledCount = baseCount;
                groupHealthMultiplier = baseHealthMultiplier * playerCount;
            }
            else
            {
                scaledCount = baseCount * playerCount;
                groupHealthMultiplier = baseHealthMultiplier;
            }
        }

        if (!isInfiniteSpawn)
        {
            for (int i = 0; i < scaledCount; i++)
            {
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    Transform targetSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                    if (targetSpawnPoint)
                    {
                        spawnPosition = targetSpawnPoint.position;
                    }
                }
                else
                {
                    spawnPosition = transform.position;
                    Debug.LogWarning("Nenhum ponto de spawn de inimigo configurado!");
                }

                SpawnEnemy(group.enemyPrefab, spawnPosition, groupHealthMultiplier);
                yield return new WaitForSeconds(group.spawnInterval);
            }
        }
        else if (isInfiniteSpawn && currentBossInstance != null)
        {
            while (currentBossInstance != null)
            {
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    Transform targetSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                    spawnPosition = targetSpawnPoint.position;
                }
                else
                {
                    spawnPosition = transform.position;
                }

                SpawnEnemy(group.enemyPrefab, spawnPosition, groupHealthMultiplier);
                yield return new WaitForSeconds(group.spawnInterval);
            }
        }
        else if (isInfiniteSpawn && currentBossInstance == null)
        {
            Debug.LogWarning($"Grupo '{group.enemyPrefab.name}' configurado para spawn infinito sem Boss.");
        }
    }

    GameObject SpawnEnemy(GameObject enemyPrefab, Vector3 position, float healthMultiplier)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Tentando spawnar um prefab de inimigo nulo!");
            return null;
        }

        GameObject enemyInstance = Instantiate(enemyPrefab, position, Quaternion.identity);

        Debug.Log($"[WaveManager] SpawnEnemy INSTANCIOU: {enemyInstance.name} (ID: {enemyInstance.GetInstanceID()})");

        // --- INÍCIO DA NOVA LÓGICA DE CONEXÃO ---
        
        // Procura o Linker no prefab que acabamos de spawnar (em filhos, inclusive)
        BossHealthLinker newLinker = enemyInstance.GetComponentInChildren<BossHealthLinker>(true);
        
        if (newLinker != null)
        {
            Debug.LogWarning($"[WaveManager] !! Um BossHealthLinker (ID: {newLinker.GetInstanceID()}) foi encontrado no prefab spawnado!");
            
            if (bossUIHealthScript != null)
            {
                // !! ENTREGA O LINKER DIRETAMENTE PARA A UI !!
                Debug.Log("[WaveManager] ...Entregando este NOVO linker para o bossUIHealthScript.");
                bossUIHealthScript.Initialize(newLinker);
            }
            else
            {
                Debug.LogError("[WaveManager] ...Spawnou um Boss com Linker, mas a referência 'bossUIHealthScript' é NULA! A UI de vida não vai funcionar.");
            }
        }
        // --- FIM DA NOVA LÓGICA DE CONEXÃO ---


        HealthSystem enemyHealth = enemyInstance.GetComponent<HealthSystem>();
        if (enemyHealth != null)
        {
            float baseMaxHealth = enemyHealth.MaxHealth;
            enemyHealth.MaxHealth = baseMaxHealth * healthMultiplier;
            enemyHealth.HealFull();
        }
        NetworkObject netObj = enemyInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
        }
        else
        {
            Debug.LogWarning($"[WaveManager] Inimigo '{enemyPrefab.name}' foi spawnado SEM NetworkObject. Clientes não o verão.");
        }

        return enemyInstance;
    }
}

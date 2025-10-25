using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

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
    private bool playersReady = false; // *** NOVO: Flag para controlar se jogadores estão prontos ***

    void Start()
    {
        currentWaveIndex = Mathf.Clamp(startOnWave - 1, -1, waves.Count - 1);
        FindAndSetPlayerSpawnPoints();

        if (UpgradeManager.Instance == null)
            Debug.LogError("WaveManager: UpgradeManager não encontrado na cena!");

        // *** NOVA LÓGICA: Se inscreve no evento e espera ***
        if (GameSceneManager.OnAllPlayersSpawned != null)
        {
            GameSceneManager.OnAllPlayersSpawned += OnPlayersSpawned;
        }
        else
        {
            // Fallback: se o evento não existir, espera um tempo e inicia
            Debug.LogWarning("[WaveManager] Evento OnAllPlayersSpawned não encontrado. Usando fallback.");
            StartCoroutine(DelayedStart());
        }
    }

    // *** NOVO: Método chamado quando todos os jogadores foram spawnados ***
    private void OnPlayersSpawned()
    {
        playersReady = true;
        
        // Remove a inscrição do evento
        GameSceneManager.OnAllPlayersSpawned -= OnPlayersSpawned;
        
        // Inicia as ondas
        HandleShopClosed();
    }

    // *** NOVO: Fallback caso o evento não funcione ***
    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(.1f);
        playersReady = true;
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
        
        // *** IMPORTANTE: Remove a inscrição do evento ***
        GameSceneManager.OnAllPlayersSpawned -= OnPlayersSpawned;
        
        StopAllCoroutines();
    }

    public void RetryCurrentWave()
    {
        if (currentWaveIndex < 0 || currentWaveIndex >= waves.Count)
        {
             Debug.LogError("Tentando Retry, mas o índice da onda é inválido.");
             return;
        }

        StopAllCoroutines();

        var enemies = FindObjectsOfType<EnemyController>();
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        currentBossInstance = null;

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
            }
        }
    }

    IEnumerator StartNextWave(bool advanceWaveIndex = true)
    {
        // *** NOVA VERIFICAÇÃO: Espera se os jogadores não estão prontos ***
        if (!playersReady)
        {
            Debug.LogWarning("[WaveManager] Jogadores não estão prontos. Aguardando...");
            yield return new WaitUntil(() => playersReady);
        }

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

        // *** AGORA O PLAYER COUNT DEVE ESTAR CORRETO ***
        int playerCount = 1;
        if (PlayerManager.Instance != null)
        {
            playerCount = PlayerManager.Instance.GetActivePlayerCount();
            playerCount = Mathf.Max(1, playerCount);
            Debug.Log($"[WaveManager] PlayerCount correto: {playerCount} jogadores+bots");
        }
        
     
        OnNewWaveStarted?.Invoke(currentWave.waveName ?? $"Onda {currentWaveIndex + 1}");

        // LOG DA COMPOSIÇÃO DA ONDA
        Debug.Log("========================================");
        Debug.Log($"COMPOSIÇÃO DA ONDA {currentWaveIndex}: {currentWave.waveName}");
        Debug.Log($"Jogadores+Bots Ativos: {playerCount}");
        Debug.Log("----------------------------------------");
        
        int groupIndex = 0;
        foreach (var group in currentWave.enemyGroups)
        {
            float enemyHealthMultiplier = group.scaleHealthInsteadOfQuantity ? playerCount : 1;
            Debug.Log($"Jogadores+Bots Ativos: {playerCount}. Multiplicador HP Inimigo: {enemyHealthMultiplier:F2}");

            string enemyName = group.enemyPrefab != null ? group.enemyPrefab.name : "NULL";
            
            if (group.isBossGroup)
            {
                Debug.Log($"[Grupo {groupIndex}] BOSS: {enemyName} x1 (Vida x{enemyHealthMultiplier:F2})");
            }
            else if (group.count <= 0)
            {
                Debug.Log($"[Grupo {groupIndex}] MINIONS INFINITOS: {enemyName} (enquanto boss vivo)");
            }
            else
            {
                int baseCount = group.count;
                int finalCount;
                float finalHealthMult;
                
                if (group.scaleHealthInsteadOfQuantity)
                {
                    finalCount = baseCount;
                    finalHealthMult = enemyHealthMultiplier * playerCount;
                    Debug.Log($"[Grupo {groupIndex}] {enemyName} x{finalCount} (VIDA ESCALADA x{finalHealthMult:F2})");
                }
                else
                {
                    finalCount = baseCount * playerCount;
                    finalHealthMult = enemyHealthMultiplier;
                    Debug.Log($"[Grupo {groupIndex}] {enemyName} x{finalCount} (QUANTIDADE ESCALADA, Vida x{finalHealthMult:F2})");
                }
            }
            groupIndex++;
        }
        Debug.Log("========================================");

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
            UpgradeManager.Instance.ShowUpgradeScreen();
        }
        else
        {
            Debug.LogError("UpgradeManager.Instance é nulo. Pulando tela de Upgrade.");
            HandleShopClosed();
        }
    }

    // *** MÉTODO: Encontra o jogador local ***
    private Transform FindLocalPlayer()
    {
        // Busca por AgentManager que é dono e não é bot
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
                     spawnPosition = targetSpawnPoint.position;
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

        HealthSystem enemyHealth = enemyInstance.GetComponent<HealthSystem>();
        if (enemyHealth != null)
        {
            float baseMaxHealth = enemyHealth.MaxHealth;
            enemyHealth.MaxHealth = baseMaxHealth * healthMultiplier;
            enemyHealth.HealFull();
        }
        return enemyInstance;
    }
}
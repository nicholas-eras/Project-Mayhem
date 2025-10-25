using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

/// <summary>
/// Gerencia o fluxo das ondas de inimigos, incluindo spawn, espera,
/// coleta de moedas e transição para a loja de upgrades.
/// Também escala a dificuldade com base no número de jogadores.
/// </summary>
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

        [Tooltip("Se for um Boss, marque para que a vida do Boss escale com os jogadores, mas NUNCA a quantidade.")]
        public bool dontScaleQuantity = false;

        [Tooltip("Se true, mantém a quantidade fixa mas MULTIPLICA A VIDA ao invés de multiplicar a quantidade.")]
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

    void Start()
    {
        currentWaveIndex = Mathf.Clamp(startOnWave - 1, -1, waves.Count - 1);
        FindAndSetPlayerSpawnPoints();

        if (UpgradeManager.Instance == null)
            Debug.LogError("WaveManager: UpgradeManager não encontrado na cena!");

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

        if (playerInstance == null && PlayerManager.Instance != null)
        {
             playerInstance = PlayerManager.Instance.GetClosestPlayer(Vector3.zero);
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

        int playerCount = 1;
        if (PlayerManager.Instance != null)
        {
            playerCount = PlayerManager.Instance.GetActivePlayerCount();
            playerCount = Mathf.Max(1, playerCount);
        }
        
        float enemyHealthMultiplier = 1f + (healthMultiplierPerPlayer * (playerCount - 1));
        Debug.Log($"Jogadores Ativos: {playerCount}. Multiplicador HP Inimigo: {enemyHealthMultiplier:F2}");

        OnNewWaveStarted?.Invoke(currentWave.waveName ?? $"Onda {currentWaveIndex + 1}");

        // LOG DETALHADO DA COMPOSIÇÃO DA ONDA
        Debug.Log("========================================");
        Debug.Log($"COMPOSIÇÃO DA ONDA {currentWaveIndex}: {currentWave.waveName}");
        Debug.Log($"Jogadores Ativos: {playerCount}");
        Debug.Log("----------------------------------------");
        
        int groupIndex = 0;
        foreach (var group in currentWave.enemyGroups)
        {
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
                    int wouldBeScaledCount = baseCount + (extraEnemiesPerPlayer * (playerCount - 1));
                    float quantityRatio = (float)wouldBeScaledCount / baseCount;
                    finalHealthMult = enemyHealthMultiplier * quantityRatio;
                    Debug.Log($"[Grupo {groupIndex}] {enemyName} x{finalCount} (Base: {baseCount}, VIDA ESCALADA x{finalHealthMult:F2} - compensando {wouldBeScaledCount} inimigos)");
                }
                else if (group.dontScaleQuantity)
                {
                    finalCount = baseCount;
                    finalHealthMult = enemyHealthMultiplier;
                    Debug.Log($"[Grupo {groupIndex}] {enemyName} x{finalCount} (FIXO, Vida x{finalHealthMult:F2})");
                }
                else
                {
                    finalCount = baseCount + (extraEnemiesPerPlayer * (playerCount - 1));
                    finalCount = Mathf.Max(1, finalCount);
                    finalHealthMult = enemyHealthMultiplier;
                    Debug.Log($"[Grupo {groupIndex}] {enemyName} x{finalCount} (Base: {baseCount}, Vida x{finalHealthMult:F2})");
                }
            }
            groupIndex++;
        }
        Debug.Log("========================================");

        List<Coroutine> runningSpawners = new List<Coroutine>();
        foreach (var group in currentWave.enemyGroups)
        {
            Coroutine spawner = StartCoroutine(SpawnEnemyGroup(group, playerCount, enemyHealthMultiplier));
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

    private void HandleShopClosed()
    {
        StartCoroutine(StartNextWave(true));
    }

    IEnumerator SpawnEnemyGroup(EnemyGroup group, int playerCount, float healthMultiplier)
    {
        if (group.initialDelay > 0)
        {
            yield return new WaitForSeconds(group.initialDelay);
        }

        Vector3 spawnPosition = Vector3.zero;

        // LÓGICA DE BOSS FIXO
        if (group.isBossGroup)
        {
            spawnPosition = waves[currentWaveIndex].bossFixedSpawnPosition;
            
            currentBossInstance = SpawnEnemy(group.enemyPrefab, spawnPosition, healthMultiplier); 
            yield break;
        }

        bool isInfiniteSpawn = group.count <= 0;

        int baseCount = group.count;
        int scaledCount = baseCount;
        float groupHealthMultiplier = healthMultiplier; // Multiplier padrão

        if (!isInfiniteSpawn)
        {
            // NOVA LÓGICA: scaleHealthInsteadOfQuantity
            if (group.scaleHealthInsteadOfQuantity)
            {
                // Mantém a quantidade base
                scaledCount = baseCount;
                // Multiplica a vida EXTRA além do multiplier base
                // Exemplo: Se tinha 3 inimigos e agora tem 2 players (que dariam 5 inimigos)
                // Ao invés de spawnar 5, spawna 3 com vida aumentada proporcionalmente
                int wouldBeScaledCount = baseCount + (extraEnemiesPerPlayer * (playerCount - 1));
                float quantityRatio = (float)wouldBeScaledCount / baseCount;
                groupHealthMultiplier = healthMultiplier * quantityRatio;
                
                Debug.Log($"Grupo '{group.enemyPrefab.name}' com scaleHealthInsteadOfQuantity. " +
                         $"Quantidade fixa: {scaledCount}. Vida multiplicada por {groupHealthMultiplier:F2} " +
                         $"(seria {wouldBeScaledCount} inimigos normais).");
            }
            else if (group.dontScaleQuantity)
            {
                scaledCount = baseCount;
                Debug.Log($"Grupo '{group.enemyPrefab.name}' NÃO escalado em quantidade. Base: {baseCount}.");
            }
            else
            {
                scaledCount = baseCount + (extraEnemiesPerPlayer * (playerCount - 1));
                scaledCount = Mathf.Max(1, scaledCount);
                Debug.Log($"Grupo '{group.enemyPrefab.name}' escalado. Base: {baseCount}, Total: {scaledCount}.");
            }
        }

        // Spawn Finito
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
        // Spawn Infinito
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
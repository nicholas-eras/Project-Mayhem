using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

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
        
        [Tooltip("Marque se este grupo deve ser tratado como um Boss (spawn fixo).")]
        public bool isBossGroup = false; 
        
        // --- NOVO PARÂMETRO ADICIONADO AQUI ---
        [Tooltip("Marque se a morte para este boss deve dar a opção de 'Tentar Novamente'.")]
        public bool isGreaterBoss = false; 
    }

    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public List<EnemyGroup> enemyGroups; 
        [Header("Configuração de Arena")]
        [Tooltip("Se for True, o Player nascerá no ponto 'isBossSpawnPoint = true'.")]
        public bool useBossPlayerSpawn = false; 
        public Vector3 bossFixedSpawnPosition = Vector3.zero; 
    }

    [Header("Configuração das Ondas")]
    [SerializeField] private List<Wave> waves;
    
    [Tooltip("Índice da onda para começar (0 = 1ª Onda). Usado para testes.")]
    [SerializeField] private int startOnWave = 0;

    [Header("Referências")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Configuração de Coleta")]
    [SerializeField] private float coinCollectionTime = 1.5f;

    private int currentWaveIndex = 0;
    
    private Transform playerInstance;
    private Vector3 playerStandardSpawnPosition = Vector3.zero;
    private Vector3 playerBossSpawnPosition = Vector3.zero;
    private GameObject currentBossInstance = null; 

    void Start()
    {
        currentWaveIndex = Mathf.Clamp(startOnWave, 0, waves.Count - 1); 
        
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerInstance = playerGO.transform;
        }

        FindAndSetPlayerSpawnPoints();

        if (UpgradeManager.Instance == null)
        {
            Debug.LogError("UpgradeManager não encontrado. A funcionalidade de Shop/Next Wave falhará.");
        }
        StartCoroutine(StartNextWave());
    }
    
    private void FindAndSetPlayerSpawnPoints()
    {
        PlayerSpawn[] spawns = FindObjectsOfType<PlayerSpawn>();
        
        if (spawns.Length == 0)
        {
            Debug.LogWarning("Nenhum PlayerSpawn.cs encontrado. Player sempre nascerá em (0,0,0).");
            return;
        }

        foreach (PlayerSpawn spawn in spawns)
        {
            if (spawn.isBossSpawnPoint)
            {
                playerBossSpawnPosition = spawn.transform.position; 
            }
            else
            {
                playerStandardSpawnPosition = spawn.transform.position; 
            }
        }
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
        Debug.Log($"[WaveManager] Tentando novamente a wave: {currentWaveIndex}");

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
        StartCoroutine(StartNextWave());
    }

    // --- PROPRIEDADE ATUALIZADA ---
    public bool IsCurrentWaveGreaterBoss
    {
        get
        {
            if (currentWaveIndex < 0 || currentWaveIndex >= waves.Count)
            {
                return false; // Não está em uma onda válida
            }
            
            Wave currentWave = waves[currentWaveIndex];

            // Verifica todos os grupos de inimigos da onda
            foreach (var group in currentWave.enemyGroups)
            {
                // Se QUALQUER grupo nesta wave estiver marcado...
                if (group.isGreaterBoss)
                {
                    return true; // ...a wave inteira é considerada uma "Greater Boss Wave".
                }
            }
            
            // Se nenhum grupo estava marcado, retorna falso.
            return false; 
        }
    }


    IEnumerator StartNextWave()
    {
        // 1. CHECAGEM DE FIM DE JOGO (VITÓRIA)
        if (currentWaveIndex >= waves.Count)
        {           
            if (GameManager.Instance != null)
            {
                // Passa 'false' porque o jogador VENCEU (não morreu para um boss)
                GameManager.Instance.GameOver(false);
            }
            yield break; // Termina a corrotina
        }

        Wave currentWave = waves[currentWaveIndex];
        
        // 2. POSICIONAMENTO DO PLAYER
        if (playerInstance != null)
        {
            if (currentWave.useBossPlayerSpawn) 
            {
               playerInstance.position = playerBossSpawnPosition;
            }
            else
            {
               playerInstance.position = playerStandardSpawnPosition; 
            }
        }
        
        // 3. INÍCIO DA ONDA E SPAWN
        OnNewWaveStarted?.Invoke(currentWave.waveName);
        
        List<Coroutine> runningSpawners = new List<Coroutine>();
        foreach (var group in currentWave.enemyGroups)
        {
            Coroutine spawner = StartCoroutine(SpawnEnemyGroup(group));
            runningSpawners.Add(spawner);
        }

        foreach (var spawner in runningSpawners)
        {
            yield return spawner;
        }
        
        // 4. ESPERA PELOS INIMIGOS
        while (GameObject.FindGameObjectWithTag("Enemy") != null)
        {
            yield return null;
        }

        // 5. LÓGICA DE COLETA DE MOEDAS
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
                if (coin != null)
                {
                    Destroy(coin.gameObject);
                }
            }
        }

        // 6. CHAMA O SHOP (Próxima Fase)
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
        currentWaveIndex++;
        currentBossInstance = null; 
        StartCoroutine(StartNextWave());
    }

    IEnumerator SpawnEnemyGroup(EnemyGroup group)
    {
        if (group.initialDelay > 0)
        {
            yield return new WaitForSeconds(group.initialDelay);
        }

        Transform targetSpawnPoint = null;
        if (group.isBossGroup)
        {
            GameObject tempSpawn = new GameObject($"Boss_Spawn_{group.enemyPrefab.name}");
            tempSpawn.transform.position = waves[currentWaveIndex].bossFixedSpawnPosition;
            targetSpawnPoint = tempSpawn.transform;
        }
        
        bool isInfiniteSpawn = group.count <= 0;
        int totalToSpawn = isInfiniteSpawn ? 1 : group.count; 

        if (group.isBossGroup)
        {
            currentBossInstance = SpawnEnemy(group.enemyPrefab, targetSpawnPoint);
            totalToSpawn = 0; 
            
            if (targetSpawnPoint != null)
            {
                Destroy(targetSpawnPoint.gameObject);
            }
        }

        // A. Spawn Finito
        if (!isInfiniteSpawn)
        {
            for (int i = 0; i < totalToSpawn; i++)
            {
                SpawnEnemy(group.enemyPrefab, targetSpawnPoint);
                yield return new WaitForSeconds(group.spawnInterval);
            }
        }
        // B. Spawn Infinito (Minions)
        else if (isInfiniteSpawn && currentBossInstance != null)
        {
            while (currentBossInstance != null)
            {
                SpawnEnemy(group.enemyPrefab, targetSpawnPoint);
                yield return new WaitForSeconds(group.spawnInterval);
            }
        }
    }

    GameObject SpawnEnemy(GameObject enemyPrefab, Transform fixedSpawnPoint)
    {
        Transform finalSpawnPoint;

        if (fixedSpawnPoint != null)
        {
            finalSpawnPoint = fixedSpawnPoint;
        }
        else
        {
            if (spawnPoints.Length == 0) return null;
            finalSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        }
        
        return Instantiate(enemyPrefab, finalSpawnPoint.position, finalSpawnPoint.rotation);
    }
}
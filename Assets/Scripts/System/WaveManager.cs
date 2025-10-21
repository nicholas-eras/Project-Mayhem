using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    // FIX 1: DECLARE THE MISSING EVENT
    // Other scripts (like a UIManager) can listen to this to update the wave display.
    public event System.Action<string> OnNewWaveStarted;

    [System.Serializable]
    public class EnemyGroup
    {
        public GameObject enemyPrefab;
        public int count; 
        public float spawnInterval; 
        public float initialDelay; 
        
        [Tooltip("Marque se este grupo deve ser tratado como um Boss (spawn fixo).")]
        public bool isBossGroup = false; // <--- NOVO CAMPO
    }

    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public List<EnemyGroup> enemyGroups; // Lista de "receitas" de spawn para esta onda
        [Header("Configuração de Arena")]
        [Tooltip("Se for True, o Player nascerá no ponto 'isBossSpawnPoint = true'.")]
        public bool useBossPlayerSpawn = false; // <--- CORREÇÃO: ADICIONAR ESTE CAMPO
        public Vector3 bossFixedSpawnPosition = Vector3.zero; // <--- NOVO CAMPO
    }

    [Header("Configuração das Ondas")]
    [SerializeField] private List<Wave> waves;

    [Header("Referências")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Configuração de Coleta")]
    [SerializeField] private float coinCollectionTime = 1.5f;

    private int currentWaveIndex = 0;
    
    private Transform playerInstance;
    private Vector3 playerStandardSpawnPosition = Vector3.zero;
    private Vector3 playerBossSpawnPosition = Vector3.zero;
    private GameObject currentBossInstance = null; // <--- ADICIONAR ISSO AQUI
    void Start()
    {
        // 1. Encontra a instância do Player
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerInstance = playerGO.transform;
        }

        // 2. Encontra os pontos de spawn do Player
        FindAndSetPlayerSpawnPoints();

        // A verificação de segurança já está boa aqui.
        if (UpgradeManager.Instance == null)
        {
            Debug.LogError("UpgradeManager não encontrado. A funcionalidade de Shop/Next Wave falhará.");
        }
        // Inicia a primeira onda.
        StartCoroutine(StartNextWave());
    }
    
    // NOVO MÉTODO: Localiza os objetos PlayerSpawn na cena
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
        // FIX 2: CORRECT EVENT SUBSCRIPTION
        // Subscribe to the event using the singleton 'Instance'.
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnShopClosed += HandleShopClosed;
        }
    }

    void OnDisable()
    {
        // FIX 2: CORRECT EVENT UNSUBSCRIPTION
        // Unsubscribe using the same singleton 'Instance' to prevent memory leaks.
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnShopClosed -= HandleShopClosed;
        }
        StopAllCoroutines();
    }

    IEnumerator StartNextWave()
    {
        // 1. CHECAGEM DE FIM DE JOGO
        if (currentWaveIndex >= waves.Count)
        {
            Debug.Log("Fim de Jogo! Todas as ondas completadas.");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver(); // Chama o método que retorna ao Menu/Seleção
            }
            yield break; // Termina a corrotina
        }

        Wave currentWave = waves[currentWaveIndex];
        
        // 2. POSICIONAMENTO DO PLAYER (Prepara a arena para a luta)
        if (playerInstance != null)
        {
            // Se a onda for marcada como 'Boss Spawn', move o Player para a posição Boss.
            // NOTA: Você deve adicionar 'public bool useBossPlayerSpawn' na sua struct Wave.
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

        // Espera todas as coroutines de spawn terminarem.
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
            
            // Destrói moedas restantes (por segurança)
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
            // Se o Manager falhar, força o avanço para a próxima onda
            HandleShopClosed(); 
        }
    }
    // Esta função é chamada pelo evento OnShopClosed
    private void HandleShopClosed()
    {
        currentWaveIndex++;
        currentBossInstance = null; // Limpa a referência para a próxima onda
        StartCoroutine(StartNextWave());
    }

    // Esta coroutine gerencia o spawn de apenas UM grupo de inimigos.
    IEnumerator SpawnEnemyGroup(EnemyGroup group)
    {
        if (group.initialDelay > 0)
        {
            yield return new WaitForSeconds(group.initialDelay);
        }

        Transform targetSpawnPoint = null;
        if (group.isBossGroup)
        {
            // Se for Boss, cria um Transform temporário para o spawn fixo
            GameObject tempSpawn = new GameObject($"Boss_Spawn_{group.enemyPrefab.name}");
            // Usa o ponto fixo definido na struct Wave
            tempSpawn.transform.position = waves[currentWaveIndex].bossFixedSpawnPosition;
            targetSpawnPoint = tempSpawn.transform;
        }
        
        bool isInfiniteSpawn = group.count <= 0;
        int totalToSpawn = isInfiniteSpawn ? 1 : group.count; // Se infinito, o loop é infinito, senão, usa a contagem

        // Se o grupo for o Boss, salva a instância para referência global
        if (group.isBossGroup)
        {
            // Spawna o Boss uma vez, independentemente da contagem.
            currentBossInstance = SpawnEnemy(group.enemyPrefab, targetSpawnPoint);
            totalToSpawn = 0; // O Boss já nasceu, então não faz mais spawn neste loop.
            
            // Limpa o Transform temporário
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
            // O loop continua enquanto o Boss existir
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
            finalSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        
        // Retorna a instância para que possa ser armazenada
        return Instantiate(enemyPrefab, finalSpawnPoint.position, finalSpawnPoint.rotation);
    }
}
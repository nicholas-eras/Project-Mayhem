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
        public int count; // Quantos inimigos DESTE TIPO irão surgir
        public float spawnInterval; // O intervalo de tempo entre cada spawn DESTE GRUPO
        public float initialDelay; // Um atraso opcional antes que este grupo comece a surgir
    }

    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public List<EnemyGroup> enemyGroups; // Lista de "receitas" de spawn para esta onda
    }

    [Header("Configuração das Ondas")]
    [SerializeField] private List<Wave> waves;

    [Header("Referências")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Configuração de Coleta")]
    [SerializeField] private float coinCollectionTime = 1.5f;

    private int currentWaveIndex = 0;

    void Start()
    {
        // A verificação de segurança já está boa aqui.
        if (UpgradeManager.Instance == null)
        {
            Debug.LogError("UpgradeManager não encontrado. A funcionalidade de Shop/Next Wave falhará.");
        }
        // Inicia a primeira onda.
        StartCoroutine(StartNextWave());
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
        
        // This now works because the event is declared.
        OnNewWaveStarted?.Invoke(currentWave.waveName);
        
        // --- Lógica de Spawn ---
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
        
        // Espera todos os inimigos serem derrotados
        while (GameObject.FindGameObjectWithTag("Enemy") != null)
        {
            yield return null;
        }

        // --- Lógica de Coleta de Moedas ---
        Coin[] remainingCoins = FindObjectsOfType<Coin>(); // Changed to FindObjectsOfType for better performance
        
        if (remainingCoins.Length > 0)
        {
            foreach (Coin coin in remainingCoins)
            {
                if (coin != null) coin.ForceAttract();
            }

            yield return new WaitForSeconds(coinCollectionTime);
            
            // Re-check for coins that might have been collected during the wait
            Coin[] coinsToDestroy = FindObjectsOfType<Coin>();
            foreach (Coin coin in coinsToDestroy)
            {
                if (coin != null)
                {
                    Destroy(coin.gameObject);
                }
            }
        }

        // 2. CHAMA O SHOP
        if (UpgradeManager.Instance != null)
        {
             UpgradeManager.Instance.ShowUpgradeScreen();
        }
        else
        {
             Debug.LogError("UpgradeManager.Instance é nulo. Pulando tela de Upgrade.");
             // Força o avanço para a próxima onda se o Manager estiver faltando
             HandleShopClosed(); 
        }
    }

    // Esta função é chamada pelo evento OnShopClosed
    private void HandleShopClosed()
    {
        currentWaveIndex++;
        // Inicia a próxima onda
        StartCoroutine(StartNextWave());
    }

    // Esta coroutine gerencia o spawn de apenas UM grupo de inimigos.
    IEnumerator SpawnEnemyGroup(EnemyGroup group)
    {
        if (group.initialDelay > 0)
        {
            yield return new WaitForSeconds(group.initialDelay);
        }

        for (int i = 0; i < group.count; i++)
        {
            SpawnEnemy(group.enemyPrefab);
            yield return new WaitForSeconds(group.spawnInterval);
        }
    }

    void SpawnEnemy(GameObject enemyPrefab)
    {
        if (spawnPoints.Length == 0) return;
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(enemyPrefab, randomSpawnPoint.position, randomSpawnPoint.rotation);
    }
    
    // FIX 3: REMOVED UNNECESSARY METHOD
    // The 'Initialize' method and 'currentUpgradeManager' field were removed.
    // Relying solely on the singleton pattern (UpgradeManager.Instance) is cleaner,
    // more reliable, and removes the bugs caused by the mixed approach.
}
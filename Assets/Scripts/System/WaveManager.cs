using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; 

public class WaveManager : MonoBehaviour
{
    public static UnityAction<string> OnNewWaveStarted;
    // NOVO! Esta classe representa um sub-grupo de inimigos dentro de uma onda.
    [System.Serializable]
    public class EnemyGroup
    {
        public GameObject enemyPrefab;
        public int count; // Quantos inimigos DESTE TIPO irão surgir
        public float spawnInterval; // O intervalo de tempo entre cada spawn DESTE GRUPO
        public float initialDelay; // Um atraso opcional antes que este grupo comece a surgir
    }

    // A classe Wave agora usa uma lista de EnemyGroups em vez de uma lista de prefabs.
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

    private int currentWaveIndex = 0;
    // private bool waveIsRunning = false;

    // Tempo extra para esperar as moedas serem coletadas (em segundos)
    [Header("Configuração de Coleta")]
    [SerializeField] private float coinCollectionTime = 1.5f;

    void Start()
    {
        StartCoroutine(StartNextWave());
    }

    void OnEnable()
    {
        // Se inscreve para ouvir quando a loja fechar
        UpgradeManager.OnShopClosed += HandleShopClosed;
    }

    void OnDisable()
    {
        // Cancela a inscrição
        UpgradeManager.OnShopClosed -= HandleShopClosed;
    }

    IEnumerator StartNextWave()
    {
        if (currentWaveIndex >= waves.Count)
        {
            yield break;
        }

        // waveIsRunning = true;
        Wave currentWave = waves[currentWaveIndex];
        OnNewWaveStarted?.Invoke(currentWave.waveName);
        // --- NOVA LÓGICA DE SPAWN ---
        // Vamos iniciar uma coroutine para CADA grupo de inimigos.
        // Isso permite que vários grupos surjam em paralelo, cada um com seu próprio timer!
        List<Coroutine> runningSpawners = new List<Coroutine>();
        foreach (var group in currentWave.enemyGroups)
        {
            // Inicia a coroutine de spawn para o grupo e guarda uma referência a ela.
            Coroutine spawner = StartCoroutine(SpawnEnemyGroup(group));
            runningSpawners.Add(spawner);
        }

        // Espera todas as coroutines de spawn terminarem.
        foreach (var spawner in runningSpawners)
        {
            yield return spawner;
        }
        
        // Espera todos os inimigos serem derrotados para avançar
        while (GameObject.FindGameObjectWithTag("Enemy") != null)
        {
            yield return null;
        }

        // waveIsRunning = false;

        Coin[] remainingCoins = GameObject.FindObjectsOfType<Coin>();
        
        if (remainingCoins.Length > 0)
        {
            // 2. Para cada moeda, força a atração
            foreach (Coin coin in remainingCoins)
            {
                coin.ForceAttract();
            }

            // 3. Espera um tempo fixo para a animação de coleta
            yield return new WaitForSeconds(coinCollectionTime);
            
            // Opcional, mas recomendado: Destrói qualquer moeda que ainda não foi coletada
            foreach (Coin coin in remainingCoins)
            {
                if (coin != null)
                {
                    Destroy(coin.gameObject);
                }
            }
        }

        // AQUI é onde você mostraria a tela de UPGRADES
        // AGORA, EM VEZ DE INICIAR A PRÓXIMA ONDA, NÓS ABRIMOS A LOJA!
        UpgradeManager.Instance.ShowUpgradeScreen();
    }

    // Esta função é chamada pelo evento OnShopClosed
    private void HandleShopClosed()
    {
        currentWaveIndex++;
        // Só agora iniciamos a próxima onda
        StartCoroutine(StartNextWave());
    }

    // Esta coroutine gerencia o spawn de apenas UM grupo de inimigos.
    IEnumerator SpawnEnemyGroup(EnemyGroup group)
    {
        // Espera o delay inicial, se houver
        if (group.initialDelay > 0)
        {
            yield return new WaitForSeconds(group.initialDelay);
        }

        // Loop para criar os inimigos do grupo
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
}
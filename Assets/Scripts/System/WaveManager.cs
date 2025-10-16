using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
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
    private bool waveIsRunning = false;

    void Start()
    {
        StartCoroutine(StartNextWave());
    }

    IEnumerator StartNextWave()
    {
        if (currentWaveIndex >= waves.Count)
        {
            Debug.Log("Todas as ondas completas! VOCÊ VENCEU!");
            yield break;
        }

        waveIsRunning = true;
        Wave currentWave = waves[currentWaveIndex];
        Debug.Log("Iniciando onda: " + currentWave.waveName);

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
        
        Debug.Log("Todos os inimigos da onda foram criados. Aguardando derrota...");

        // Espera todos os inimigos serem derrotados para avançar
        while (GameObject.FindGameObjectWithTag("Enemy") != null)
        {
            yield return null;
        }

        Debug.Log("Onda " + currentWave.waveName + " completa!");
        waveIsRunning = false;
        
        // AQUI é onde você mostraria a tela de UPGRADES

        currentWaveIndex++;
        yield return new WaitForSeconds(3f);
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
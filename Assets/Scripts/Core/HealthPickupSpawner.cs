using UnityEngine;
using System.Collections; // Necessário para Coroutines

public class HealthPickupSpawner : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("O prefab do item de cura a ser instanciado.")]
    [SerializeField] private GameObject healthPickupPrefab;

    [Tooltip("O objeto pai que contém o BottomLeft_Marker e o TopRight_Marker.")]
    [SerializeField] private Transform mapBoundsParent;

    [Header("Configuração de Spawn por Tempo")]
    [Tooltip("A chance (0 a 1) de um item de cura spawnar a cada checagem.")]
    [Range(0f, 1f)]
    [SerializeField] private float spawnCheckChance = 0.15f; // 15% de chance a cada checagem

    [Tooltip("O tempo mínimo e máximo (em segundos) entre as checagens de spawn.")]
    [SerializeField] private Vector2 timeBetweenChecks = new Vector2(5f, 15f); // 5s a 15s

    [Header("Limites do Mapa")]
    [Tooltip("Distância de segurança da borda do mapa para evitar spawn fora de vista.")]
    [SerializeField] private float borderPadding = 1f;
    
    [SerializeField] private float pickupLifetime = 15f; // <-- NOVO CAMPO DE DURAÇÃO
    
    private Transform bottomLeftMarker;
    private Transform topRightMarker;
    private Coroutine spawnRoutine;

    void Start()
    {
        InitializeBounds();
        
        // Inicia o processo de spawn periódico se os limites forem válidos
        if (bottomLeftMarker != null && topRightMarker != null && spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(RandomSpawnRoutine());
        }
        else if (bottomLeftMarker == null || topRightMarker == null)
        {
             Debug.LogError("[Spawner] Spawner não inicializado devido a marcadores de limite ausentes.");
        }
    }

    private void InitializeBounds()
    {
        // 1. Garante que o objeto pai foi configurado ou tenta encontrá-lo
        if (mapBoundsParent == null)
        {
            GameObject boundsGO = GameObject.Find("--- MAP BOUNDS ---");
            if (boundsGO != null)
            {
                mapBoundsParent = boundsGO.transform;
            }
        }
        
        if (mapBoundsParent == null) return;

        // 2. Encontra os marcadores filhos
        bottomLeftMarker = mapBoundsParent.Find("BottomLeft_Marker");
        topRightMarker = mapBoundsParent.Find("TopRight_Marker");
    }

    /// <summary>
    /// Rotina que verifica periodicamente se deve spawnar um item.
    /// </summary>
    private IEnumerator RandomSpawnRoutine()
    {
        while (true) // Roda continuamente
        {
            // 1. Espera um tempo aleatório
            float waitTime = Random.Range(timeBetweenChecks.x, timeBetweenChecks.y);
            yield return new WaitForSeconds(waitTime);

            // 2. Verifica a chance de spawn
            if (Random.value <= spawnCheckChance)
            {
                SpawnHealthPickupAtRandomPosition();
            }
        }
    }

    /// <summary>
    /// Spawna um item de cura em uma posição aleatória dentro dos limites do mapa.
    /// </summary>
    public GameObject SpawnHealthPickupAtRandomPosition()
    {
        if (healthPickupPrefab == null || bottomLeftMarker == null || topRightMarker == null)
        {
            Debug.LogError("[Spawner] Configurações do Spawner incompletas para spawn.");
            return null;
        }

        // 1. Obtém e ajusta as coordenadas X e Y do retângulo com padding
        float minX = bottomLeftMarker.position.x + borderPadding;
        float maxX = topRightMarker.position.x - borderPadding;
        float minY = bottomLeftMarker.position.y + borderPadding;
        float maxY = topRightMarker.position.y - borderPadding;

        // 2. Garante que os limites não estejam invertidos
        if (minX > maxX) { float temp = minX; minX = maxX; maxX = temp; }
        if (minY > maxY) { float temp = minY; minY = maxY; maxY = temp; }
        
        // 3. Verifica se a área de spawn é minimamente válida após o padding
        if (maxX <= minX || maxY <= minY)
        {
             Debug.LogWarning("[Spawner] Área de spawn inválida após o Padding. Ajuste os marcadores ou o Padding.");
             return null;
        }
        
        // 4. Calcula a posição de spawn aleatória
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);

        // Usa o Z do próprio spawner ou um valor fixo (ex: 0)
        Vector3 spawnPosition = new Vector3(randomX, randomY, -0.1f);

        // 5. Instancia o item
        GameObject newPickup = Instantiate(healthPickupPrefab, spawnPosition, Quaternion.identity);
        
        // 6. Configura o tempo de vida do item
        HealthPickup pickupComponent = newPickup.GetComponent<HealthPickup>();
        if (pickupComponent != null)
        {
            pickupComponent.Setup(pickupLifetime); // <-- CHAMA O SETUP COM A DURAÇÃO
        }
        
        return newPickup;
    }

    // Gizmo para visualizar a área de spawn
    private void OnDrawGizmos()
    {
        // Tenta pegar os marcadores se não estiver no modo Play
        if (bottomLeftMarker == null || topRightMarker == null)
        {
            InitializeBounds();
        }
        
        if (bottomLeftMarker != null && topRightMarker != null)
        {
            Gizmos.color = Color.green;
            
            // Centraliza o cubo e calcula o tamanho
            Vector3 center = (bottomLeftMarker.position + topRightMarker.position) / 2f;
            float sizeX = Mathf.Abs(topRightMarker.position.x - bottomLeftMarker.position.x);
            float sizeY = Mathf.Abs(topRightMarker.position.y - bottomLeftMarker.position.y);
            
            // Desenha a área total do mapa
            Gizmos.DrawWireCube(center, new Vector3(sizeX, sizeY, 0));

            // Desenha a área de spawn (com padding)
            Gizmos.color = Color.yellow;
            float spawnSizeX = sizeX - 2 * borderPadding;
            float spawnSizeY = sizeY - 2 * borderPadding;

            if (spawnSizeX > 0 && spawnSizeY > 0)
            {
                Gizmos.DrawWireCube(center, new Vector3(spawnSizeX, spawnSizeY, 0));
            }
        }
    }
}
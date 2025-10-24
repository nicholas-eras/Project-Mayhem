using UnityEngine;
using System.Linq; // <-- ADICIONAR ESTA LINHA

public enum DeathEffect
{
    None,            // Destrói normalmente
    InstantiatePrefab // Instancia um prefab (Área de Efeito, Partículas, etc.)
}

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float collisionDamage = 10f;

    [Tooltip("Distância mínima do jogador para que o inimigo pare de se mover.")]
    [SerializeField] private float stopDistance = 0f; // Distância de parada (o seu "enemydistance")

    [Header("Tipo de Dano")]
    [SerializeField] private DamageType damageType = DamageType.Standard;

    [Header("Configurações de Chefe")]
    [Tooltip("Marque se este inimigo for um chefe.")]
    public bool isBoss = false;

    [Tooltip("O prefab da barra de vida que será instanciado.")]
    public GameObject worldHealthBarPrefab;

    [Tooltip("O ponto de ancoragem para a barra de vida (um objeto filho vazio).")]
    public Transform healthBarMountPoint;

    [Header("Efeito de Morte")]
    [Tooltip("O que o inimigo deve fazer ao ser destruído.")]
    [SerializeField] private DeathEffect deathEffect = DeathEffect.None;

    [Tooltip("O Prefab a ser instanciado (Ex: PoisonCloudZone, FireZone).")]
    [SerializeField] private GameObject deathPayloadPrefab;

    [Tooltip("A duração que o Prefab de Efeito deve persistir (Passado via Setup).")]
    [SerializeField] private float areaEffectDuration = 5f; // Duração para AreaEffectZone.cs
    [Tooltip("A escala que o Prefab de Efeito de Morte deve ter quando instanciado.")]
    [SerializeField] private float deathPayloadScale = 1.0f; // <-- NOVO CAMPO AQUI!
    
    private Transform playerTarget;

    [Header("Efeito de Status ATIVO (Aura)")] 
    [Tooltip("O Prefab que contém o componente AreaEffectZone para ser carregado pelo inimigo.")]
    [SerializeField] private GameObject activePayloadPrefab; // Prefab da Aura/Nuvem (ex: FireCloud)
    
    [Tooltip("A escala que o Prefab de Efeito deve ter quando instanciado.")]
    [SerializeField] private float payloadScale = 1.0f; // Novo campo para escala
    private GameObject currentActivePayloadInstance;

    private SpriteRenderer enemyRenderer; 

    [Tooltip("Marque se esta é uma parte do chefe principal que compartilha vida.")]
    public bool isGreaterBoss = false; // <--- NOVA FLAG AQUI!

    void Start()
    {
        ChooseRandomTarget();
        ActivateActivePayload();
        enemyRenderer = GetComponent<SpriteRenderer>();
    }

    // Função de Morte Chamada pelo HealthSystem
    public void Die()
    {
        // 1. Checa se o efeito de morte está ativo
        if (deathEffect == DeathEffect.InstantiatePrefab)
        {
            HandleInstantiateDeathPayload();
        }
        
        // Finalmente, remove o objeto do inimigo
        Destroy(gameObject);
    }

    private void HandleInstantiateDeathPayload()
    {
        if (deathPayloadPrefab == null)
        {
            Debug.LogWarning($"[EC] {gameObject.name} FALHA: Death Payload Prefab esta nulo.");
            return;
        }

        // 1. Instancia o Prefab
        GameObject payloadGO = Instantiate(deathPayloadPrefab, transform.position, Quaternion.identity);
        payloadGO.transform.localScale = Vector3.one * deathPayloadScale;
        
        // 2. Tenta configurar o Prefab como uma Zona de Efeito
        AreaEffectZone effectZone = payloadGO.GetComponent<AreaEffectZone>();

        if (effectZone != null)
        {
            effectZone.Setup(areaEffectDuration);
            // NOVO LOG: Confirma o Setup
        }
        else
        {
            Debug.LogWarning($"[EC] Payload instanciado, mas nao contem AreaEffectZone. Setup ignorado.");
        }
    }
    private void ActivateActivePayload()
    {
        if (activePayloadPrefab == null) return;
        if (currentActivePayloadInstance != null) return;
        
        // 1. Instancia o prefab de efeito (que contém AreaEffectZone e Collider) como FILHO
        // Isso garante que ele se mova com o inimigo.
        GameObject payloadGO = Instantiate(activePayloadPrefab, transform.position, Quaternion.identity, transform);
        currentActivePayloadInstance = payloadGO;
        
        // 2. Ajusta a escala do prefab
        payloadGO.transform.localScale = Vector3.one * payloadScale;

        // === GARANTIA DE ORDEM DE RENDERIZAÇÃO (FRENTE) ===
        // 1. Obtém todos os Renderers no payload (pode ser Sprite ou ParticleSystem)
        Renderer[] payloadRenderers = payloadGO.GetComponentsInChildren<Renderer>();
        
        // 2. Garante que o inimigo tem um renderer para comparação
        if (enemyRenderer == null) 
        {
            enemyRenderer = GetComponent<SpriteRenderer>(); 
        }

        if (enemyRenderer != null)
        {
            foreach (Renderer payloadRenderer in payloadRenderers)
            {
                // Copia a Sorting Layer
                payloadRenderer.sortingLayerID = enemyRenderer.sortingLayerID;
                
                // Define uma ordem maior para garantir que seja desenhado na frente (ex: +10)
                payloadRenderer.sortingOrder = enemyRenderer.sortingOrder + 10; 
            }
        }
        else
        {
            // Fallback (Se não for sprite, ajuste o Z local)
            payloadGO.transform.localPosition = new Vector3(0, 0, -0.1f);
        }
        // ==================================================

        // 3. Garante que o AreaEffectZone não se autodestrua
        // O AreaEffectZone deve ser modificado para IGNORAR a autodestruição se for filho.
        // Ou, você pode forçar o cloudDuration no AreaEffectZone a um valor muito alto/negativo via Setup:

        AreaEffectZone effectZone = payloadGO.GetComponent<AreaEffectZone>();
        if (effectZone != null)
        {
            // Chamando Setup para definir a duração para um valor alto (ex: 9999) 
            // ou usando uma sobrecarga que diz 'nao destruir'.
            // Vamos usar Setup(9999f) para simular uma duracao "infinita" enquanto o inimigo vive.
            effectZone.Setup(9999f); 
        }
    }
    void Update()
    {
        if (playerTarget == null) return;

        // 1. Calcular a distância atual até o jogador
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // 2. Mover-se Apenas se a distância for maior que a stopDistance
        if (distanceToPlayer > stopDistance)
        {
            float step = moveSpeed * Time.deltaTime;
            // Move o inimigo em direção ao jogador
            transform.position = Vector2.MoveTowards(transform.position, playerTarget.position, step);
        }
        // Nota: Se a distância for menor ou igual a stopDistance, o inimigo fica parado,
        // permitindo que o EnemyShooter cuide do tiro.
    }

    // Esta é a única função de dano necessária para colisões.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                // Cria o pacote de dano de colisão.
                DamageInfo damagePacket = new DamageInfo(collisionDamage, damageType);

                // Tenta causar dano, passando a identidade do inimigo (gameObject) como fonte.
                playerHealth.TakeDamage(damagePacket, gameObject);
            }
        }
    }

    void ChooseRandomTarget()
    {
        GameObject[] possibleTargets = GameObject.FindGameObjectsWithTag("Player");
        
        if (possibleTargets.Length > 0)
        {
            // Escolhe um alvo aleatório
            int randomIndex = Random.Range(0, possibleTargets.Length);
            playerTarget = possibleTargets[randomIndex].transform;
            
            // Ou se quiser o MAIS PRÓXIMO:
            playerTarget = possibleTargets.OrderBy(t => Vector3.Distance(t.transform.position, transform.position)).First().transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Desenha um círculo na posição do inimigo
        Gizmos.color = Color.yellow; // Cor para a distância de parada (ex: Amarelo)

        // O círculo representa o alcance de parada. 
        // O inimigo para quando o Player entra DENTRO deste círculo.
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // Opcional: Desenhar a direção para onde o inimigo está olhando (para debug)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * (stopDistance + 0.5f));
    }
}
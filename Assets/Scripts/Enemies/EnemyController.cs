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
    [SerializeField] private int collisionDamage = 10;

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

    private Transform playerTarget;

    void Start()
    {
        ChooseRandomTarget();
    }

    // Função de Morte Chamada pelo HealthSystem
    public void Die()
{
    // 1. Checa se o efeito de morte está ativo
    if (deathEffect == DeathEffect.InstantiatePrefab)
    {
        HandleInstantiateDeathPayload();
    }
    
    // 2. Loga a destruição final
    Debug.Log($"[EC] {gameObject.name}: Destruicao final."); 

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
    
    // NOVO LOG: Confirma a instanciação
    Debug.Log($"[EC] SUCESSO: Instanciando payload '{deathPayloadPrefab.name}' em {transform.position}"); 

    // 1. Instancia o Prefab
    GameObject payloadGO = Instantiate(deathPayloadPrefab, transform.position, Quaternion.identity);

    // 2. Tenta configurar o Prefab como uma Zona de Efeito
    AreaEffectZone effectZone = payloadGO.GetComponent<AreaEffectZone>();
    
    if (effectZone != null)
    {
        effectZone.Setup(areaEffectDuration); 
        // NOVO LOG: Confirma o Setup
        Debug.Log($"[EC] Setup da AreaEffectZone concluido. Duracao: {areaEffectDuration}s."); 
    }
    else
    {
        Debug.LogWarning($"[EC] Payload instanciado, mas nao contem AreaEffectZone. Setup ignorado.");
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
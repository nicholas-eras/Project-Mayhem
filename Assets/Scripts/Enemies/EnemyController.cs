using UnityEngine;

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

    private Transform playerTarget;

    void Start()
    {
        // Encontra o jogador pelo Tag "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
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
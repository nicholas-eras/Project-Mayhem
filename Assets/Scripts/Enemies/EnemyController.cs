using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private int collisionDamage = 10;

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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
        }
    }

    void Update()
    {
        if (playerTarget == null) return;

        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector2.MoveTowards(transform.position, playerTarget.position, step);
    }

    // Esta é a única função de dano necessária.
    // Ela simplesmente "bate na porta" do HealthSystem do jogador.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                // Cria o pacote de dano.
                DamageInfo damagePacket = new DamageInfo(collisionDamage, damageType);

                // Tenta causar dano, passando a identidade do inimigo (gameObject).
                playerHealth.TakeDamage(damagePacket, gameObject);
            }
        }
    }
}
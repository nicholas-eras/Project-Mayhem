using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float moveSpeed = 4f;

    // Variáveis para guardar referências
    private Transform playerTarget;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Encontra o GameObject do jogador pela sua Tag e guarda a referência do seu Transform.
        // É melhor fazer isso no Start do que no Update para economizar processamento.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
        }
    }
    
    void FixedUpdate()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
       
        transform.position = Vector2.MoveTowards(
            transform.position,
            playerTarget.position,
            moveSpeed * Time.fixedDeltaTime
        );
        
    }

    // Este método é chamado quando outro Collider2D entra no nosso.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Se colidimos com o jogador...
        if (collision.gameObject.CompareTag("Player"))
        {
            // ...causamos dano nele.
            // (Ainda não implementamos, mas a lógica ficaria aqui)
            // collision.gameObject.GetComponent<HealthSystem>().TakeDamage(10);
            
            // Opcional: o inimigo pode se destruir ao colidir
            // Destroy(gameObject);
        }
    }
}
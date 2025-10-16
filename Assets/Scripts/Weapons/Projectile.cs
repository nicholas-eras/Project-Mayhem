using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 2f; // Tempo em segundos antes de se destruir
    [SerializeField] private float damage = 1f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Dispara o projétil para "frente" (no seu eixo Y local)
        rb.velocity = transform.up * speed;
        // Destrói o projétil depois de 'lifetime' segundos para não poluir a cena
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Verificamos se o objeto que atingimos tem a tag "Enemy"
        if (other.CompareTag("Enemy"))
        {
            // Tentamos pegar o componente HealthSystem do objeto atingido
            HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
            
            // Se o inimigo realmente tem um sistema de vida...
            if (enemyHealth != null)
            {
                // ...chamamos a função para causar dano!
                enemyHealth.TakeDamage(damage); // Causa 5 de dano. Você pode tornar isso uma variável.
            }
        }
        
        // O projétil se destrói ao atingir QUALQUER COISA (exceto outros triggers que não sejam inimigos)
        // Para evitar que o projétil seja destruído por coisas que não deveria, podemos adicionar uma checagem.
        // if (!other.isTrigger) { Destroy(gameObject); }
        // Por enquanto, vamos manter simples:
        Destroy(gameObject);
    }
}
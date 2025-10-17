using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 2f;
    
    // O dano agora é uma variável privada, definida pela arma.
    private float damageAmount; 

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        rb.velocity = transform.right * speed;
        Destroy(gameObject, lifetime);
    }
    
    // NOVA FUNÇÃO: A arma chama esta função para definir o dano.
    public void SetDamage(float damage)
    {
        damageAmount = damage;
    }

    // FUNÇÃO ATUALIZADA
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                // 1. Cria o pacote de dano com o valor que recebemos da arma.
                DamageInfo damagePacket = new DamageInfo(damageAmount, DamageType.Standard);

                // 2. Chama a função TakeDamage. Como é um projétil, não passamos
                //    o segundo parâmetro (damageSource), então nenhum cooldown será aplicado ao inimigo.
                enemyHealth.TakeDamage(damagePacket);
                
                Destroy(gameObject);

            }
        }
        
        // Se o objeto que atingimos não for um trigger (como uma parede), o projétil se destrói.
        // Isso evita que ele seja destruído por triggers como a área de coleta de moedas.
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
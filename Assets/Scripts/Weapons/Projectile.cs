using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Tornar todas as variáveis privadas.
    private float speed = 20f;     // Valor padrão (se não configurado)
    private float lifetime = 2f;   // Valor padrão (se não configurado)
    private float damageAmount; 

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    // FUNÇÃO USADA PELO PLAYER (WeaponController/AutoShooter)
    public void SetDamage(float damage)
    {
        damageAmount = damage;
    }

    // FUNÇÃO USADA PELO INIMIGO (EnemyShooter)
    // Para configurar TODOS os atributos de uma vez
    public void Configure(float damage, float spd, float lt)
    {
        damageAmount = damage;
        speed = spd;
        lifetime = lt;
    }

    void Start()
    {
        // Se for o projétil do Inimigo (usou Configure), usaremos os valores setados.
        // Se for o projétil do Player (usou SetDamage), usaremos os valores padrão (speed, lifetime)
        // e o dano setado.
        
        if (rb != null)
        {
            rb.velocity = transform.right * speed;
        }

        // Destruição programada
        Destroy(gameObject, lifetime);
    }

    // ... (restante da função OnTriggerEnter2D) ...
    void OnTriggerEnter2D(Collider2D other)
    {
        // Lógica para projéteis do PLAYER atingindo INIMIGOS
        if (other.CompareTag("Enemy"))
        {
            HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                // Causa dano em inimigos
                DamageInfo damagePacket = new DamageInfo(damageAmount, DamageType.Standard);
                enemyHealth.TakeDamage(damagePacket);
                
                Destroy(gameObject);
                return;
            }
        }
        // Lógica para projéteis do INIMIGO atingindo o PLAYER
        else if (other.CompareTag("Player"))
        {
             HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                // Causa dano no jogador
                DamageInfo damagePacket = new DamageInfo(damageAmount, DamageType.Standard); 
                playerHealth.TakeDamage(damagePacket); 
                
                Destroy(gameObject);
                return;
            }
        }
        
        // Se o objeto que atingimos não for um trigger (como uma parede ou cenário sólido), o projétil se destrói.
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Tornar todas as variáveis privadas.
    private float speed = 20f;     // Valor padrão
    private float lifetime = 2f;   // Valor padrão
    private float damageAmount;

    // NOVO: Adicione campos para o efeito de veneno (se este projétil for venenoso)
    [Header("Configuração de Veneno")]
    [Tooltip("Defina a duração e o dano por tick se este projétil for venenoso.")]
    [SerializeField] private bool isPoisonous = false; // Flag para indicar que aplica veneno
    [SerializeField] private bool isFlames = false; // Flag para indicar que aplica veneno
    [SerializeField] private float poisonTickDamage = 2f;
    [SerializeField] private float poisonDuration = 5f;
    [SerializeField] private float poisonTickInterval = 1f;
    [SerializeField] private bool destroyOnCollision = true;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // FUNÇÕES DE CONFIGURAÇÃO (EXISTENTES)
    public void SetDamage(float damage)
    {
        damageAmount = damage;
    }

    public void Configure(float damage, float spd, float lt)
    {
        damageAmount = damage;
        speed = spd;
        lifetime = lt;
    }

    void Start()
    {
        if (rb != null)
        {
            rb.velocity = transform.right * speed;
        }
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }

    // FUNÇÃO DE COLISÃO
    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. LÓGICA DO PLAYER ATINGINDO INIMIGOS
        if (other.CompareTag("Enemy"))
        {
            HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                // Inimigos não têm PoisonEffect, então aplicamos dano STANDARD
                DamageInfo damagePacket = new DamageInfo(damageAmount, DamageType.Standard);
                enemyHealth.TakeDamage(damagePacket);
            }
            // Projétil se destrói
            Destroy(gameObject);
            return;
        }

        // 2. LÓGICA DO INIMIGO ATINGINDO O PLAYER
        else if (other.CompareTag("Player"))
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();

            if (playerHealth != null)
            {
                // CRIA O PACOTE DE DANO DE IMPACTO ANTES DE TUDO
                DamageInfo impactDamagePacket = new DamageInfo(damageAmount, DamageType.Standard);

                bool statusApplied = false;

                // VERIFICA SE DEVE APLICAR O EFEITO DE VENENO
                if (isPoisonous)
                {
                    PoisonEffect poisonEffect = other.GetComponent<PoisonEffect>();
                    if (poisonEffect != null)
                    {
                        // === CORREÇÃO 1: APLICAR DANO DE IMPACTO AQUI ===
                        playerHealth.TakeDamage(impactDamagePacket); 
                        // === FIM CORREÇÃO 1 ===
                        
                        poisonEffect.ApplyPoison(poisonTickDamage, poisonDuration, poisonTickInterval);
                        statusApplied = true;
                    }
                }
                // VERIFICA SE DEVE APLICAR O EFEITO DE FOGO
                else if (isFlames)
                {
                    // ATENÇÃO: O nome do componente aqui ainda é 'poisonEffect' - corrija para 'fireEffect' para clareza, se necessário.
                    FireEffect fireEffect = other.GetComponent<FireEffect>(); 
                    
                    if (fireEffect != null)
                    {
                        // === CORREÇÃO 2: APLICAR DANO DE IMPACTO AQUI ===
                        playerHealth.TakeDamage(impactDamagePacket);
                        // === FIM CORREÇÃO 2 ===

                        fireEffect.ApplyFire(poisonTickDamage, poisonDuration, poisonTickInterval);
                        statusApplied = true;
                    }
                }

                // SE NENHUM STATUS FOI APLICADO (projétil normal), aplica o dano STANDARD.
                if (!statusApplied) 
                {
                    // Este é o bloco ORIGINAL (que agora é o fallback)
                    playerHealth.TakeDamage(impactDamagePacket);
                }
            }

            // Projétil se destrói
            if (destroyOnCollision)
            {
                Destroy(gameObject);
            }
            return;
        }

        // 3. Destruição contra objetos não-trigger (paredes)
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
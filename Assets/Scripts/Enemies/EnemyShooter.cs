using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Configurações de Disparo")]
    [Tooltip("O prefab do projétil (deve conter o script Projectile).")]
    [SerializeField] private GameObject projectilePrefab;
    
    [Tooltip("O ponto de onde o projétil será disparado (um objeto filho vazio).")]
    [SerializeField] private Transform firePoint;

    [Header("Estatísticas do Projétil")]
    [Tooltip("A quantidade de dano que este projétil causará.")]
    [SerializeField] private float projectileDamage = 5f;
    
    [Tooltip("A velocidade com que o projétil se moverá.")]
    [SerializeField] private float projectileSpeed = 10f;
    
    [Tooltip("O tempo, em segundos, que o projétil existirá antes de ser destruído.")]
    [SerializeField] private float projectileLifetime = 3f;

    [Header("Alcance e Cadência")]
    [Tooltip("O tempo mínimo, em segundos, entre cada tiro.")]
    [SerializeField] public float fireRateInterval = 1f;
    
    [Tooltip("A distância máxima do jogador para que o inimigo comece a atirar (Shoot Distance).")]
    [SerializeField] public float shootingRange = 10f;

    public Transform playerTarget;
    private float nextFireTime;
    private PatternShooter patternShooter; 

    void Awake()
    {
        // Tenta obter o PatternShooter (se estiver anexado)
        patternShooter = GetComponent<PatternShooter>(); 
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
        }

        nextFireTime = Time.time;
    }
    
    void Update()
    {
        if (playerTarget == null || firePoint == null || projectilePrefab == null)
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= shootingRange)
        {
            // O PatternShooter lida com a mira/rotação, a menos que não esteja anexado.
            if (patternShooter == null)
            {
                AimAtPlayer(); 
            }

            if (Time.time >= nextFireTime)
            {
                // Verifica se o padrão é Espiral (Corrotina) para gerenciar o cooldown corretamente.
                bool isSpiral = patternShooter != null && patternShooter.currentPatternRoutine != null;

                // CRUCIAL: Chama o PatternShooter se:
                // 1. Ele não existir (Shoot simples).
                // 2. O Pattern existir E for um Burst OU for uma Espiral que acabou de terminar.
                if (patternShooter != null)
                {
                    // Se for Espiral e estiver rodando, não faz nada (a corrotina controla o tiro).
                    if (isSpiral)
                    {
                        // Se for o primeiro tiro da Espiral, o ShootPattern será chamado.
                        // Se a espiral já estiver rodando, o controle fica na corrotina.
                    }
                    else
                    {
                        // Para todos os outros padrões (incluindo Espiral finalizada), chame ShootPattern.
                        patternShooter.ShootPattern(projectileDamage, projectileSpeed, projectileLifetime);
                        nextFireTime = Time.time + fireRateInterval; // Aplica o cooldown normal
                    }
                }
                else
                {
                    // PatternShooter não existe: tiro simples.
                    Shoot(); 
                    nextFireTime = Time.time + fireRateInterval;
                }
            }
        }
    }
    
    // =================================================================
    // MÉTODOS DE INSTANCIAÇÃO (PARA USO EXTERNO PELO PatternShooter)
    // =================================================================

    // 1. MÉTODO PADRÃO (VOID): Usado pela maioria dos padrões (simplesmente atira).
    public void InstantiateProjectile(Vector3 position, Quaternion rotation, float damage, float speed, float lifetime) 
    {
        if (projectilePrefab == null) return;

        GameObject projectileGO = Instantiate(projectilePrefab, position, rotation); 
        
        // Aplica correção de escala
        Vector3 originalScale = projectileGO.transform.localScale;
        originalScale.x = Mathf.Abs(originalScale.x); 
        projectileGO.transform.localScale = originalScale; 

        Projectile projectileScript = projectileGO.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Configure(damage, speed, lifetime);
        }
    }
    
    // 2. MÉTODO COM RETORNO (GameObject): Usado APENAS pela Cruz Rotatória (para setar o Parent).
    public GameObject InstantiateProjectileGo(Vector3 position, Quaternion rotation, float damage, float speed, float lifetime)
    {
        if (projectilePrefab == null) return null;

        GameObject projectileGO = Instantiate(projectilePrefab, position, rotation); 
        
        // Aplica correção de escala
        Vector3 originalScale = projectileGO.transform.localScale;
        originalScale.x = Mathf.Abs(originalScale.x); 
        projectileGO.transform.localScale = originalScale; 

        Projectile projectileScript = projectileGO.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Configure(damage, speed, lifetime);
        }
        
        return projectileGO; // Retorna o objeto instanciado
    }
    
    // =================================================================
    // MÉTODOS INTERNOS E DE CONTROLE
    // =================================================================

    private void AimAtPlayer()
    {
        if (playerTarget == null || firePoint == null) return;

        Vector2 direction = playerTarget.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // GIRA APENAS O PONTO DE TIRO
        firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        // FLIP/INVERSÃO DO CORPO
        Vector3 currentScale = transform.localScale;
        if (direction.x < 0)
        {
            currentScale.x = -Mathf.Abs(currentScale.x);
        }
        else
        {
            currentScale.x = Mathf.Abs(currentScale.x);
        }
        transform.localScale = currentScale;
    }

    public void Shoot()
    {
        // Se o PatternShooter não existe, o inimigo atira um projétil simples do FirePoint
        Quaternion rotation = firePoint.rotation;
        InstantiateProjectile(firePoint.position, rotation, projectileDamage, projectileSpeed, projectileLifetime); 
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(transform.position, shootingRange);
    }
}
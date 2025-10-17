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
    [SerializeField] private float fireRate = 1f;
    
    [Tooltip("A distância máxima do jogador para que o inimigo comece a atirar (Shoot Distance).")]
    [SerializeField] private float shootingRange = 10f;

    private Transform playerTarget;
    private float nextFireTime;

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
            AimAtPlayer();

            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    private void AimAtPlayer()
    {
        if (playerTarget == null || firePoint == null) return;

        Vector2 direction = playerTarget.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Armazena a escala atual para evitar perder os valores pequenos (0.07)
        Vector3 currentScale = transform.localScale;

        // 1. GIRA APENAS O PONTO DE TIRO (Isso não afeta o corpo, resolve o problema do tiro)
        firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        // 2. FLIP/INVERSÃO DO CORPO: Apenas muda o sinal da escala X.
        if (direction.x < 0)
        {
            // Vira para a esquerda: usa o valor negativo da escala X, mantendo Y e Z
            // Mathf.Abs garante que o valor seja 0.07, e o '-' inverte o sinal.
            currentScale.x = -Mathf.Abs(currentScale.x);
        }
        else
        {
            // Vira para a direita: usa o valor positivo da escala X
            currentScale.x = Mathf.Abs(currentScale.x);
        }

        // Aplica a escala com o flip, mantendo o 0.07 nos eixos X, Y e Z
        transform.localScale = currentScale;
    }
    
    // NO EnemyShooter.cs
    private void Shoot()
    {
        // 1. Instancia o projétil com a rotação correta
        GameObject projectileGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // 2. CRUCIAL: Neutralizar a escala X negativa, mantendo o tamanho original.
        // O firePoint herda a escala do inimigo (que pode ser negativa: -0.07).
        Vector3 originalScale = projectileGO.transform.localScale;
        
        // Força a escala X a ser POSITIVA, mantendo o valor absoluto (tamanho original)
        originalScale.x = Mathf.Abs(originalScale.x); 
        
        // Aplica a escala corrigida
        projectileGO.transform.localScale = originalScale; 

        // 3. Configura o projétil
        Projectile projectileScript = projectileGO.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.Configure(projectileDamage, projectileSpeed, projectileLifetime);
        }
        else
        {
            Debug.LogError("O prefab do projétil não contém o script 'Projectile'!");
        }
    }
    private void OnDrawGizmosSelected()
    {
        // Desenha um círculo na posição do inimigo (onde o script está)
        Gizmos.color = Color.red; // Cor para o alcance de tiro (ex: Vermelho)
        
        // O círculo representa o alcance de tiro.
        // O inimigo só atira quando o Player entra DENTRO deste círculo.
        Gizmos.DrawWireSphere(transform.position, shootingRange);
    }
}
using UnityEngine;

// Anexe este script a um GameObject filho do Player, como um "WeaponMount".
public class AutoShooter : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("O Prefab do projétil que será disparado.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("O ponto exato de onde o projétil será criado.")]
    [SerializeField] private Transform firePoint;

    [Header("Atributos da Arma")]
    [Tooltip("Tiros por segundo.")]
    [SerializeField] private float fireRate = 2f;

    [Tooltip("O alcance máximo para encontrar um alvo.")]
    [SerializeField] private float targetRange = 15f;

    [Header("Configuração de Alvo")]
    [Tooltip("Qual layer (camada) contém os inimigos?")]
    [SerializeField] private LayerMask enemyLayer;

    // Variáveis privadas para controle interno
    private Transform currentTarget;
    private float fireTimer; // Temporizador para controlar a cadência de tiro

    void Update()
    {
        // A cada frame, tentamos encontrar o alvo mais próximo
        FindTarget();
        
        // Contagem regressiva do temporizador
        fireTimer -= Time.deltaTime;

        // Se o temporizador zerou E temos um alvo, atiramos
        if (fireTimer <= 0f && currentTarget != null)
        {
            Shoot();
            // Reseta o temporizador para o próximo tiro
            fireTimer = 1f / fireRate;
        }
    }

    private void FindTarget()
    {
        // Cria um círculo invisível e pega todos os colisores na layer de inimigos que estão dentro dele
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, targetRange, enemyLayer);

        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        // Itera por todos os inimigos encontrados para descobrir qual está mais perto
        foreach (Collider2D enemyCollider in enemiesInRange)
        {
            float distanceToEnemy = Vector2.Distance(transform.position, enemyCollider.transform.position);
            if (distanceToEnemy < closestDistance)
            {
                closestDistance = distanceToEnemy;
                closestEnemy = enemyCollider.transform;
            }
        }
        
        // Define o inimigo mais próximo como nosso alvo atual
        currentTarget = closestEnemy;
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null || currentTarget == null)
        {
            return;
        }

        // Calcula a direção do WeaponMount (onde este script está) para o alvo
        Vector2 directionToTarget = (currentTarget.position - transform.position).normalized;
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;

        // Gira o próprio WeaponMount para apontar para o alvo.
        // O FirePoint, sendo filho, girará junto.
        // O "- 90f" pode ser necessário se o seu sprite da arma (não o projétil) aponta para a direita.
        // Comece sem ele. Se a arma apontar para o lado errado, adicione-o.
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Cria o projétil na posição e rotação EXATAS do FirePoint
        Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    }
    
    // Desenha um gizmo na Scene View para visualizarmos o alcance do tiro (ótimo para debug)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetRange);
    }
}
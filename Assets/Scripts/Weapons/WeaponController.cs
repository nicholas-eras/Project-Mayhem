using UnityEngine;
using System.Collections; // Necessário para Coroutines (se usar laser)

public class WeaponController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("O SpriteRenderer principal que contém o visual da arma.")]
    public SpriteRenderer mainSpriteRenderer; // <-- ADICIONE ESTA LINHA

    [Header("Referências")]
    [Tooltip("Os dados desta arma (dano, cadência, etc.)")]
    public WeaponData weaponData;

    [Tooltip("O ponto de onde os projéteis são disparados.")]
    public Transform firePoint;

    [Header("Configuração de Alvo")]
    [SerializeField] private LayerMask enemyLayer;

    private Transform currentTarget;
    private float fireTimer;

    // Variável para controlar se o tiro contínuo está ativo (para o laser)
    private bool isFiringConstant = false; 
    private Coroutine constantFireCoroutine;

    void Update()
    {
        if (weaponData == null) return; 

        FindTarget();
        AimWeapon();

        // Lógica de tiro para projéteis (Single, Spread, Double)
        if (weaponData.fireType != WeaponFireType.Constant)
        {
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f && currentTarget != null)
            {
                PerformAttack();
                fireTimer = 1f / weaponData.fireRateInterval;
            }
        }
        // Lógica de tiro para Constant (Laser)
        else
        {
            HandleConstantFire();
        }
    }

    // Função central que decide qual método de tiro executar
    void PerformAttack()
    {
        // Toca o som de tiro (apenas uma vez para o ataque)
        if (!string.IsNullOrEmpty(weaponData.shootSoundName))
        {
             AudioManager.Instance.PlaySFX(weaponData.shootSoundName); 
             // Deixei comentado para evitar erro de compilação
        }

        switch (weaponData.fireType)
        {
            case WeaponFireType.Single:
                ShootSingle();
                break;
            case WeaponFireType.Spread:
                ShootSpread();
                break;
            case WeaponFireType.Double:
                ShootDouble();
                break;
            // Constant é tratado separadamente no Update, mas deixado aqui por segurança.
            case WeaponFireType.Constant: 
                // Inicia o laser, mas a lógica de rate of fire é mais complexa.
                break; 
        }
    }

    // --- MÉTODOS DE TIRO ESPECÍFICOS ---

    // 1. Single (O original, ligeiramente renomeado)
    void ShootSingle()
    {
        InstantiateProjectile(firePoint.position, firePoint.rotation);
    }

    // 2. Spread (Seu novo método, com o ângulo do WeaponData)
    void ShootSpread()
    {
        float angle = weaponData.spreadAngle;
        Quaternion baseRotation = firePoint.rotation;

        // Projétil do centro
        InstantiateProjectile(firePoint.position, baseRotation); 
        // Projétil +Angle
        InstantiateProjectile(firePoint.position, baseRotation * Quaternion.Euler(0f, 0f, angle)); 
        // Projétil -Angle
        InstantiateProjectile(firePoint.position, baseRotation * Quaternion.Euler(0f, 0f, -angle)); 
    }

    // 3. Double (Dois projéteis com posições laterais, centrados na mira)
    void ShootDouble()
    {
        // Encontra o vetor perpendicular à direção de tiro (para os lados)
        Vector3 rightVector = firePoint.right; 
        
        // Offset de posição
        float separation = weaponData.doubleShotSeparation; 
        
        // Posição para o lado "direito"
        Vector3 pos1 = firePoint.position + rightVector * separation;
        // Posição para o lado "esquerdo"
        Vector3 pos2 = firePoint.position - rightVector * separation;

        // Instancia os dois projéteis na mesma direção (rotação do firePoint)
        InstantiateProjectile(pos1, firePoint.rotation);
        InstantiateProjectile(pos2, firePoint.rotation);
    }

    // 4. Constant (Tratamento para o laser)
    void HandleConstantFire()
    {
        // Se temos alvo E o laser não está ativo -> Inicia o laser
        if (currentTarget != null && !isFiringConstant)
        {
            isFiringConstant = true;
            // Inicia uma corrotina para aplicar dano e exibir o laser.
            constantFireCoroutine = StartCoroutine(FireConstantLaser()); 
        }
        // Se perdemos o alvo E o laser está ativo -> Para o laser
        else if (currentTarget == null && isFiringConstant)
        {
            StopConstantLaser();
        }
    }

    IEnumerator FireConstantLaser()
    {
        // **ESTE É UM SIMPLES PLACEHOLDER**
        // Em um jogo real, você usaria LineRenderer ou um objeto Laser.
        
        // Loop infinito enquanto o laser está ativo
        while (isFiringConstant)
        {
            // Lógica de dano a cada 1/fireRateInterval segundo
            // (Simulando o "Rate of Fire" como o tick de dano)
            
            // Aqui você faria o Raycast ou Overlap para causar DANO CONTÍNUO.
            // Exemplo de como usar o 'laserLength':
            // RaycastHit2D hit = Physics2D.Raycast(firePoint.position, firePoint.right, weaponData.laserLength, enemyLayer);
            // if (hit.collider != null) { 
            //     // Causar dano 'weaponData.damage * Time.deltaTime' 
            // }

            yield return new WaitForSeconds(1f / weaponData.fireRateInterval);
        }
    }

    void StopConstantLaser()
    {
        if (constantFireCoroutine != null)
        {
            StopCoroutine(constantFireCoroutine);
        }
        isFiringConstant = false;
    }

    // --- FUNÇÃO AUXILIAR ---
    
    // Funcao auxiliar para instanciar e configurar qualquer projétil
    void InstantiateProjectile(Vector3 position, Quaternion rotation)
    {
        if (weaponData.projectilePrefab == null) return;
        
        GameObject projectileInstance = Instantiate(weaponData.projectilePrefab, position, rotation);
        Projectile projectileScript = projectileInstance.GetComponent<Projectile>();
        
        if (projectileScript != null)
        {
            projectileScript.SetDamage(weaponData.damage);
        }
    }


    // --- FUNÇÕES EXISTENTES (Inalteradas) ---

    void FindTarget()
    {
        // Lógica de mira idêntica à anterior...
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, weaponData.range, enemyLayer);
        float closestDist = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (var enemy in enemiesInRange)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = enemy.transform;
            }
        }
        currentTarget = closestEnemy;
    }

    void AimWeapon()
    {
        if (currentTarget == null) return;

        Vector2 direction = (currentTarget.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnDrawGizmosSelected()
    {
        if (weaponData == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, weaponData.range);
    }
}
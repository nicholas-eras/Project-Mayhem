using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Os dados desta arma (dano, cadência, etc.)")]
    public WeaponData weaponData;

    [Tooltip("O ponto de onde os projéteis são disparados.")]
    public Transform firePoint;

    [Header("Configuração de Alvo")]
    [SerializeField] private LayerMask enemyLayer;

    private Transform currentTarget;
    private float fireTimer;

    void Update()
    {
        if (weaponData == null) return; // Segurança

        FindTarget();
        AimWeapon();

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f && currentTarget != null)
        {
            Shoot();
            fireTimer = 1f / weaponData.fireRate;
        }
    }

    void FindTarget()
    {
        // Lógica de mira idêntica à anterior, mas usando o range dos dados da arma.
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

    void Shoot()
    {
        if (weaponData.projectilePrefab == null || firePoint == null) return;

        // 1. Cria o projétil
        GameObject projectileInstance = Instantiate(weaponData.projectilePrefab, firePoint.position, firePoint.rotation);

        // 2. Tenta pegar o script do projétil
        Projectile projectileScript = projectileInstance.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            // 3. "Informa" ao projétil qual é o seu dano, vindo diretamente do WeaponData
            projectileScript.SetDamage(weaponData.damage);
        }

        // Toca o som de tiro
        if (!string.IsNullOrEmpty(weaponData.shootSoundName))
        {
            AudioManager.Instance.PlaySFX(weaponData.shootSoundName);
        }
    }

    // Gizmo para visualizar o alcance individual de cada arma
    private void OnDrawGizmosSelected()
    {
        if (weaponData == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, weaponData.range);
    }
}
using UnityEngine;
using System.Collections; // Necessário para Coroutines (se usar laser)

/// <summary>
/// Controla o comportamento de uma arma individual (mira, tiro, aplicação de stats).
/// Este script deve estar no prefab de cada arma.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("O SpriteRenderer principal que contém o visual da arma.")]
    public SpriteRenderer mainSpriteRenderer;

    [Tooltip("Os dados BASE desta arma (ScriptableObject). Contém os stats iniciais.")]
    public WeaponData weaponData; // <-- Este é o asset original

    [Tooltip("O ponto de onde os projéteis são disparados.")]
    public Transform firePoint;

    [Header("Configuração de Alvo")]
    [SerializeField] private LayerMask enemyLayer;

    // --- VARIÁVEIS INTERNAS ---
    private Transform currentTarget; // Inimigo mais próximo no alcance
    private float fireTimer;         // Contador para o próximo tiro
    private bool isFiringConstant = false; // Flag para armas de tiro contínuo (laser)
    private Coroutine constantFireCoroutine; // Referência da corrotina do laser

    // --- REFERÊNCIA AO "CHEFE" E STATS COM BÔNUS ---
    private PlayerWeaponManager ownerManager; // Referência ao PlayerWeaponManager que criou esta arma

    // Stats que serão realmente usados no jogo (calculados a partir do WeaponData + bônus do Owner)
    private float runtimeDamage;
    private float runtimeShotsPerSecond; // <-- Stat calculado: Tiros por Segundo
    private float runtimeRange;
    // --- FIM DAS NOVAS VARIÁVEIS ---

    /// <summary>
    /// Chamado pelo PlayerWeaponManager logo após a arma ser instanciada.
    /// Recebe a referência do "Chefe" e calcula os stats iniciais com bônus.
    /// </summary>
    public void Setup(PlayerWeaponManager owner)
    {
        this.ownerManager = owner; // Guarda a referência de quem criou a arma

        // Calcula os stats que a arma usará, considerando os bônus do Player
        CalculateRuntimeStats();
    }

    /// <summary>
    /// Calcula (ou recalcula) os stats atuais da arma com base nos bônus do PlayerWeaponManager.
    /// </summary>
    public void CalculateRuntimeStats()
    {
        if (weaponData == null)
        {
            Debug.LogError("WeaponData não está assignado neste WeaponController!", this);
            return; // Impede erros se o WeaponData estiver faltando
        }

        if (ownerManager != null)
        {
            // Aplica os multiplicadores do Chefe (PlayerWeaponManager) aos stats base do WeaponData
            runtimeDamage = weaponData.damage * ownerManager.damageMultiplier;

            // LÓGICA DE CADÊNCIA CORRIGIDA:
            // Assume que 'weaponData.fireRateInterval' significa Tiros por Segundo.
            // Multiplicamos pelos bônus para AUMENTAR os tiros por segundo.
            float effectiveFireRateMultiplier = Mathf.Max(0.01f, ownerManager.fireRateMultiplier); // Evita multiplicador zero/negativo
            runtimeShotsPerSecond = weaponData.fireRateInterval * effectiveFireRateMultiplier;

            runtimeRange = weaponData.range * ownerManager.rangeMultiplier;
        }
        else
        {
            // Fallback (Se por algum motivo não houver Owner, usa os stats base)
            Debug.LogWarning($"WeaponController {name} não possui Owner. Usando stats base.", this);
            runtimeDamage = weaponData.damage;
            runtimeShotsPerSecond = weaponData.fireRateInterval; // Usa o valor base como Tiros/Segundo
            runtimeRange = weaponData.range;
        }

        // Reseta o timer com a nova cadência.
        // Usa a fórmula: Intervalo = 1 / TirosPorSegundo
        if (runtimeShotsPerSecond > 0)
            fireTimer = 1f / runtimeShotsPerSecond; // Define o tempo até o próximo tiro
        else
            fireTimer = float.MaxValue; // Se a cadência for zero ou negativa, nunca atira
    }

    // --- LÓGICA PRINCIPAL DO COMPORTAMENTO DA ARMA ---

    void Update()
    {
        if (weaponData == null) return; // Segurança

        // Atualiza o alvo mais próximo baseado no alcance calculado
        FindTarget(runtimeRange);
        // Rotaciona a arma para mirar no alvo
        AimWeapon();

        // Lógica de disparo baseada no tipo da arma
        if (weaponData.fireType != WeaponFireType.Constant)
        {
            // Para armas de projétil (Single, Spread, Double)
            fireTimer -= Time.deltaTime; // Decrementa o timer
            if (fireTimer <= 0f && currentTarget != null) // Se o timer zerou e temos um alvo
            {
                PerformAttack(); // Atira

                // Reseta o timer para o próximo tiro usando a cadência calculada
                if (runtimeShotsPerSecond > 0)
                {
                    fireTimer = 1f / runtimeShotsPerSecond; // Intervalo até o próximo tiro
                }
                else
                {
                    fireTimer = float.MaxValue; // Previne loop de tiro se rate <= 0
                }
            }
        }
        else
        {
            // Para armas de tiro contínuo (Laser)
            HandleConstantFire();
        }
    }

    /// <summary>
    /// Função central que decide qual método de tiro executar com base no fireType.
    /// </summary>
    void PerformAttack()
    {
        if (weaponData == null) return;

        // Toca o som de tiro associado (se houver e o AudioManager existir)
        if (!string.IsNullOrEmpty(weaponData.shootSoundName))
        {
            AudioManager.Instance?.PlaySFX(weaponData.shootSoundName);
        }

        // Chama a função de tiro apropriada
        switch (weaponData.fireType)
        {
            case WeaponFireType.Single: ShootSingle(); break;
            case WeaponFireType.Spread: ShootSpread(); break;
            case WeaponFireType.Double: ShootDouble(); break;
            case WeaponFireType.Constant: break; // Já tratado em HandleConstantFire
        }
    }

    // --- MÉTODOS DE TIRO ESPECÍFICOS ---

    void ShootSingle()
    {
        InstantiateProjectile(firePoint.position, transform.rotation); // Usa a rotação da arma
    }

    void ShootSpread()
    {
        if (weaponData == null || firePoint == null) return;
        float angle = weaponData.spreadAngle;
        Quaternion baseRotation = transform.rotation; // Rotação atual da arma

        InstantiateProjectile(firePoint.position, baseRotation); // Centro
        InstantiateProjectile(firePoint.position, baseRotation * Quaternion.Euler(0f, 0f, angle)); // +Ângulo
        InstantiateProjectile(firePoint.position, baseRotation * Quaternion.Euler(0f, 0f, -angle)); // -Ângulo
    }

    void ShootDouble()
    {
        if (weaponData == null || firePoint == null) return;
        Vector3 rightVector = transform.right; // Direita local da arma
        float separation = weaponData.doubleShotSeparation;
        Vector3 pos1 = firePoint.position + rightVector * separation;
        Vector3 pos2 = firePoint.position - rightVector * separation;

        InstantiateProjectile(pos1, transform.rotation); // Usa a rotação da arma
        InstantiateProjectile(pos2, transform.rotation); // Usa a rotação da arma
    }

    // --- LÓGICA DO LASER (Tiro Contínuo) ---

    void HandleConstantFire()
    {
        // Se temos um alvo e o laser não está ativo, inicia a corrotina
        if (currentTarget != null && !isFiringConstant)
        {
            isFiringConstant = true;
            constantFireCoroutine = StartCoroutine(FireConstantLaser());
            // (Aqui você ativaria o visual do laser, ex: LineRenderer)
        }
        // Se perdemos o alvo e o laser estava ativo, para a corrotina
        else if (currentTarget == null && isFiringConstant)
        {
            StopConstantLaser();
        }
    }

    IEnumerator FireConstantLaser()
    {
        while (isFiringConstant) // Loop enquanto o laser estiver ativo
        {
            // Lógica de dano do laser:
            // 1. Atualizar visual do laser (LineRenderer apontando para o alvo ou na direção da arma)
            // 2. Fazer Raycast ou Overlap na direção do laser
            // RaycastHit2D hit = Physics2D.Raycast(firePoint.position, transform.right, runtimeRange, enemyLayer);
            // 3. Se atingir um inimigo:
            // if (hit.collider != null)
            // {
            //     HealthSystem enemyHealth = hit.collider.GetComponent<HealthSystem>();
            //     if (enemyHealth != null)
            //     {
            //         // Dano por segundo = Dano base * Tiros por segundo (ajustado pelo delta time se aplicado no Update)
            //         // Como estamos em uma corrotina com delay, aplicamos o dano do tick.
            //         float damagePerTick = runtimeDamage; // Ou ajuste conforme sua lógica
            //         enemyHealth.TakeDamage(new DamageInfo(damagePerTick, DamageType.Standard), gameObject); // Exemplo
            //     }
            // }

            // Espera o intervalo calculado a partir dos Tiros por Segundo
            if (runtimeShotsPerSecond > 0)
                 yield return new WaitForSeconds(1f / runtimeShotsPerSecond); // Intervalo entre "pulsos" de dano
            else
                 yield return null; // Evita loop infinito se cadência for zero
        }
    }

    void StopConstantLaser()
    {
        if (constantFireCoroutine != null)
        {
            StopCoroutine(constantFireCoroutine); // Para a corrotina de dano
        }
        isFiringConstant = false;
        // (Aqui você desativaria o visual do laser)
    }

    // --- FUNÇÃO AUXILIAR PARA CRIAR PROJÉTEIS ---

    /// <summary>
    /// Instancia um projétil na posição e rotação dadas e define seu dano.
    /// </summary>
    void InstantiateProjectile(Vector3 position, Quaternion rotation)
    {
        if (weaponData == null || weaponData.projectilePrefab == null)
        {
            Debug.LogError("Tentando instanciar projétil sem WeaponData ou Prefab!", this);
            return;
        }

        // Cria a instância do projétil
        GameObject projectileInstance = Instantiate(weaponData.projectilePrefab, position, rotation);
        // Pega o script do projétil
        Projectile projectileScript = projectileInstance.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            // Define o dano usando o valor calculado (com bônus)
            projectileScript.SetDamage(runtimeDamage);
            // (Você pode passar outras informações aqui, como quem atirou, etc.)
        }
        else
        {
            // Avisa se o prefab do projétil estiver faltando o script necessário
            Debug.LogWarning($"Prefab de projétil '{weaponData.projectilePrefab.name}' não contém o script 'Projectile'. Dano não será aplicado corretamente.", projectileInstance);
        }
    }


    // --- FUNÇÕES DE MIRA ---

    /// <summary>
    /// Encontra o inimigo mais próximo dentro do alcance calculado.
    /// </summary>
    void FindTarget(float currentRange)
    {
        currentTarget = null; // Reseta o alvo a cada frame
        // Encontra todos os colisores na camada de inimigos dentro do círculo de alcance
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, currentRange, enemyLayer);
        float closestDistSqr = Mathf.Infinity; // Usa SqrMagnitude (distância ao quadrado) para otimização

        // Itera por todos os inimigos encontrados
        foreach (var enemy in enemiesInRange)
        {
            // Calcula a distância ao quadrado (mais rápido que a raiz quadrada)
            float distSqr = (enemy.transform.position - transform.position).sqrMagnitude;
            // Se este inimigo está mais perto que o mais perto encontrado até agora...
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr; // ...atualiza a menor distância...
                currentTarget = enemy.transform; // ...e define este como o novo alvo.
            }
        }
        // Ao final do loop, 'currentTarget' será o inimigo mais próximo ou null.
    }

    /// <summary>
    /// Rotaciona a arma (o objeto com este script) para apontar para o alvo atual.
    /// </summary>
    void AimWeapon()
    {
        // Se não houver alvo ou ponto de disparo, não faz nada
        if (currentTarget == null || firePoint == null) return;

        // Calcula a direção do ponto de disparo (firePoint) para o alvo
        Vector2 direction = (currentTarget.position - firePoint.position).normalized;
        // Calcula o ângulo dessa direção em graus
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // Aplica a rotação ao Transform deste objeto (a arma inteira)
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // --- GIZMOS PARA DEBUG VISUAL NO EDITOR ---
    private void OnDrawGizmosSelected()
    {
        // Desenha um círculo ciano mostrando o alcance da arma no Editor
        // Usa o alcance calculado se possível, senão usa o alcance base do WeaponData
        float rangeToDraw = (Application.isPlaying && ownerManager != null) ? runtimeRange : (weaponData != null ? weaponData.range : 1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangeToDraw);
    }
}
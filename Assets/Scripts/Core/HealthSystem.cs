using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class HealthSystem : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    public float CurrentHealth { get { return currentHealth; } }
    
    public float MaxHealth { get { return maxHealth; } set { maxHealth = value; } }

    [Header("Regeneração (Apenas Player)")]
    [SerializeField] private bool canRegen = false;
    public float regenRate = 1f;
    public float regenInterval = 1f;

    [Header("Audio")]
    [SerializeField] private string takeDamageSoundName;
    [SerializeField] private string deathSoundName;
 
    [Header("Cooldown de Dano por Fonte")]
    [Tooltip("Por quanto tempo uma fonte de dano não pode causar dano novamente. Este é o tempo de invulnerabilidade do Player.")]
    [SerializeField] public float damageCooldown = 1f;

    private List<GameObject> sourcesOnCooldown = new List<GameObject>();

    public UnityAction<float, float> OnHealthChanged;
    public UnityEvent OnDeath;

    private bool isPlayer = false;
    private PlayerStatusEffects playerStatusEffects; 

    [Header("Configuração de Boss")]
    [Tooltip("Se for True, a vida será vinculada ao BossHealthLinker.")]
    [SerializeField] private bool isGreaterBossPart = false; // Flag para partes do Greater Boss
    private BossHealthLinker healthLinker; // Referência ao Linker Central

    void Awake()
    {
        // Se for uma parte do Greater Boss, a vida inicial é gerenciada pelo Linker
        if (!isGreaterBossPart)
        {
            currentHealth = MaxHealth;
        }
        else
        {
             // Encontra o linker no Awake (deve estar na cena)
            healthLinker = FindObjectOfType<BossHealthLinker>();
            if (healthLinker == null)
            {
                Debug.LogError("Greater Boss Part precisa de um BossHealthLinker na cena!");
            }
        }
    }
    void Start()
    {
        if (gameObject.CompareTag("Player"))
        {
            isPlayer = true;
            canRegen = true;
        }

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (canRegen)
        {
            StartCoroutine(RegenRoutine());
        }
            
        // --- FIX 1: SUBSCRIBE USING THE SINGLETON INSTANCE ---
        // Access the OnShopClosed event via UpgradeManager.Instance
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnShopClosed += FullHealthRegen;
        }
        

        EnemyController controller = GetComponent<EnemyController>();
        if (controller != null && controller.isBoss)
        {
            SetupBossHealthBar(controller);
        }

        // Se for parte do Boss, a vida inicial é a vida atual do linker
        if (isGreaterBossPart && healthLinker != null)
        {
            currentHealth = healthLinker.CurrentHealth;
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }
    }

    // --- FIX 2: ADD ONDESTROY TO UNSUBSCRIBE FROM THE EVENT ---
    // This prevents errors if the player object is destroyed but the UpgradeManager is not.
    private void OnDestroy()
    {
        if (isPlayer && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnShopClosed -= FullHealthRegen;
        }
    }

    private IEnumerator RegenRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenInterval);

            if (currentHealth < MaxHealth)
            {
                currentHealth += regenRate;
                currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
                OnHealthChanged?.Invoke(currentHealth, MaxHealth);
            }
        }
    }
    
    public void FullHealthRegen()
    {
        currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        // 2. Limpa os Efeitos de Status Negativos
        if (playerStatusEffects != null)
        {
            playerStatusEffects.CureAllStatusEffects(); // <--- CHAMADA ADICIONADA AQUI
        }
    }
    
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    private void SetupBossHealthBar(EnemyController controller)
    {
        if (controller.worldHealthBarPrefab == null || controller.healthBarMountPoint == null)
        {
            Debug.LogError("Configuração da barra de vida do chefe está incompleta para: " + name);
            return;
        }

        GameObject barInstance = Instantiate(controller.worldHealthBarPrefab, controller.healthBarMountPoint);
        WorldHealthBar healthBar = barInstance.GetComponentInChildren<WorldHealthBar>();
        
        if (healthBar != null)
        {
            OnHealthChanged += healthBar.UpdateHealth; 
        }
    }

    public void TakeDamage(DamageInfo info, GameObject damageSource = null)
    {
        // 1. CHECAGEM DE COOLDOWN/INVULNERABILIDADE (i-frames)

        // O cooldown é ativado apenas se houver uma fonte de dano (não é tick de veneno).
        bool hasDamageSource = damageSource != null;

        // Verifica se a fonte de dano está em cooldown
        bool isSourceOnCooldown = hasDamageSource && sourcesOnCooldown.Contains(damageSource);

        if (isSourceOnCooldown)
        {
            // Permite que o dano UNBLOCKABLE e de VENENO passem pelo cooldown.
            // Se for STANDARD, o dano é bloqueado (return).
            if (info.type != DamageType.Unblockable && info.type != DamageType.Poison && info.type != DamageType.Fire)
            {
                return;
            }
            // Se for Unblockable ou Poison, o código continua e o dano é aplicado.
        }

        // Se o alvo já está morto, saia
        if (currentHealth <= 0) return;

        // 2. APLICAÇÃO DE DANO E FEEDBACK

        // Toca o som de dano
        if (!string.IsNullOrEmpty(takeDamageSoundName))
        {
            // Remove os comentários se o AudioManager.Instance.PlaySFX estiver configurado.
            AudioManager.Instance.PlaySFX(takeDamageSoundName);
        }

        float damageToApply = info.damageAmount;

        if (info.type == DamageType.Poison && currentHealth - info.damageAmount < 1)
        {
            currentHealth = 1;
        }
        else if (isGreaterBossPart && healthLinker != null)
        {
            healthLinker.TakeDamage(damageToApply);
            // Atualiza a vida local apenas para a UI
            currentHealth = healthLinker.CurrentHealth;
        }                         
        else
        {
            currentHealth -= info.damageAmount;
            currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        }        

        // Atualiza a UI
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        // 3. ATIVAÇÃO DO COOLDOWN DE INVULNERABILIDADE

        // O cooldown de invulnerabilidade é aplicado APENAS se:
        // a) Houver uma fonte (para rastreá-la).
        // b) O dano NÃO for um tick de veneno (pois o veneno não deve ser bloqueado por i-frames).
        if (hasDamageSource && info.type != DamageType.Poison && info.type != DamageType.Fire)
        {
            StartCoroutine(AddSourceToCooldown(damageSource));
        }

        // 4. CHECAGEM DE MORTE
        if (currentHealth <= 0)
        {
            Die();
        }
    }        

    private IEnumerator AddSourceToCooldown(GameObject source)
    {
        sourcesOnCooldown.Add(source);
        yield return new WaitForSeconds(damageCooldown);
        sourcesOnCooldown.Remove(source);
    }

    private void Die()
    {
        OnDeath?.Invoke();

        // 1. Loga que a morte está ocorrendo
        Debug.Log($"[HS] {gameObject.name} (HP 0): Tentando iniciar a morte.");

        EnemyController enemyController = GetComponent<EnemyController>();

        if (enemyController != null)
        {
            // 2. Loga se encontrou o controlador
            Debug.Log("[HS] Chamando EnemyController.Die() para liberar payload.");
            enemyController.Die();
        }
        else
        {
            // 3. Loga se destruiu diretamente (Player, etc.)
            Debug.Log("[HS] Nao ha Controller. Destruicao direta.");
            Destroy(gameObject);
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void HealFull()
    {
        currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }
    
    // ========================================================
    // FUNÇÕES DE UPGRADE CHAMADAS PELO UPGRADEMANAGER
    // ========================================================

    public void IncreaseInvulnerabilityTime(float timeIncrease)
    {
        damageCooldown += timeIncrease;
    }

    public void IncreaseMaxHealth(float percentage)
    {
        float maxHealthIncrease = MaxHealth + percentage;
        MaxHealth = Mathf.RoundToInt(maxHealthIncrease); 
        Heal(Mathf.RoundToInt(maxHealthIncrease));
        
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }
    
    public void IncreaseRegenRate(float percentage)
    {
        regenRate += percentage;
    }
}
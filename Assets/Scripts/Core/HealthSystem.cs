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
    [SerializeField] private bool isGreaterBossPart = false;
    private BossHealthLinker healthLinker;
    [HideInInspector] public bool IsBotAgent = false; // Flag que sobrevive à desativação

    // --- NOVO: Variáveis para a lógica de morte do Jogador ---
    private bool isDead = false; // Flag para prevenir morte dupla
    private WaveManager waveManager; // Referência para saber se é wave de boss

    void Awake()
    {
        if (!isGreaterBossPart)
        {
            currentHealth = MaxHealth;
        }
        else
        {
            healthLinker = FindObjectOfType<BossHealthLinker>();
            if (healthLinker == null)
            {
                Debug.LogError("Greater Boss Part precisa de um BossHealthLinker na cena!");
            }
            currentHealth = 100000f;
        }
    }
    
    void Start()
    {
        if (gameObject.CompareTag("Player"))
        {
            isPlayer = true;
            canRegen = true;
            
            // --- NOVO: Encontra o WaveManager ---
            waveManager = FindObjectOfType<WaveManager>();
            if (waveManager == null)
            {
                Debug.LogWarning("HealthSystem do Player não encontrou o WaveManager!");
            }
        }
        
        if (isGreaterBossPart && healthLinker != null)
        {
            maxHealth = healthLinker.initialTotalHealth; 
            currentHealth = healthLinker.CurrentHealth;
            healthLinker.OnBossHealthChanged += UpdateLocalHealthFromLinker;
        }

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (canRegen)
        {
            StartCoroutine(RegenRoutine());
        }
            
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnShopClosed += FullHealthRegen;
        }
        
        EnemyController controller = GetComponent<EnemyController>();
        if (controller != null && controller.isBoss)
        {
            SetupBossHealthBar(controller);
        }
    }

    private void UpdateLocalHealthFromLinker(float newCurrentHealth, float newMaxHealth)
    {
        currentHealth = newCurrentHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth); 

        // --- MUDANÇA: Adiciona checagem de isDead ---
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    private void OnDestroy()
    {
        if (isPlayer && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnShopClosed -= FullHealthRegen;
        }
        
        if (isGreaterBossPart && healthLinker != null)
        {
            healthLinker.OnBossHealthChanged -= UpdateLocalHealthFromLinker;
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
        if (playerStatusEffects != null)
        {
            playerStatusEffects.CureAllStatusEffects();
        }
    }
    
    public void Heal(int amount)
    {
        Heal((float)amount); // Reutiliza a outra função Heal
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
        bool hasDamageSource = damageSource != null;
        bool isSourceOnCooldown = hasDamageSource && sourcesOnCooldown.Contains(damageSource);

        if (isSourceOnCooldown)
        {
            if (info.type != DamageType.Unblockable && info.type != DamageType.Poison && info.type != DamageType.Fire)
            {
                return;
            }
        }

        // --- MUDANÇA: Se o alvo já está morto, saia ---
        if (isDead || currentHealth <= 0) return;

        // 2. APLICAÇÃO DE DANO E FEEDBACK
        if (!string.IsNullOrEmpty(takeDamageSoundName))
        {
            AudioManager.Instance.PlaySFX(takeDamageSoundName); 
        }

        float damageToApply = info.damageAmount;

        // === LÓGICA DE DANO PARA GREATER BOSS ===
        if (isGreaterBossPart && healthLinker != null)
        {
            healthLinker.TakeDamage(damageToApply);
            return; 
        } 
        // ==========================================
        
        // Dano normal (inimigos e Bosses não Greater)
        else
        {
            if (info.type == DamageType.Poison && currentHealth - info.damageAmount < 1)
            {
                currentHealth = 1;
            }
            else
            {
                currentHealth -= info.damageAmount;
                currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
            }
        } 
        
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        // 3. ATIVAÇÃO DO COOLDOWN DE INVULNERABILIDADE
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

    // No seu HealthSystem, ajuste o método ResetForRetry:

    // No HealthSystem, modifique o ResetForRetry:

    public void ResetForRetry()
    {
        // 1. Restaura a vida e a flag de morte
        currentHealth = maxHealth;
        isDead = false;

        // 2. Notifica a UI para atualizar
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // 3. Reativa o GameObject se estiver desativado
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // 4. *** VERIFICA SE É BOT PELO AGENTMANAGER (MELHOR ABORDAGEM) ***
        AgentManager agent = GetComponent<AgentManager>();
        bool isBot = false;

        if (agent != null)
        {
            isBot = agent.IsPlayerBot();
            IsBotAgent = isBot; // Sincroniza com HealthSystem

        }
        else
        {
            isBot = IsBotAgent; // Usa o valor do HealthSystem como fallback
        }

        // 5. Reativa os componentes baseado se é bot ou humano
        PlayerController pc = GetComponent<PlayerController>();
        BotController bc = GetComponent<BotController>();
        PlayerWeaponManager pwm = GetComponent<PlayerWeaponManager>();

        if (pc != null)
        {
            pc.enabled = !isBot;
        }

        if (bc != null)
        {
            bc.enabled = isBot;
        }

        if (pwm != null) pwm.enabled = true;

        // 6. Limpa a lista de cooldowns
        sourcesOnCooldown.Clear();
    }

// DENTRO DE HealthSystem.cs -> Die()
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Verifica se é Player (pela tag) OU se tem o componente BotController
        bool isPlayerOrBot = gameObject.CompareTag("Player") || GetComponent<BotController>() != null;

        if (isPlayerOrBot)
        {
            // --- É O JOGADOR OU BOT ---
            // Tenta pegar os componentes
            PlayerController pc = GetComponent<PlayerController>();
            BotController bc = GetComponent<BotController>();
            PlayerWeaponManager pwm = GetComponent<PlayerWeaponManager>();

            // Desativa os componentes SE eles existirem
            if (pc != null) pc.enabled = false;
            if (bc != null) bc.enabled = false;
            if (pwm != null) pwm.enabled = false;

            // Toca o som de morte, se houver
            if (!string.IsNullOrEmpty(deathSoundName))
            {
                AudioManager.Instance?.PlaySFX(deathSoundName);
            }

            // Desativa o GameObject. Isso chamará OnDisable() no PlayerTargetable,
            // que avisará o PlayerManager.
            gameObject.SetActive(false);
        }
        else // É um Inimigo (Não Player nem Bot)
        {
            // --- É UM INIMIGO ---
            OnDeath?.Invoke(); // Dispara evento para loot, etc.

            if (!string.IsNullOrEmpty(deathSoundName))
            {
                 AudioManager.Instance?.PlaySFX(deathSoundName);
            }

            EnemyController enemyController = GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.Die(); // O EnemyController cuida do efeito de morte e Destroy
            }
            else
            {
                Destroy(gameObject); // Fallback: Apenas destrói
            }
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
        // --- MUDANÇA: Cálculo de vida corrigido ---
        // 'percentage' deve ser um multiplicador (ex: 0.2 para 20%)
        // Se 'percentage' for um valor fixo (ex: 20), a lógica abaixo está errada.
        // Assumindo que 'percentage' é o valor bruto a ser somado:
        
        float healthIncreaseAmount = percentage; // Se o upgrade.value for 20, aumenta 20.
        
        // Se o seu 'percentage' for 0.2 (20%):
        // float healthIncreaseAmount = MaxHealth * percentage; 

        MaxHealth += healthIncreaseAmount;
        Heal(healthIncreaseAmount); // Cura o jogador no mesmo montante
        
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }
    
    public void IncreaseRegenRate(float percentage)
    {
        // Mesma lógica de IncreaseMaxHealth:
        // 'percentage' é o valor bruto a ser somado (ex: 0.5)
        regenRate += percentage; 
    }
}
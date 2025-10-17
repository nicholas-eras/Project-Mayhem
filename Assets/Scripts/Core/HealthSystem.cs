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
    
    void Awake()
    {
        currentHealth = MaxHealth;
    }

    void Start()
    {
        if (gameObject.CompareTag("Player"))
        {
            isPlayer = true;
            canRegen = true;
        }

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (isPlayer)
        {
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
        }

        EnemyController controller = GetComponent<EnemyController>();
        if (controller != null && controller.isBoss)
        {
            SetupBossHealthBar(controller);
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
        if (damageSource != null && sourcesOnCooldown.Contains(damageSource))
        {
            return;
        }

        if (currentHealth <= 0) return;

        if (!string.IsNullOrEmpty(takeDamageSoundName))
        {
            AudioManager.Instance.PlaySFX(takeDamageSoundName);
        }

        currentHealth -= info.damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (damageSource != null)
        {
            StartCoroutine(AddSourceToCooldown(damageSource));
        }

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
        if (!string.IsNullOrEmpty(deathSoundName))
        {
            AudioManager.Instance.PlaySFX(deathSoundName);
        }
        Destroy(gameObject);
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
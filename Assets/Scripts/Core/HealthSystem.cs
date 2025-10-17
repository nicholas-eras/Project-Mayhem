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
    
    // MaxHealth agora é settable (propriedade)
    public float MaxHealth { get { return maxHealth; } set { maxHealth = value; } }

    // --- Configurações de Regeneração ---
    [Header("Regeneração (Apenas Player)")]
    [SerializeField] private bool canRegen = false;
    public float regenRate = 1f;        // Vida restaurada por tick (Setável pelo UpgradeManager)
    public float regenInterval = 1f;    // Tempo entre ticks (Setável pelo UpgradeManager)

    // --- Configurações de Áudio ---
    [Header("Audio")]
    [SerializeField] private string takeDamageSoundName; // <-- CORREÇÃO: ADICIONADO
    [SerializeField] private string deathSoundName;      // <-- CORREÇÃO: ADICIONADO

    [Header("Cooldown de Dano por Fonte")]
    [Tooltip("Por quanto tempo uma fonte de dano não pode causar dano novamente. Este é o tempo de invulnerabilidade do Player.")]
    [SerializeField] private float damageCooldown = 1f;

    // A nossa "lista negra" de atacantes em cooldown.
    private List<GameObject> sourcesOnCooldown = new List<GameObject>();

    public UnityAction<float, float> OnHealthChanged;
    public UnityEvent OnDeath;

    private bool isPlayer = false;
    
    void Awake()
    {
        currentHealth = MaxHealth; // Usa a propriedade
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
            // Inscrição para curar totalmente no início de cada nova onda
            UpgradeManager.OnShopClosed += FullHealthRegen;
        }

        // Tenta encontrar o EnemyController (lógica de chefe/barra de vida)
        EnemyController controller = GetComponent<EnemyController>();
        if (controller != null && controller.isBoss)
        {
            SetupBossHealthBar(controller);
        }
    }

    // Rotina de Regeneração
    private IEnumerator RegenRoutine()
    {
        while (true) // Loop infinito enquanto o objeto existir
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
    
    // Cura Total no Início da Onda
    public void FullHealthRegen()
    {
        currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }
    
    // Função de cura genérica
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // Configuração de barra de vida de chefe (assumindo a existência de EnemyController)
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
            // Note: Requer que WorldHealthBar tenha um método UpdateHealth(float current, float max)
            // Assumindo que você tem o script WorldHealthBar
            OnHealthChanged += healthBar.UpdateHealth; 
        }
    }

    // A função principal que aplica dano.
    public void TakeDamage(DamageInfo info, GameObject damageSource = null)
    {
        // Verifica se a fonte de dano está em cooldown (Invulnerabilidade ativa)
        if (damageSource != null && sourcesOnCooldown.Contains(damageSource))
        {
            return;
        }

        if (currentHealth <= 0) return;

        // Toca o som de dano
        if (!string.IsNullOrEmpty(takeDamageSoundName)) // <-- CORRIGIDO: Variável existe
        {
            // Assumindo que AudioManager.Instance é global
            AudioManager.Instance.PlaySFX(takeDamageSoundName); // <-- REMOVA O COMENTÁRIO SE FOR USAR
        }

        // Aplicamos o dano e atualizamos a UI.
        currentHealth -= info.damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        // Se uma fonte de dano foi fornecida, iniciamos seu cooldown (invulnerabilidade).
        if (damageSource != null)
        {
            StartCoroutine(AddSourceToCooldown(damageSource));
        }

        // Verificamos se o jogador morreu.
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // A coroutine que gerencia o cooldown.
    private IEnumerator AddSourceToCooldown(GameObject source)
    {
        sourcesOnCooldown.Add(source); // Adiciona o atacante à lista.
        yield return new WaitForSeconds(damageCooldown); // Espera o tempo definido.
        sourcesOnCooldown.Remove(source); // Remove o atacante da lista.
    }

    private void Die()
    {
        OnDeath?.Invoke();
        if (!string.IsNullOrEmpty(deathSoundName)) // <-- CORRIGIDO: Variável existe
        {
            AudioManager.Instance.PlaySFX(deathSoundName); // <-- REMOVA O COMENTÁRIO SE FOR USAR
        }
        Destroy(gameObject);
    }
    
    // ========================================================
    // FUNÇÕES DE UPGRADE CHAMADAS PELO UPGRADEMANAGER
    // ========================================================

    // 1. AUMENTO DO TEMPO DE INVULNERABILIDADE (DAMAGE COOLDOWN)
    public void IncreaseInvulnerabilityTime(float timeIncrease)
    {
        damageCooldown += timeIncrease;
        Debug.Log("Tempo de Invulnerabilidade aumentado! Novo Cooldown: " + damageCooldown + "s");
    }

    // 2. AUMENTO DA VIDA MÁXIMA
    public void IncreaseMaxHealth(float percentage)
    {
        float maxHealthIncrease = MaxHealth + percentage;
        MaxHealth = Mathf.RoundToInt(maxHealthIncrease); 
        Heal(Mathf.RoundToInt(maxHealthIncrease)); // Cura o novo HP
        
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }
    
    // 3. AUMENTO DA REGENERAÇÃO
    public void IncreaseRegenRate(float percentage)
    {
        regenRate += percentage;
        Debug.Log("Regeneração por Tick aumentada! Novo Rate: " + regenRate);
    }
}
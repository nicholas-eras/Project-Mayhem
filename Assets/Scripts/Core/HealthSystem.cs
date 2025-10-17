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
    public float MaxHealth { get { return maxHealth; } }

    // --- NOVO --- Configurações de Regeneração
    [Header("Regeneração (Apenas Player)")]
    [SerializeField] private bool canRegen = false;
    [SerializeField] private float regenRate = 1f; // Vida restaurada por tick
    [SerializeField] private float regenInterval = 1f; // Tempo entre ticks

    [Header("Audio")]
    [SerializeField] private string takeDamageSoundName;
    [SerializeField] private string deathSoundName;

    [Header("Cooldown de Dano por Fonte")]
    [Tooltip("Por quanto tempo uma fonte de dano específica não pode causar dano novamente.")]
    [SerializeField] private float damageCooldown = 1f;

    // A nossa "lista negra" de atacantes em cooldown.
    private List<GameObject> sourcesOnCooldown = new List<GameObject>();

    public UnityAction<float, float> OnHealthChanged;
    public UnityEvent OnDeath;

    private bool isPlayer = false;
    
    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Verifica se o objeto tem a Tag "Player" ou está na Layer "Player"
        // (A Tag é a forma mais comum de identificar o jogador)
        if (gameObject.CompareTag("Player"))
        {
            isPlayer = true;
            canRegen = true;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Se for o Player, inicia as lógicas específicas (Regen e Cura por Onda)
        if (isPlayer)
        {
            if (canRegen)
            {
                StartCoroutine(RegenRoutine());
            }
            // Inscrição para curar totalmente no início de cada nova onda
            UpgradeManager.OnShopClosed += FullHealthRegen;
        }

        // NOVA LÓGICA DE CHEFE:
        // Tenta encontrar o EnemyController neste mesmo objeto.
        EnemyController controller = GetComponent<EnemyController>();
        if (controller != null && controller.isBoss)
        {
            // Se for um chefe, chama a função para criar a barra de vida.
            SetupBossHealthBar(controller);
        }
    }

    // --- NOVO --- Rotina de Regeneração
    private IEnumerator RegenRoutine()
    {
        while (true) // Loop infinito enquanto o objeto existir
        {
            yield return new WaitForSeconds(regenInterval);

            if (currentHealth < maxHealth)
            {
                currentHealth += regenRate;
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }
    }
    
    // --- NOVO --- Cura Total no Início da Onda
    public void FullHealthRegen()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        // Opcional: Adicionar um efeito visual/sonoro de cura total.
    }

    // NOVA FUNÇÃO
    private void SetupBossHealthBar(EnemyController controller)
    {
        // Verificações de segurança
        if (controller.worldHealthBarPrefab == null || controller.healthBarMountPoint == null)
        {
            Debug.LogError("Configuração da barra de vida do chefe está incompleta para: " + name);
            return;
        }

        // 1. Cria a instância da barra de vida.
        GameObject barInstance = Instantiate(controller.worldHealthBarPrefab, controller.healthBarMountPoint);

        // 2. Encontra o script WorldHealthBar dentro da instância.
        WorldHealthBar healthBar = barInstance.GetComponentInChildren<WorldHealthBar>();
        
        // 3. "Inscreve" a função de atualização da barra no evento OnHealthChanged DESTE HealthSystem.
        if (healthBar != null)
        {
            OnHealthChanged += healthBar.UpdateHealth;
        }
    }

    // A função principal que age como nosso "porteiro".
    public void TakeDamage(DamageInfo info, GameObject damageSource = null)
    {
        // Se não houver uma fonte de dano, o dano sempre é aplicado (ex: projéteis).
        // Se houver uma fonte, verificamos se ela está na lista negra.
        if (damageSource != null && sourcesOnCooldown.Contains(damageSource))
        {
            // Se estiver na lista, o dano é bloqueado e a função termina aqui.
            return;
        }

        if (currentHealth <= 0) return;

        // Se o dano foi aceito, tocamos o som.
        if (!string.IsNullOrEmpty(takeDamageSoundName))
        {
            AudioManager.Instance.PlaySFX(takeDamageSoundName);
        }

        // Aplicamos o dano e atualizamos a UI.
        currentHealth -= info.damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Se uma fonte de dano foi fornecida, iniciamos seu cooldown.
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

    // A coroutine que gerencia a lista negra.
    private IEnumerator AddSourceToCooldown(GameObject source)
    {
        sourcesOnCooldown.Add(source); // Adiciona o atacante à lista.
        yield return new WaitForSeconds(damageCooldown); // Espera o tempo definido.
        sourcesOnCooldown.Remove(source); // Remove o atacante da lista.
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
}
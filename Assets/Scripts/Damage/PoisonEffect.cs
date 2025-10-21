using UnityEngine;
using System.Collections;

// Este script precisa do HealthSystem no mesmo objeto para funcionar
[RequireComponent(typeof(HealthSystem))]
public class PoisonEffect : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("O Prefab da animação/partículas de veneno que aparecerá sobre o jogador.")]
    [SerializeField] private GameObject poisonVisualPrefab;
    private GameObject currentVisualInstance;

    private HealthSystem targetHealthSystem;
    private Coroutine poisonRoutine;

    void Awake()
    {
        targetHealthSystem = GetComponent<HealthSystem>();
    }

    /// <summary>
    /// Aplica o efeito de veneno, iniciando o dano por tempo.
    /// </summary>
    /// <param name="tickDamage">Dano causado por cada tick.</param>
    /// <param name="duration">Duração total do veneno.</param>
    /// <param name="tickInterval">Tempo entre cada tick de dano.</param>
    public void ApplyPoison(float tickDamage, float duration, float tickInterval = 1f)
    {
        // Se já estiver envenenado, reinicia a rotina (o novo veneno tem prioridade)
        if (poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine);
        }

        // 1. Inicia o visual
        StartVisuals();

        // 2. Inicia a rotina de dano
        poisonRoutine = StartCoroutine(PoisonDamageRoutine(tickDamage, duration, tickInterval));
    }

    private IEnumerator PoisonDamageRoutine(float tickDamage, float duration, float tickInterval)
    {
        float elapsedTime = 0f;

        // Loop principal para aplicar dano a cada tick
        while (elapsedTime < duration)
        {
            // O dano é aplicado. É essencial que o HealthSystem.TakeDamage trate 
            // este DamageType.Poison para que NÃO ative o cooldown de invulnerabilidade.
            targetHealthSystem.TakeDamage(new DamageInfo(tickDamage, DamageType.Poison));
            
            yield return new WaitForSeconds(tickInterval);
            elapsedTime += tickInterval;
        }

        // 3. Finaliza os efeitos
        StopVisuals();
        poisonRoutine = null;
    }
    
    private void StartVisuals()
    {
        if (poisonVisualPrefab != null && currentVisualInstance == null)
        {
            // Cria a animação/partícula como filho do Player
            currentVisualInstance = Instantiate(poisonVisualPrefab, transform);
            // Certifique-se de que o prefab visual é configurado para ser destruído quando o veneno acabar, 
            // ou deixe o StopVisuals() cuidar da destruição.
        }
    }

    private void StopVisuals()
    {
        if (currentVisualInstance != null)
        {
            // Destrói a animação visual quando o veneno acaba
            Destroy(currentVisualInstance);
            currentVisualInstance = null;
        }
    }
}
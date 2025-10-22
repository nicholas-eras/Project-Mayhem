using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Este script precisa do HealthSystem no mesmo objeto para funcionar
[RequireComponent(typeof(HealthSystem))]
public class PoisonEffect : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("O Prefab da animação/partículas de veneno que aparecerá sobre o jogador.")]
    [SerializeField] private GameObject poisonVisualPrefab;
    private GameObject currentVisualInstance;

    private HealthSystem targetHealthSystem;
    
    // VARIÁVEL ÚNICA para rastrear a Coroutine que está rodando.
    private Coroutine poisonRoutine;
    
    public bool IsPoisoned { get; private set; } = false;

    void Awake()
    {
        targetHealthSystem = GetComponent<HealthSystem>();
        if (targetHealthSystem == null)
        {
            Debug.LogError("PoisonEffect requer um HealthSystem no mesmo objeto.");
        }
    }

    /// <summary>
    /// Aplica o efeito de veneno, iniciando o dano por tempo.
    /// </summary>
    /// <param name="tickDamage">Dano causado por cada tick.</param>
    /// <param name="duration">Duração total do veneno.</param>
    /// <param name="tickInterval">Tempo entre cada tick de dano.</param>
    public void ApplyPoison(float tickDamage, float duration, float tickInterval = 1f)
    {
        if (targetHealthSystem == null) return;
        
        // Se já estiver ativo, para a rotina antiga (o novo veneno tem prioridade/reseta a duração)
        if (poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine);
        }

        IsPoisoned = true;

        // 1. Inicia o visual
        StartVisuals();

        // 2. Inicia a rotina de dano e ARMAZENA na variável única
        poisonRoutine = StartCoroutine(PoisonDamageRoutine(tickDamage, duration, tickInterval));
    }

    private IEnumerator PoisonDamageRoutine(float tickDamage, float duration, float tickInterval)
    {
        float elapsedTime = 0f;

        // Loop principal para aplicar dano a cada tick, baseado na duração total
        while (IsPoisoned && elapsedTime < duration)
        {
            // Aplica o dano. DamageType.Poison deve ser tratada como não bloqueável
            // e não ativadora de i-frames no HealthSystem.TakeDamage.
            targetHealthSystem.TakeDamage(new DamageInfo(tickDamage, DamageType.Poison));
            
            yield return new WaitForSeconds(tickInterval);
            elapsedTime += tickInterval;
        }

        // 3. Finaliza os efeitos se o loop terminar naturalmente (pela duração)
        poisonRoutine = null;
        StopVisuals();
        IsPoisoned = false;
    }
    
    private void StartVisuals()
    {
        if (poisonVisualPrefab != null && currentVisualInstance == null)
        {
            currentVisualInstance = Instantiate(poisonVisualPrefab, transform.position, Quaternion.identity, transform);
            
            // Ajusta a posição Z para que fique na frente (conforme sua lógica)
            Vector3 newLocalPosition = currentVisualInstance.transform.localPosition;
            newLocalPosition.z = -0.1f; 
            currentVisualInstance.transform.localPosition = newLocalPosition;
        }
    }

    private void StopVisuals()
    {
        if (currentVisualInstance != null)
        {
            // Destrói a animação visual quando o efeito acaba
            Destroy(currentVisualInstance);
            currentVisualInstance = null;
        }
    }

    /// <summary>
    /// CURA EXTERNA - Chamado pelo PlayerStatusEffects (fim da wave).
    /// </summary>
    public void Cure()
    {
        // Se a rotina de dano estiver rodando, pare-a
        if (poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine); 
            poisonRoutine = null;
        }
        
        // Finaliza o visual e o estado
        StopVisuals();
        IsPoisoned = false;
    }
}
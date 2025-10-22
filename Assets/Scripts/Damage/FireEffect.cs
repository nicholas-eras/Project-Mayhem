using UnityEngine;
using System.Collections;

[RequireComponent(typeof(HealthSystem))]
public class FireEffect : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("O Prefab da animação/partículas de fogo que aparecerá sobre o jogador.")]
    [SerializeField] private GameObject fireVisualPrefab;
    private GameObject currentVisualInstance;

    private HealthSystem targetHealthSystem;
    private Coroutine fireRoutine;
    
    public bool IsOnFire { get; private set; } = false;

    void Awake()
    {
        targetHealthSystem = GetComponent<HealthSystem>();
        if (targetHealthSystem == null)
        {
            Debug.LogError("FireEffect requer um HealthSystem no mesmo objeto.");
        }
    }

    public void ApplyFire(float tickDamage, float duration, float tickInterval = 1f)
    {
        if (targetHealthSystem == null) return;
        
        // Se já estiver pegando fogo, NÃO reinicia (mantém o fogo original)
        if (fireRoutine != null)
        {
            Debug.Log($"[FireEffect] Já está pegando fogo. Ignorando nova aplicação.", this);
            return;
        }

        IsOnFire = true;
        StartVisuals();
        fireRoutine = StartCoroutine(FireDamageRoutine(tickDamage, duration, tickInterval));
        
        Debug.Log($"[FireEffect] Fogo aplicado! Dano: {tickDamage}, Duração: {duration}s, Intervalo: {tickInterval}s", this);
    }

    private IEnumerator FireDamageRoutine(float tickDamage, float duration, float tickInterval)
    {
        float elapsedTime = 0f;
        int tickCount = 0;

        while (IsOnFire && elapsedTime < duration)
        {
            // Aplica o dano como Fire (passa por i-frames mas não os ativa)
            DamageInfo damageInfo = new DamageInfo(tickDamage, DamageType.Fire);
            targetHealthSystem.TakeDamage(damageInfo, null); // null = não ativa cooldown
            
            tickCount++;
            Debug.Log($"[FireEffect] TICK #{tickCount}: {tickDamage} dano aplicado. Tempo decorrido: {elapsedTime:F2}s/{duration}s", this);
            
            yield return new WaitForSeconds(tickInterval);
            elapsedTime += tickInterval;
        }

        // Finaliza o efeito
        Debug.Log($"[FireEffect] Fogo extinto após {tickCount} ticks ({elapsedTime:F2}s).", this);
        fireRoutine = null;
        StopVisuals();
        IsOnFire = false;
    }
    
    private void StartVisuals()
    {
        if (fireVisualPrefab != null && currentVisualInstance == null)
        {
            currentVisualInstance = Instantiate(fireVisualPrefab, transform.position, Quaternion.identity, transform);
            
            Vector3 newLocalPosition = currentVisualInstance.transform.localPosition;
            newLocalPosition.z = -0.1f; 
            currentVisualInstance.transform.localPosition = newLocalPosition;
        }
    }

    private void StopVisuals()
    {
        if (currentVisualInstance != null)
        {
            Destroy(currentVisualInstance);
            currentVisualInstance = null;
        }
    }

    public void Cure()
    {
        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine); 
            fireRoutine = null;
        }
        
        StopVisuals();
        IsOnFire = false;
        
        Debug.Log($"[FireEffect] Fogo curado externamente.", this);
    }
}
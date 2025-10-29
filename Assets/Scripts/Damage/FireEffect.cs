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
    if (fireRoutine != null) return;

    // Proteção extra — garante que o script e o objeto estejam ativos
    if (!isActiveAndEnabled || gameObject == null || !gameObject.activeInHierarchy)
        return;

    IsOnFire = true;
    StartVisuals();

    // Proteção final: evita crash se o objeto for desativado no mesmo frame
    if (isActiveAndEnabled && gameObject.activeInHierarchy)
        fireRoutine = StartCoroutine(FireDamageRoutine(tickDamage, duration, tickInterval));
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

            yield return new WaitForSeconds(tickInterval);
            elapsedTime += tickInterval;
        }

        // Finaliza o efeito
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
    }
}
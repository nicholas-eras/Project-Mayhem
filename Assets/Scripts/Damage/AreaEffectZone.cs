using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AreaEffectType
{
    Poison,
    Fire,
    Slow,
    DamageZone,
    Darkness,
    Confusion,
    preventShooting
}

[RequireComponent(typeof(Collider2D))]
public class AreaEffectZone : MonoBehaviour
{
    [Header("Configuração de Duração")]
    [Tooltip("Duração total da área de efeito antes de se autodestruir.")]
    [SerializeField] private float cloudDuration = 5f;

    [Header("Efeito Aplicado ao Player (Status DOT)")]
    [SerializeField] private AreaEffectType effectType;
    [Tooltip("Valor de penalidade (tickDamage para Poison/Fire, fator para Slow).")]
    [SerializeField] private float penaltyValue = 2f; 
    [Tooltip("Duração do Status aplicado (ex: 3s de fogo).")]
    [SerializeField] private float effectDuration = 3f;
    [Tooltip("Intervalo de tick para o Status aplicado (Fire/Poison).")]
    [SerializeField] private float tickInterval = 1f;

    [Header("Dano Extra da Zona (Penalidade por ficar dentro)")]
    [Tooltip("Dano EXTRA aplicado enquanto permanece na zona. Soma com o status de Fire/Poison!")]
    [SerializeField] private float zoneTickDamage = 0f; 
    [Tooltip("Intervalo de tempo (segundos) entre os ticks de dano EXTRA da zona.")]
    [SerializeField] private float zoneTickInterval = 0.5f;
    
    // Lista de alvos dentro da zona
    private List<GameObject> targetsInZone = new List<GameObject>();
    // Dicionário para rastrear o tempo desde o último tick
    private Dictionary<GameObject, float> damageTimers = new Dictionary<GameObject, float>();

    void Start()
    {
        if (cloudDuration > 0f)
        {
            Destroy(gameObject, cloudDuration);
        }
        
        Vector3 currentPos = transform.position;
        currentPos.z = -0.1f; 
        transform.position = currentPos;
    }

    void Update()
    {
        // Processa dano extra da zona (se configurado)
        if (zoneTickDamage <= 0f) return;

        for (int i = targetsInZone.Count - 1; i >= 0; i--)
        {
            GameObject target = targetsInZone[i];
            
            if (target == null)
            {
                targetsInZone.RemoveAt(i);
                damageTimers.Remove(target);
                continue;
            }

            if (damageTimers.ContainsKey(target))
            {
                // ✅ Ignora se o alvo foi desativado (morto)
                if (target == null || !target.activeInHierarchy)
                {
                    damageTimers.Remove(target);
                    return;
                }
    
                damageTimers[target] += Time.deltaTime;

                while (damageTimers[target] >= zoneTickInterval)
                {
                    HealthSystem targetHealth = target.GetComponent<HealthSystem>();
                    if (targetHealth != null)
                    {
                        // Usa o tipo correspondente para passar pelos i-frames
                        DamageType damageType = DamageType.Fire;
                        if (effectType == AreaEffectType.Poison) damageType = DamageType.Poison;
                        
                        DamageInfo damagePacket = new DamageInfo(zoneTickDamage, damageType);
                        targetHealth.TakeDamage(damagePacket, null); // null = não ativa i-frames
                    }
                    if (!damageTimers.ContainsKey(target))
                    {
                        return;
                    }
                    damageTimers[target] -= zoneTickInterval;
                }
            }
        }
    }

    private void OnDestroy()
    {
        targetsInZone.Clear();
        damageTimers.Clear();
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject target = other.gameObject;
            if (target == null || !target.activeInHierarchy)
            {
                // Apenas remova da sua lista/dicionário de alvos
                // Ex: if (targetsInside.ContainsKey(target)) targetsInside.Remove(target);
                return;
            }
    
            // 2. Inicia timer de dano extra (só enquanto estiver dentro)
            if (zoneTickDamage > 0f && !targetsInZone.Contains(target))
            {
                targetsInZone.Add(target);
                damageTimers[target] = 0f;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject target = other.gameObject;
            ApplyEffectToTarget(target);

            // Para o dano extra quando sair (mas o status continua!)
            if (targetsInZone.Contains(target))
            {
                targetsInZone.Remove(target);
                damageTimers.Remove(target);
            }
        }
    }
    
    private void ApplyEffectToTarget(GameObject target)
    {
        if (target == null || !target.activeInHierarchy)
            return;

        switch (effectType)
        {
            case AreaEffectType.Poison:
                var poison = target.GetComponent<PoisonEffect>();
                if (poison != null && poison.isActiveAndEnabled)
                {
                    poison.ApplyPoison(penaltyValue, effectDuration, tickInterval);
                }
                break;

            case AreaEffectType.Fire:
                var fire = target.GetComponent<FireEffect>();
                if (fire != null && fire.isActiveAndEnabled && target.activeInHierarchy)
                {
                    fire.ApplyFire(penaltyValue, effectDuration, tickInterval);
                }
                break;
        }
    }

    public void Setup(float duration)
    {
        cloudDuration = duration;
    }
    
    public AreaEffectType GetEffectType() 
    {
        return effectType;
    }
}
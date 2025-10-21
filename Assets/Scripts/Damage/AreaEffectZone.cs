using UnityEngine;

// Definição dos Tipos de Efeito (Recomendado para ser global, mas incluído aqui)
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

// Requer um collider 2D (marcado como Is Trigger) para funcionar
[RequireComponent(typeof(Collider2D))]
public class AreaEffectZone : MonoBehaviour
{
    [Header("Configuração de Duração")]
    [Tooltip("Duração total da área de efeito antes de se autodestruir.")]
    [SerializeField] private float cloudDuration = 5f;

    [Header("Efeito Aplicado ao Player")]
    [SerializeField] private AreaEffectType effectType;
    [SerializeField] private float penaltyValue = 2f; 
    [SerializeField] private float effectDuration = 3f;

    private float tickInterval = 1f; // Frequência do dano por segundo (Padrão para Poison)
    
    void Start()
    {
        // Garante que a nuvem desapareça
        if (cloudDuration > 0f)
        {
            // Garante que a nuvem desapareça (usa o valor definido por Setup ou o valor padrão)
            Destroy(gameObject, cloudDuration);
        }
        Vector3 currentPos = transform.position;
        currentPos.z = -0.1f; 
        transform.position = currentPos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyEffectToTarget(other.gameObject);
        }
    }

    private void ApplyEffectToTarget(GameObject target)
    {
        // O código se concentra em chamar o efeito correto no alvo
        switch (effectType)
        {
            case AreaEffectType.Poison:
                PoisonEffect poison = target.GetComponent<PoisonEffect>();
                if (poison != null)
                {
                    // penaltyValue é o tickDamage aqui
                    poison.ApplyPoison(penaltyValue, effectDuration, tickInterval);
                }
                break;
            
            // Adicione aqui a lógica para outros efeitos no futuro
            // case AreaEffectType.Fire: ...
        }
    }

    // Método de configuração opcional, se o inimigo quiser sobrescrever os valores
    public void Setup(float duration)
    {
        cloudDuration = duration;
    }
}
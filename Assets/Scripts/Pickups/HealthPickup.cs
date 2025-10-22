using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))] // Garante que tenha um Collider para a colisão
public class HealthPickup : MonoBehaviour
{
    [Header("Configuração de Cura")]
    [Tooltip("A quantidade de vida a ser restaurada (0 para cura total).")]
    [SerializeField] private float healAmount = 10f;

    [Tooltip("Se True, o item também curará todos os efeitos de status negativos.")]
    [SerializeField] private bool curesAllStatusEffects = true;

    [Header("Feedback")]
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private string pickupSoundName = "HealPickup";

    /// <summary>
    /// Chamado pelo Spawner para definir o tempo de vida do item.
    /// </summary>
    public void Setup(float lifetime)
    {
        // Se a duração for maior que zero, inicia a autodestruição.
        if (lifetime > 0)
        {
            // Se o item não for pego, ele será destruído após 'lifetime' segundos.
            Destroy(gameObject, lifetime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Checa se o objeto que colidiu é o Player
        if (other.CompareTag("Player"))
        {
            // Tenta obter os componentes necessários
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            PlayerStatusEffects statusEffects = other.GetComponent<PlayerStatusEffects>();

            bool wasCollected = false;

            // 2. Aplica a Cura
            if (playerHealth != null)
            {
                if (healAmount == 0)
                {
                    // Cura totalmente se o valor for 0
                    playerHealth.HealFull();
                }
                else
                {
                    // Cura a quantidade especificada (você precisará de um método Heal(float) no seu HS)
                    playerHealth.Heal(healAmount);
                }
                wasCollected = true;
            }

            // 3. Cura Status Negativos
            if (curesAllStatusEffects && statusEffects != null)
            {
                statusEffects.CureAllStatusEffects();
                wasCollected = true;
            }

            // 4. Se o jogador realmente o coletou (e teve algum efeito)
            if (wasCollected)
            {
                GiveFeedbackAndDestroy(other.transform); // <--- MUDANÇA AQUI
            }
        }
    }

    private void GiveFeedbackAndDestroy(Transform targetTransform) 
    {
        // Toca som (assumindo AudioManager.Instance está configurado)
        if (!string.IsNullOrEmpty(pickupSoundName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(pickupSoundName);
        }

        if (pickupEffectPrefab != null)
        {
            // NOVO: Instancia na POSIÇÃO DO ALVO (Player)
            // Usamos targetTransform.position em vez de transform.position
            GameObject fxInstance = Instantiate(
                        pickupEffectPrefab,
                        targetTransform.position, // Posição global inicial (opcional, mas bom ter)
                        Quaternion.identity,
                        targetTransform             // <--- ESTE É O PARENT (O Player)
                    );
            Vector3 localPos = fxInstance.transform.localPosition;
            localPos.z = -0.1f; // Use um valor negativo pequeno, ex: -0.1f ou -0.01f
            fxInstance.transform.localPosition = localPos;
            // Opcional: Se o seu efeito de partícula tem um script de autodestruição, 
            // ele vai funcionar. Caso contrário, você pode adicioná-lo ou usar:
            Destroy(fxInstance, 1.0f); // Destrói o efeito após 1 segundos
        }

        // Destrói o item de cura
        Destroy(gameObject);
    }
}

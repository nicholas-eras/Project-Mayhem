using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Componentes da UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    private HealthSystem targetHealthSystem;

    private void Awake()
    {
        if (healthSlider == null)
        {
            healthSlider = GetComponent<Slider>();
        }
    }

    public void Setup(HealthSystem target)
    {
        targetHealthSystem = target;

        if (healthSlider != null)
        {
            // MODIFICADO: Usa a propriedade 'MaxHealth'
            healthSlider.maxValue = target.MaxHealth;
        }

        // A inscrição no evento está correta
        // (O 'UnityAction' funciona igual ao 'Action' para isso)
        target.OnHealthChanged += UpdateUI;

        // MODIFICADO: Usa as propriedades 'CurrentHealth' e 'MaxHealth'
        UpdateUI(target.CurrentHealth, target.MaxHealth);
    }

    // Esta função já é compatível e não precisa de mudança
    private void UpdateUI(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)} / {maxHealth}";
        }
    }

    private void OnDestroy()
    {
        if (targetHealthSystem != null)
        {
            targetHealthSystem.OnHealthChanged -= UpdateUI;
        }
    }
}
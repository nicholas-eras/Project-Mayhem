using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour
{
    private Slider healthSlider;

    void Awake()
    {
        healthSlider = GetComponent<Slider>(); // Isso deve pegar o Slider anexado a este GameObject
        if (healthSlider == null)
        {
            Debug.LogError("WorldHealthBar: Slider component not found on this GameObject!", this);
        }
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // Necessário para UnityAction e UnityEvent

[RequireComponent(typeof(RectTransform))]
public class FollowTargetUI : MonoBehaviour
{
    public Transform target;
    public Vector3 offset; 

    private Slider healthSlider;
    private HealthSystem targetHealth;
    private Camera mainCamera;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        healthSlider = GetComponentInChildren<Slider>();
        mainCamera = Camera.main;
    }

    public void Setup(HealthSystem healthSystem)
    {
        this.targetHealth = healthSystem;
        this.target = healthSystem.transform;

        if (healthSlider == null || targetHealth == null)
        {
            Debug.LogError("FollowTargetUI: Slider ou HealthSystem não encontrado!");
            Destroy(gameObject);
            return;
        }

        healthSlider.maxValue = targetHealth.MaxHealth;
        healthSlider.value = targetHealth.CurrentHealth;

        // --- CORREÇÃO MISTA ---
        
        // OnHealthChanged é um 'UnityAction' (delegate), então usamos +=
        // e o método UpdateHealthBar DEVE receber (float, float)
        targetHealth.OnHealthChanged += UpdateHealthBar; 
        
        // OnDeath é um 'UnityEvent', então usamos .AddListener()
        // e o método HideOnDeath NÃO deve ter parâmetros
        targetHealth.OnDeath.AddListener(HideOnDeath);
    }

    void LateUpdate()
    {
        if (target == null || mainCamera == null)
        {
            if(target == null) 
                Destroy(gameObject);
            return;
        }
        
        Vector2 screenPos = mainCamera.WorldToScreenPoint(target.position + offset);
        rectTransform.position = screenPos;

        float dot = Vector3.Dot(mainCamera.transform.forward, (target.position - mainCamera.transform.position).normalized);
        healthSlider.gameObject.SetActive(dot > 0);
    }

    // --- CORREÇÃO DO MÉTODO ---
    // Este método DEVE receber (float current, float max) para
    // funcionar com o 'UnityAction<float, float>'
    private void UpdateHealthBar(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.value = current;
        }
    }

    // Este método NÃO deve ter parâmetros para
    // funcionar com o 'UnityEvent'
    private void HideOnDeath()
    {  
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (targetHealth != null)
        {
            // --- LIMPEZA MISTA ---
            targetHealth.OnHealthChanged -= UpdateHealthBar; 
            targetHealth.OnDeath.RemoveListener(HideOnDeath);
        }
    }
}
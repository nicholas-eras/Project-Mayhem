using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BossHealthLinker : MonoBehaviour
{
    [Header("Configuração de Vida Compartilhada")]
    [Tooltip("Vida total compartilhada pelo Greater Boss.")]
    [SerializeField] public float initialTotalHealth = 1000f;
    
    private float currentTotalHealth;
    public float CurrentHealth => currentTotalHealth;

    public UnityAction<float, float> OnBossHealthChanged;
    public UnityEvent OnBossDefeated;

    void Awake()
    {
        currentTotalHealth = initialTotalHealth;
    }
    
    void Start()
    {
        // NOVO: Envia o valor inicial de vida para que a UI configure o Slider.
        OnBossHealthChanged?.Invoke(currentTotalHealth, initialTotalHealth);
    }
    
    /// <summary>
    /// Recebe dano de qualquer parte do Boss e distribui a morte.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (currentTotalHealth <= 0) return;

        currentTotalHealth -= amount;
        currentTotalHealth = Mathf.Max(0, currentTotalHealth);
        // Notifica a UI central
        OnBossHealthChanged?.Invoke(currentTotalHealth, initialTotalHealth);

        if (currentTotalHealth <= 0)
        {
            DefeatBoss();
        }
    }

    private void DefeatBoss()
    {
        OnBossDefeated?.Invoke();
        
        // Aqui você pode adicionar a lógica para destruir os WallBossControllers
        WallBossController[] walls = FindObjectsOfType<WallBossController>();
        foreach (WallBossController wall in walls)
        {
            if (wall != null)
            {
                wall.DefeatWall(); // Chama o método de destruição da parede
            }
        }
    }
}

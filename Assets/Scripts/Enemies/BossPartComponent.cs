using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Script base para Bocas e Olhos
public class BossPartComponent : MonoBehaviour
{
    [Header("Sprite / Visuals")]
    [SerializeField] private SpriteRenderer partRenderer;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite attackSprite;
        
    [Header("Tempo de Ataque")]
    [Tooltip("O tempo que o sprite de ataque deve ficar visível.")]
    [SerializeField] public float attackSpriteDuration = 0.5f;
    private Coroutine attackCycleRoutine; 

    public UnityEvent OnAttackStart;
    public UnityEvent OnAttackEnd;

    private EnemyShooter enemyShooter;
    private BossEyeController eyeController;
    private float nextFireTime; // Timer local APENAS DA BOCA
    private Coroutine attackRoutine; // Para o visual da Boca

    void Start()
    {
        // 1. OBTENÇÃO DE COMPONENTES
        enemyShooter = GetComponent<EnemyShooter>();
        eyeController = GetComponent<BossEyeController>(); // Verifica se é um Olho
        
        // Garante que o componente está no WallBossController
        if (GetComponentInParent<WallBossController>() == null)
        {
            Debug.LogError("BossPartComponent deve ser filho de um WallBossController!");
        }

        if (partRenderer != null)
        {
            partRenderer.sprite = defaultSprite;
        }
        
        // 3. INICIALIZAÇÃO DO TIMER DA BOCA (Se NÃO for um Olho)
        if (eyeController == null && enemyShooter != null) 
        {
            // Inicializa o timer da Boca com um atraso para escalonar
            nextFireTime = Time.time + enemyShooter.fireRateInterval + Random.Range(0f, 1f); 
        }
    }
    
    // =================================================================
    // MÉTODOS VISUAIS
    // =================================================================

    /// <summary>
    /// Troca para o sprite de ataque.
    /// </summary>
    public void SetAttackVisual()
    {
        if (partRenderer != null && attackSprite != null)
        {
            partRenderer.sprite = attackSprite;
        }
    }

    /// <summary>
    /// Troca para o sprite padrão/idle.
    /// </summary>
    public void SetDefaultVisual()
    {
        if (partRenderer != null)
        {
            partRenderer.sprite = defaultSprite;
        }
    }

    /// <summary>
    /// Ativa o visual de ataque e dispara o evento AttackStart (Usado pela Boca).
    /// </summary>
    public void BeginAttack()
    {
        if (eyeController != null) return; // Segurança
        if (attackRoutine != null) return;
                
        attackRoutine = StartCoroutine(AttackVisualRoutine());
    }

    // O NOVO LOOP DE ATAQUE INDIVIDUAL
    void Update()
    {
        if (eyeController != null) return; // Esta parte do Update é apenas para Bocas

        if (enemyShooter != null && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + enemyShooter.fireRateInterval; // Reinicia o timer da Boca

            if (enemyShooter.playerTarget == null) return;
            float distanceToPlayer = Vector2.Distance(transform.position, enemyShooter.playerTarget.position);

            if (distanceToPlayer <= enemyShooter.shootingRange)
            {
                BeginAttack(); // Dispara o ataque da Boca
            }
        }
    }


    private IEnumerator AttackVisualRoutine()
    {
        OnAttackStart?.Invoke();
        SetAttackVisual();
        
        // Dispara o tiro no meio do visual
        if (enemyShooter != null) enemyShooter.Shoot(); 

        yield return new WaitForSeconds(attackSpriteDuration);
        
        SetDefaultVisual();
        OnAttackEnd?.Invoke();
        attackRoutine = null;
    }

    // Adiciona um método público para ser chamado como Coroutine (Usado pelo BossEyeController)
    public IEnumerator AttackVisualRoutineProxy()
    {        
        OnAttackStart?.Invoke(); // Dispara o evento de ataque ANTES do visual
        
        SetAttackVisual();

        // 2. Espera a duração do visual
        yield return new WaitForSeconds(attackSpriteDuration);

        SetDefaultVisual(); // Usa o novo método

        OnAttackEnd?.Invoke();
    }
}
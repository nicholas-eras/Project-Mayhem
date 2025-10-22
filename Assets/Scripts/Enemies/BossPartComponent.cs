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
    [SerializeField] private float attackSpriteDuration = 0.5f;

    public UnityEvent OnAttackStart;
    public UnityEvent OnAttackEnd;

    private Coroutine attackRoutine;
    
    void Start()
    {
        if (partRenderer != null)
        {
            partRenderer.sprite = defaultSprite;
        }
        
        // Garante que o componente está no WallBossController
        if (GetComponentInParent<WallBossController>() == null)
        {
            Debug.LogError("BossPartComponent deve ser filho de um WallBossController!");
        }
    }

    /// <summary>
    /// Ativa o visual de ataque e dispara o evento AttackStart.
    /// </summary>
    public void BeginAttack()
    {
        if (attackRoutine != null) return;
        attackRoutine = StartCoroutine(AttackVisualRoutine());
        OnAttackStart?.Invoke();
    }

    private IEnumerator AttackVisualRoutine()
    {
        // 1. Troca para o sprite de ataque
        if (partRenderer != null && attackSprite != null)
        {
            partRenderer.sprite = attackSprite;
        }

        // 2. Espera a duração do visual
        yield return new WaitForSeconds(attackSpriteDuration);

        // 3. Troca de volta e notifica
        if (partRenderer != null)
        {
            partRenderer.sprite = defaultSprite;
        }
        OnAttackEnd?.Invoke();
        attackRoutine = null;
    }
}


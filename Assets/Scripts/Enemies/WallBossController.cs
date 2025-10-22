using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WallBossController : MonoBehaviour
{
    [Header("Configuração da Parede")]
    [Tooltip("O lado que esta parede representa.")]
    [SerializeField] private string wallName = "Left Wall";
    [Tooltip("Intervalo mínimo de tempo entre os ataques de diferentes componentes.")]
    [SerializeField] private float componentAttackCooldown = 1.0f;

    // Lista de componentes (bocas/olhos) que podem atacar
    private BossPartComponent[] attackComponents;

    void Start()
    {
        // Encontra todos os BossPartComponent (bocas/olhos) filhos
        attackComponents = GetComponentsInChildren<BossPartComponent>();
        
        // Inicia a rotina de ataques
        StartCoroutine(AttackCycleRoutine());
    }
    
    /// <summary>
    /// Gerencia a cadência de ataques entre as bocas/olhos.
    /// </summary>
    private IEnumerator AttackCycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(componentAttackCooldown);

            // 1. Seleciona um componente aleatório
            if (attackComponents.Length == 0) continue;
            BossPartComponent targetPart = attackComponents[Random.Range(0, attackComponents.Length)];
            
            // 2. Tenta iniciar o ataque.
            // O componente de ataque (que terá o EnemyShooter ou BossLaserAttack)
            // deve escutar o OnAttackStart deste componente base para disparar o projétil/laser.
            targetPart.BeginAttack();
        }
    }

    /// <summary>
    /// Chamado pelo BossHealthLinker quando o Boss morre.
    /// </summary>
    public void DefeatWall()
    {
        // Parar todos os ataques
        StopAllCoroutines();
        
        // Adicionar efeito visual de destruição da parede
        Debug.Log($"{wallName} foi destruída!");
        
        // Destruir o objeto
        Destroy(gameObject);
    }
}


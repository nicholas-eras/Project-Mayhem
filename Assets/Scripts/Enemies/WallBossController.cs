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
    }        

    /// <summary>
    /// Chamado pelo BossHealthLinker quando o Boss morre.
    /// </summary>
    public void DefeatWall()
    {
        // Parar todos os ataques
        StopAllCoroutines();

        // Adicionar efeito visual de destruição da parede

        // Destruir o objeto
        Destroy(gameObject);
    }
}


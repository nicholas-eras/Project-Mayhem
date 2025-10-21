using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Para usar o OrderBy/Min/Max

public class BotController : MonoBehaviour
{
    [Header("Stats de Movimento")]
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float currentMoveSpeed; // Herdado do PlayerController

    [Header("Comportamento de Fuga")]
    [Tooltip("Raio para procurar e evitar inimigos.")]
    [SerializeField] private float threatDetectionRadius = 8f;
    [Tooltip("Camada onde os inimigos estão.")]
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector3 fleeDirection = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Assumindo que a velocidade é inicializada por algum Manager ou é fixa
        currentMoveSpeed = baseMoveSpeed; 
    }

    void Update()
    {
        // A lógica de fuga é executada no Update para ser mais responsiva
        FleeFromThreats();
    }

    void FixedUpdate()
    {
        // Aplica o movimento com base na direção calculada pela fuga
        rb.MovePosition(rb.position + moveInput.normalized * currentMoveSpeed * Time.fixedDeltaTime);
    }

    private void FleeFromThreats()
    {
        // 1. Detectar ameaças próximas
        Collider2D[] threats = Physics2D.OverlapCircleAll(transform.position, threatDetectionRadius, enemyLayer);

        if (threats.Length > 0)
        {
            Vector3 totalAvoidanceVector = Vector3.zero;
            
            // 2. Calcular o vetor de repulsão total
            foreach (var threat in threats)
            {
                // Vetor do inimigo para o bot
                Vector3 awayFromThreat = transform.position - threat.transform.position;
                
                // Normaliza e adiciona ao vetor total. 
                // A força de repulsão é inversamente proporcional à distância (para evitar os mais próximos mais fortemente)
                totalAvoidanceVector += awayFromThreat.normalized / awayFromThreat.magnitude;
            }

            // 3. Normaliza o vetor de fuga para obter o input (de -1 a 1)
            moveInput = totalAvoidanceVector.normalized;
        }
        else
        {
            // Se não houver ameaças, o Bot pode ficar parado ou seguir o Player principal (opcional)
            moveInput = Vector2.zero;
        }
    }
    
    // O BotController deve ter as funções de upgrade do PlayerController se você quiser que ele receba upgrades
    // Ex: public void IncreaseSpeedMultiplier(float percentage) { currentMoveSpeed *= (1f + percentage); }
}
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class BotController : MonoBehaviour
{
    [Header("Stats de Movimento")]
    [SerializeField] private float baseMoveSpeed = 5f;
    private float currentMoveSpeed;

    [Header("Comportamento IA")]
    [SerializeField] private float threatDetectionRadius = 8f;
    [SerializeField] private float attackRange = 4f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float centerSeekingWeight = 0.4f;
    [SerializeField] private float wallDetectionDistance = 2.5f;
    [SerializeField] private float botBoundsPadding = 0.5f;

    [Header("Comportamentos")]
    [Tooltip("Distância mínima para manter dos inimigos")]
    [SerializeField] private float safeDistanceFromEnemies = 3f;
    [Tooltip("Frequência de mudança de direção quando patrulhando")]
    [SerializeField] private float patrolDirectionChangeTime = 2f;
    [Tooltip("Chance de mudar para comportamento agressivo quando com vida alta")]
    [SerializeField] private float aggressiveChance = 0.3f;

    // --- Estados da IA ---
    private enum BotState { Patrulha, Fugindo, Atacando, BuscandoCentro }
    private BotState currentState = BotState.Patrulha;

    // --- Variáveis Internas ---
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool boundsInitialized = false;
    private Transform bottomLeftMarker;
    private Transform topRightMarker;
    private Vector2 mapCenter;
    private HealthSystem healthSystem;

    // --- Sistema de Direções ---
    private Vector2[] candidateDirections;
    private float[] threatScores = new float[8];
    private float[] interestScores = new float[8];

    // --- Timers e Memória ---
    private float patrolTimer = 0f;
    private Vector2 patrolDirection;
    private Transform currentTarget;
    private float lastTargetUpdateTime = 0f;
    private float targetUpdateCooldown = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        healthSystem = GetComponent<HealthSystem>();
        currentMoveSpeed = baseMoveSpeed;
    }

    void Start()
    {
        InitializeBounds();
        InitializeCandidateDirections();
        ChooseNewPatrolDirection();
    }

    void InitializeCandidateDirections()
    {
        candidateDirections = new Vector2[8];
        candidateDirections[0] = new Vector2(0, 1).normalized;
        candidateDirections[1] = new Vector2(1, 1).normalized;
        candidateDirections[2] = new Vector2(1, 0).normalized;
        candidateDirections[3] = new Vector2(1, -1).normalized;
        candidateDirections[4] = new Vector2(0, -1).normalized;
        candidateDirections[5] = new Vector2(-1, -1).normalized;
        candidateDirections[6] = new Vector2(-1, 0).normalized;
        candidateDirections[7] = new Vector2(-1, 1).normalized;
    }

    void InitializeBounds()
    {
        Transform mapBoundsParent = null;
        GameObject boundsGO = GameObject.Find("--- MAP BOUNDS ---");
        if (boundsGO != null) mapBoundsParent = boundsGO.transform;
        
        if (mapBoundsParent == null)
        {
            Debug.LogWarning($"[BotController] '--- MAP BOUNDS ---' não encontrado.", this);
            boundsInitialized = false;
            return;
        }

        bottomLeftMarker = mapBoundsParent.Find("BottomLeft_Marker");
        topRightMarker = mapBoundsParent.Find("TopRight_Marker");

        if (bottomLeftMarker == null || topRightMarker == null)
        {
            Debug.LogError($"[BotController] Marcadores de Limite não encontrados.", mapBoundsParent);
            boundsInitialized = false;
        }
        else
        {
            boundsInitialized = true;
            mapCenter = (bottomLeftMarker.position + topRightMarker.position) / 2f;
        }
    }

    void Update()
    {
        if (!boundsInitialized) return;

        UpdateState();
        CalculateBestMoveDirection();
    }

    void FixedUpdate()
    {
        if (!boundsInitialized) return;

        // Movimento simples sem rotação
        if (moveInput != Vector2.zero)
        {
            rb.MovePosition(rb.position + moveInput.normalized * currentMoveSpeed * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Máquina de estados da IA - decide o comportamento atual
    /// </summary>
    private void UpdateState()
    {
        Collider2D[] threats = Physics2D.OverlapCircleAll(transform.position, threatDetectionRadius, enemyLayer);
        Transform closestThreat = GetClosestThreat(threats);
        
        // Atualiza target periodicamente
        if (Time.time - lastTargetUpdateTime > targetUpdateCooldown)
        {
            currentTarget = closestThreat;
            lastTargetUpdateTime = Time.time;
        }

        // --- TRANSIÇÕES DE ESTADO ---
        
        // SEMPRE fugir se a vida estiver baixa e houver ameaças próximas
        float healthPercent = healthSystem.CurrentHealth / healthSystem.MaxHealth;
        if (healthPercent < 0.3f && closestThreat != null)
        {
            currentState = BotState.Fugindo;
            return;
        }

        // Se há ameaças muito próximas, FUGIR
        if (closestThreat != null && Vector2.Distance(transform.position, closestThreat.position) < safeDistanceFromEnemies)
        {
            currentState = BotState.Fugindo;
            return;
        }

        // Se há ameaças no range de ataque e vida ok, ATACAR (com certa probabilidade)
        if (closestThreat != null && Vector2.Distance(transform.position, closestThreat.position) <= attackRange)
        {
            if (healthPercent > 0.6f && Random.value < aggressiveChance)
            {
                currentState = BotState.Atacando;
                return;
            }
        }

        // Se está longe do centro, buscar centro
        Vector2 currentPosition = transform.position;
        float distanceToCenter = Vector2.Distance(currentPosition, mapCenter);
        if (distanceToCenter > threatDetectionRadius * 0.7f && threats.Length == 0)
        {
            currentState = BotState.BuscandoCentro;
            return;
        }

        // Comportamento padrão: PATRULHA
        currentState = BotState.Patrulha;
    }

    /// <summary>
    /// Calcula a direção de movimento baseada no estado atual
    /// </summary>
    private void CalculateBestMoveDirection()
    {
        Vector2 currentPosition = transform.position;
        
        // --- COMPORTAMENTOS ESPECÍFICOS POR ESTADO ---
        switch (currentState)
        {
            case BotState.Fugindo:
                moveInput = CalculateFleeBehavior(currentPosition);
                break;
                
            case BotState.Atacando:
                moveInput = CalculateAttackBehavior(currentPosition);
                break;
                
            case BotState.BuscandoCentro:
                moveInput = CalculateCenterSeekBehavior(currentPosition);
                break;
                
            case BotState.Patrulha:
            default:
                moveInput = CalculatePatrolBehavior(currentPosition);
                break;
        }

        // Aplica sistema de vetores para evitar paredes e ajustar direção
        moveInput = ApplyVectorFieldSystem(currentPosition, moveInput);
        
        // GARANTE que nunca fique completamente parado
        if (moveInput.magnitude < 0.1f)
        {
            moveInput = GetDefaultMovement();
        }
    }

    /// <summary>
    /// Movimento padrão quando não há decisão clara
    /// </summary>
    private Vector2 GetDefaultMovement()
    {
        // Movimento circular suave ao redor do centro
        Vector2 toCenter = mapCenter - (Vector2)transform.position;
        Vector2 perpendicular = new Vector2(-toCenter.y, toCenter.x).normalized;
        return perpendicular;
    }

    /// <summary>
    /// Comportamento: Fugir de ameaças
    /// </summary>
    private Vector2 CalculateFleeBehavior(Vector2 currentPosition)
    {
        if (currentTarget == null) return GetDefaultMovement();

        Vector2 fleeDirection = (currentPosition - (Vector2)currentTarget.position).normalized;
        
        // Adiciona um pouco de movimento lateral para ser imprevisível
        float lateralMovement = Mathf.Sin(Time.time * 3f) * 0.3f;
        Vector2 lateral = new Vector2(-fleeDirection.y, fleeDirection.x) * lateralMovement;
        
        return (fleeDirection + lateral).normalized;
    }

    /// <summary>
    /// Comportamento: Atacar/mirar em inimigos
    /// </summary>
    private Vector2 CalculateAttackBehavior(Vector2 currentPosition)
    {
        if (currentTarget == null) return GetDefaultMovement();

        Vector2 toTarget = (Vector2)currentTarget.position - currentPosition;
        float distance = toTarget.magnitude;

        // Movimento orbital - circula o inimigo
        if (distance < attackRange * 0.8f)
        {
            // Move lateralmente ao redor do inimigo
            Vector2 orbital = new Vector2(-toTarget.y, toTarget.x).normalized;
            return (orbital + toTarget.normalized * 0.3f).normalized;
        }
        else
        {
            // Avança mirando um pouco ao lado para ser imprevisível
            float angleOffset = Mathf.Sin(Time.time * 2f) * 0.5f;
            Vector2 perpendicular = new Vector2(-toTarget.y, toTarget.x).normalized * angleOffset;
            return (toTarget.normalized + perpendicular).normalized;
        }
    }

    /// <summary>
    /// Comportamento: Buscar o centro do mapa
    /// </summary>
    private Vector2 CalculateCenterSeekBehavior(Vector2 currentPosition)
    {
        Vector2 toCenter = mapCenter - currentPosition;
        
        // Quando perto do centro, faz movimento circular
        if (toCenter.magnitude < 2f)
        {
            Vector2 circular = new Vector2(-toCenter.y, toCenter.x).normalized;
            return (circular + toCenter.normalized * 0.2f).normalized;
        }
        
        return toCenter.normalized;
    }

    /// <summary>
    /// Comportamento: Patrulha aleatória
    /// </summary>
    private Vector2 CalculatePatrolBehavior(Vector2 currentPosition)
    {
        // Atualiza direção de patrulha periodicamente
        patrolTimer -= Time.deltaTime;
        if (patrolTimer <= 0f)
        {
            ChooseNewPatrolDirection();
            patrolTimer = patrolDirectionChangeTime;
        }

        // Suaviza mudanças de direção
        return Vector2.Lerp(moveInput, patrolDirection, Time.deltaTime * 2f);
    }

    /// <summary>
    /// Escolhe nova direção aleatória para patrulha
    /// </summary>
    private void ChooseNewPatrolDirection()
    {
        // Prefere direções que mantêm o bot no centro do mapa
        Vector2 toCenter = mapCenter - (Vector2)transform.position;
        float centerInfluence = Mathf.Clamp01(toCenter.magnitude / threatDetectionRadius);
        
        patrolDirection = new Vector2(
            Random.Range(-1f, 1f) + toCenter.x * centerInfluence * 0.5f,
            Random.Range(-1f, 1f) + toCenter.y * centerInfluence * 0.5f
        ).normalized;
    }

    /// <summary>
    /// Sistema de vetores para evitar obstáculos e ajustar direção
    /// </summary>
    private Vector2 ApplyVectorFieldSystem(Vector2 currentPosition, Vector2 desiredDirection)
    {
        if (!boundsInitialized) return desiredDirection;

        float bestScore = -Mathf.Infinity;
        Vector2 bestDirection = desiredDirection;

        Collider2D[] threats = Physics2D.OverlapCircleAll(currentPosition, threatDetectionRadius, enemyLayer);
        bool hasThreats = threats.Length > 0;

        for (int i = 0; i < candidateDirections.Length; i++)
        {
            Vector2 dir = candidateDirections[i];
            
            // --- AMEAÇAS ---
            float threat = 0f;

            // Ameaça de parede
            Vector2 probePos = currentPosition + dir * wallDetectionDistance;
            if (IsOutOfBounds(probePos))
            {
                threat = 1.0f;
            }

            // Ameaça de inimigos
            if (threat < 1.0f && hasThreats)
            {
                foreach (var threatObj in threats)
                {
                    Vector2 dirToThreat = (Vector2)threatObj.transform.position - currentPosition;
                    float distance = dirToThreat.magnitude;
                    
                    if (distance > 0)
                    {
                        float dot = Vector2.Dot(dir, dirToThreat.normalized);
                        if (dot > 0.3f) // Mais sensível a inimigos
                        {
                            float enemyThreat = (1.0f - (distance / threatDetectionRadius)) * dot;
                            threat = Mathf.Max(threat, enemyThreat);
                        }
                    }
                }
            }

            // --- INTERESSES ---
            float interest = 0f;

            // Interesse na direção desejada
            float alignmentWithDesired = Vector2.Dot(dir, desiredDirection.normalized);
            interest += alignmentWithDesired * 0.8f;

            // Interesse no centro (apenas se não houver ameaças imediatas)
            if (!hasThreats || threat < 0.3f)
            {
                Vector2 toCenter = (mapCenter - currentPosition).normalized;
                float centerInterest = Vector2.Dot(dir, toCenter) * centerSeekingWeight;
                interest += centerInterest;
            }

            // --- SCORE FINAL ---
            float finalScore = interest - threat;
            
            threatScores[i] = threat;
            interestScores[i] = interest;

            if (finalScore > bestScore)
            {
                bestScore = finalScore;
                bestDirection = dir;
            }
        }

        return bestDirection;
    }

    /// <summary>
    /// Verifica se uma posição está fora dos limites do mapa
    /// </summary>
    private bool IsOutOfBounds(Vector2 position)
    {
        return position.x < bottomLeftMarker.position.x + botBoundsPadding ||
               position.x > topRightMarker.position.x - botBoundsPadding ||
               position.y < bottomLeftMarker.position.y + botBoundsPadding ||
               position.y > topRightMarker.position.y - botBoundsPadding;
    }

    /// <summary>
    /// Encontra a ameaça mais próxima
    /// </summary>
    private Transform GetClosestThreat(Collider2D[] threats)
    {
        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (var threat in threats)
        {
            float distance = Vector2.Distance(transform.position, threat.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = threat.transform;
            }
        }

        return closest;
    }

    // --- DEBUG VISUAL MELHORADO ---
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || candidateDirections == null) return;

        // Cores por estado
        Color stateColor = Color.white;
        switch (currentState)
        {
            case BotState.Fugindo: stateColor = Color.red; break;
            case BotState.Atacando: stateColor = Color.yellow; break;
            case BotState.BuscandoCentro: stateColor = Color.green; break;
            case BotState.Patrulha: stateColor = Color.blue; break;
        }

        // Desenha estado atual
        Gizmos.color = stateColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Desenha direções do vetor field
        for (int i = 0; i < candidateDirections.Length; i++)
        {
            Vector2 dir = candidateDirections[i];
            float threat = threatScores[i];
            float interest = interestScores[i];

            Color gizmoColor = new Color(threat, interest, 0);
            Gizmos.color = gizmoColor;
            Gizmos.DrawRay(transform.position, dir * 1.5f);
        }

        // Direção atual
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, moveInput.normalized * 2.5f);

        // Range de detecção e ataque
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, threatDetectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, safeDistanceFromEnemies);

        // Alvo atual
        if (currentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}
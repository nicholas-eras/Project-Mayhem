using UnityEngine;
using System.Linq;

public class BotController : MonoBehaviour
{
    [Header("Stats de Movimento")]
    [SerializeField] private float baseMoveSpeed = 5f;
    private float currentMoveSpeed;

    [Header("Comportamento IA")]
    [Tooltip("Raio para procurar inimigos.")]
    [SerializeField] private float threatDetectionRadius = 8f;
    [Tooltip("Camada onde os inimigos estão.")]
    [SerializeField] private LayerMask enemyLayer;
    [Tooltip("O peso que 'ir para o centro' tem na decisão.")]
    [SerializeField] private float centerSeekingWeight = 0.4f;
    [Tooltip("A que distância o bot 'vê' uma parede.")]
    [SerializeField] private float wallDetectionDistance = 2.5f;

    [Header("Limites do Mapa")]
    [SerializeField] private float botBoundsPadding = 0.5f;

    [Header("Comportamento IA")]
    // ... (suas outras variáveis)
    [Tooltip("A que distância do centro o bot para de se mover (se não houver ameaças).")]
    [SerializeField] private float centerStoppingDistance = 0.5f; // <-- ADICIONE ESTA LINHA

    // --- Variáveis Internas da IA ---
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool boundsInitialized = false;
    private Transform bottomLeftMarker;
    private Transform topRightMarker;
    private Vector2 mapCenter;

    // Vetores para as 8 direções de "sondagem"
    private Vector2[] candidateDirections;
    // Scores de ameaça e interesse para debug no Gizmos
    private float[] threatScores = new float[8];
    private float[] interestScores = new float[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentMoveSpeed = baseMoveSpeed;
    }

    void Start()
    {
        InitializeBounds();
        InitializeCandidateDirections();
    }

    // Inicializa os 8 vetores de direção
    void InitializeCandidateDirections()
    {
        candidateDirections = new Vector2[8];
        candidateDirections[0] = new Vector2( 0,  1).normalized; // N
        candidateDirections[1] = new Vector2( 1,  1).normalized; // NE
        candidateDirections[2] = new Vector2( 1,  0).normalized; // E
        candidateDirections[3] = new Vector2( 1, -1).normalized; // SE
        candidateDirections[4] = new Vector2( 0, -1).normalized; // S
        candidateDirections[5] = new Vector2(-1, -1).normalized; // SW
        candidateDirections[6] = new Vector2(-1,  0).normalized; // W
        candidateDirections[7] = new Vector2(-1,  1).normalized; // NW
    }

    void InitializeBounds()
    {
        Transform mapBoundsParent = null;
        GameObject boundsGO = GameObject.Find("--- MAP BOUNDS ---");
        if (boundsGO != null)
        {
            mapBoundsParent = boundsGO.transform;
        }
        
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
            // Calcula o centro do mapa UMA VEZ
            mapCenter = (bottomLeftMarker.position + topRightMarker.position) / 2f;
        }
    }


    void Update()
    {
        // O "Cérebro" do Bot: Decide a melhor direção para ir
        CalculateBestMoveDirection();
    }

    void FixedUpdate()
    {
        // O "Corpo" do Bot: Apenas se move na direção que o cérebro decidiu
        // Não precisamos mais do Clamp aqui, pois a lógica de IA já evita as paredes.
        rb.MovePosition(rb.position + moveInput.normalized * currentMoveSpeed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// O Cérebro da IA. Calcula os scores de Ameaça e Interesse para 8 direções.
    /// </summary>
    /// <summary>
    /// O Cérebro da IA. Calcula os scores de Ameaça e Interesse para 8 direções.
    /// </summary>
    private void CalculateBestMoveDirection()
    {
        if (!boundsInitialized)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 currentPosition = transform.position;
        
        // --- LÓGICA DE DISTÂNCIA E AMEAÇA (MOVIDA PARA CIMA) --- // <-- MUDANÇA
        Vector2 dirToCenter = (mapCenter - currentPosition);
        float distanceToCenter = dirToCenter.magnitude;
        
        Collider2D[] allThreats = Physics2D.OverlapCircleAll(currentPosition, threatDetectionRadius, enemyLayer);
        bool hasEnemyThreats = allThreats.Length > 0;
        // --- FIM DA MUDANÇA ---


        // --- LÓGICA DE PARADA (A SOLUÇÃO) --- // <-- MUDANÇA
        // Se NÃO houver inimigos E estivermos dentro da "zona de conforto" do centro...
        if (!hasEnemyThreats && distanceToCenter <= centerStoppingDistance)
        {
            // ...a decisão é simples: PARAR.
            moveInput = Vector2.zero;
            
            // Limpa os scores para o Gizmo não mostrar lixo
            for(int i = 0; i < 8; i++) { threatScores[i] = 0; interestScores[i] = 0; }
            
            return; // Pula todo o resto do cálculo
        }
        // --- FIM DA MUDANÇA ---


        // Se chegamos aqui, ou há inimigos, ou estamos longe do centro.
        // O cálculo continua normalmente.

        float bestScore = -Mathf.Infinity;
        Vector2 bestDirection = Vector2.zero;

        // 2. Itera por todas as 8 direções candidatas
        for (int i = 0; i < candidateDirections.Length; i++)
        {
            Vector2 dir = candidateDirections[i];
            
            // --- A. CÁLCULO DE AMEAÇA (Threat) ---
            float threat = 0f;

            // A.1 Ameaça de Parede
            Vector2 probePos = currentPosition + dir * wallDetectionDistance;
            if (probePos.x < bottomLeftMarker.position.x + botBoundsPadding || 
                probePos.x > topRightMarker.position.x - botBoundsPadding ||
                probePos.y < bottomLeftMarker.position.y + botBoundsPadding || 
                probePos.y > topRightMarker.position.y - botBoundsPadding)
            {
                threat = 1.0f; 
            }

            // A.2 Ameaça de Inimigo
            if (threat < 1.0f && hasEnemyThreats) // <-- MUDANÇA (só calcula se 'hasEnemyThreats')
            {
                foreach (var enemy in allThreats)
                {
                    Vector2 dirToEnemy = (Vector2)enemy.transform.position - currentPosition;
                    float distanceToEnemy = dirToEnemy.magnitude;
                    
                    if (distanceToEnemy > 0) // Evita divisão por zero
                    {
                        float dot = Vector2.Dot(dir, dirToEnemy.normalized);
                        if (dot > 0.5) 
                        {
                            float enemyThreat = (1.0f - (distanceToEnemy / threatDetectionRadius)) * dot;
                            if (enemyThreat > threat)
                            {
                                threat = enemyThreat; 
                            }
                        }
                    }
                }
            }
            
            // --- B. CÁLCULO DE INTERESSE (Interest) ---
            float interest = 0f;
            
            // B.1 Interesse do Centro
            // O bot SÓ se interessa pelo centro se NÃO houver inimigos.
            // Se houver inimigos, a única prioridade é fugir (interesse = 0).
            if (!hasEnemyThreats) // <-- MUDANÇA
            {
                Vector2 dirToCenterNormalized = dirToCenter.normalized;
                float centerDot = Vector2.Dot(dir, dirToCenterNormalized);
                if (centerDot > 0)
                {
                    interest = centerDot * centerSeekingWeight;
                }
            }
            
            // --- C. DECISÃO FINAL ---
            float finalScore = interest - threat;
            
            threatScores[i] = threat;
            interestScores[i] = interest;

            if (finalScore > bestScore)
            {
                bestScore = finalScore;
                bestDirection = dir;
            }
        }

        moveInput = bestDirection;
    }

    // --- GIZMO DE DEBUG (O CÉREBRO) ---
    void OnDrawGizmosSelected()
    {
        if (candidateDirections == null) return;

        // Desenha o "Cérebro" do Bot
        for (int i = 0; i < candidateDirections.Length; i++)
        {
            Vector2 dir = candidateDirections[i];
            float threat = threatScores[i];
            float interest = interestScores[i];

            // A cor final é uma mistura de Ameaça (Vermelho) e Interesse (Verde)
            // Se a ameaça for 1.0 (parede), a cor é Vermelho Sólido.
            Color gizmoColor = new Color(threat, interest / centerSeekingWeight, 0);

            Gizmos.color = gizmoColor;
            Gizmos.DrawRay(transform.position, dir * 2.0f);
        }
        
        // Desenha a direção final escolhida em AZUL
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, moveInput.normalized * 3.0f); // Raio azul mais longo

        // Desenha o raio de detecção
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, threatDetectionRadius);
    }
}
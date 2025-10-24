using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class BossLaserAttack : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject warningLaserPrefab;
    [SerializeField] private GameObject damageLaserPrefab;

    [Header("Configuração de Ataque")]
    [Tooltip("Duração do aviso antes do laser ativo.")]
    [SerializeField] public float warningDuration = 2f; 
    [Tooltip("Duração do laser ativo.")]
    [SerializeField] public float attackDuration = 1f;

    [Header("Offset do Laser")]
    [Tooltip("O quanto o centro do laser deve ser movido para compensar a âncora (tamanho_laser / 2).")]
    [SerializeField] private float laserOffsetDistance;

    [Header("Configuração de Alvo")]
    [Tooltip("Tolerância no eixo Y para considerar que o alvo está na mesma horizontal.")]
    [SerializeField] private float horizontalTolerance = 1.0f;
    [Tooltip("A largura MÁXIMA na frente do Boss que o laser pode atingir. Se for 0, atinge o mapa todo.")]
    [SerializeField] private float attackWidth = 20f; // Novo campo para largura

    [Header("Áudio")] 
    [Tooltip("Nome do som do aviso (Laser carregando/telegrafando).")]
    [SerializeField] private string warningSoundName;
    [Tooltip("Nome do som do laser ativo (Deve ser um som em loop).")]
    [SerializeField] private string attackLoopSoundName;

    private GameObject currentLaserInstance;
    private Coroutine attackRoutine;
    private Transform playerTarget;
    
    // NOVO: Chaves únicas para rastrear este som específico no AudioManager
    private string uniqueWarningSoundKey;
    private string uniqueAttackSoundKey;

    void Start()
    {
        // Encontra o alvo (Player) uma única vez
        playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTarget == null)
        {
            Debug.LogWarning("Player não encontrado (Tag 'Player') para BossLaserAttack.");
        }
        
        // NOVO: Gera as chaves únicas no Start para que possam ser usadas em ClearLaser
        int instanceID = gameObject.GetInstanceID();
        uniqueWarningSoundKey = string.IsNullOrEmpty(warningSoundName) ? string.Empty : $"{warningSoundName}_{instanceID}";
        uniqueAttackSoundKey = string.IsNullOrEmpty(attackLoopSoundName) ? string.Empty : $"{attackLoopSoundName}_{instanceID}";
    }

    // =================================================================
    // MÉTODOS PÚBLICOS DE CONTROLE (Chamados pelo BossEyeController)
    // =================================================================

    /// <summary>
    /// Verifica se o Player está dentro da margem de tolerância horizontal E dentro da largura máxima.
    /// </summary>
    public bool IsPlayerInHorizontalRange()
    {
        if (playerTarget == null) return false;

        // 1. VERIFICAÇÃO VERTICAL (Tolerância Horizontal)
        float verticalDistance = Mathf.Abs(playerTarget.position.y - transform.position.y);
        bool isInYRange = verticalDistance <= horizontalTolerance;

        if (!isInYRange) return false;

        // 2. VERIFICAÇÃO HORIZONTAL (Largura de Ataque)
        float horizontalDistance = Mathf.Abs(playerTarget.position.x - transform.position.x);

        // Se attackWidth for 0, ele deve ter alcance infinito (comportamento anterior)
        if (attackWidth <= 0) return true; 

        // O jogador deve estar dentro da largura de ataque
        bool isInXRange = horizontalDistance <= attackWidth;

        return isInXRange;
    }

    /// <summary>
    /// Inicia o ciclo de ataque. Assumimos que o chamador (BossEyeController)
    /// já verificou se o ataque deve ocorrer.
    /// </summary>
    public void StartLaserAttack()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(LaserAttackRoutine());
    }
    
    /// <summary>
    /// Para o ataque imediatamente, destrói o laser ativo (aviso ou dano).
    /// Chamado pelo BossEyeController quando o projétil é a opção escolhida.
    /// </summary>
    public void AbortIfRunning()
    {
        if (attackRoutine != null)
        {
            ClearLaser();
        }
    }

    // Limpa qualquer laser ativo e para a coroutine de ataque.
    public void ClearLaser()
    {
        if (currentLaserInstance != null)
        {
            Destroy(currentLaserInstance);
            currentLaserInstance = null;
        }
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        // NOVO: Usa a chave única para parar o som de ataque
        if (!string.IsNullOrEmpty(uniqueAttackSoundKey) && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSFX(uniqueAttackSoundKey); 
        }
        
        // NOVO: Usa a chave única para parar o som de aviso
        if (!string.IsNullOrEmpty(uniqueWarningSoundKey) && AudioManager.Instance != null)
        {
             AudioManager.Instance.StopSFX(uniqueWarningSoundKey);
        }
    }

    // =================================================================
    // ROTINA INTERNA DO ATAQUE (Gerencia o fluxo de Aviso -> Dano)
    // =================================================================

    private IEnumerator LaserAttackRoutine()
    {
        // 1. Posição inicial do Olho (Boss)
        Vector3 eyePosition = transform.position;

        // 2. Calcula a posição de spawn aplicando o offset e a direção
        float directionFactor = transform.localScale.x > 0 ? 1f : -1f; 
        Vector3 spawnPos = new Vector3(eyePosition.x + (laserOffsetDistance * directionFactor), eyePosition.y, eyePosition.z);

        // Garante que a instância anterior (se houver) seja destruída.
        if (currentLaserInstance != null) Destroy(currentLaserInstance);
        currentLaserInstance = null;

        // A. FASE DE AVISO

        // Instancia o AVISO. SEM PARENT.
        currentLaserInstance = Instantiate(warningLaserPrefab, spawnPos, Quaternion.identity); 
        
        // **** CORREÇÃO DE ROTAÇÃO PARA HORIZONTAL ****
        currentLaserInstance.transform.rotation = Quaternion.identity; 

        // *** APLICA A ESCALA HORIZONTAL (Laser de Aviso) ***
        Vector3 warningScale = currentLaserInstance.transform.localScale;
        currentLaserInstance.transform.localScale = warningScale;
        // ***************************************************************

        // Ajusta a profundidade Z. Corrigido para -2f para resolver o problema de profundidade.
        currentLaserInstance.transform.position = new Vector3(currentLaserInstance.transform.position.x, currentLaserInstance.transform.position.y, -2f);

        // NOVO: Toca o som de aviso usando a chave única (PlayLoopingSFX)
        if (!string.IsNullOrEmpty(uniqueWarningSoundKey) && AudioManager.Instance != null)
        {
            // O AudioManager deve ser configurado para encontrar o som real (warningSoundName) 
            // e usar a chave única (uniqueWarningSoundKey) para rastreá-lo.
            AudioManager.Instance.PlayLoopingSFX(uniqueWarningSoundKey); 
        }

        yield return new WaitForSeconds(warningDuration);

        // --- TRANSIÇÃO ---

        // NOVO: PARAR O SOM DO AVISO EXPLICITAMENTE USANDO CHAVE ÚNICA
        if (!string.IsNullOrEmpty(uniqueWarningSoundKey) && AudioManager.Instance != null)
        {
             AudioManager.Instance.StopSFX(uniqueWarningSoundKey);
        }
        
        // Limpa o AVISO 
        if (currentLaserInstance != null)
        {
            Destroy(currentLaserInstance);
            currentLaserInstance = null;
        }

        // B. FASE DE DANO ATIVO

        // Instancia o DANO. SEM PARENT.
        currentLaserInstance = Instantiate(damageLaserPrefab, spawnPos, Quaternion.identity);

        // **** CORREÇÃO DE ROTAÇÃO PARA HORIZONTAL ****
        currentLaserInstance.transform.rotation = Quaternion.identity; 

        // *** APLICA A ESCALA HORIZONTAL (Laser de Dano) ***
        Vector3 damageScale = currentLaserInstance.transform.localScale;
        currentLaserInstance.transform.localScale = damageScale;
        // ***************************************************************

        // Ajusta a profundidade Z
        currentLaserInstance.transform.position = new Vector3(currentLaserInstance.transform.position.x, currentLaserInstance.transform.position.y, -2f);
        
        // NOVO: Toca o som de loop do laser usando a chave única
        if (!string.IsNullOrEmpty(uniqueAttackSoundKey) && AudioManager.Instance != null)
        {
            // O AudioManager deve ser configurado para encontrar o som real (attackLoopSoundName) 
            // e usar a chave única (uniqueAttackSoundKey) para rastreá-lo.
            AudioManager.Instance.PlayLoopingSFX(uniqueAttackSoundKey); 
        }

        // Espera a duração do ataque (dano)
        yield return new WaitForSeconds(attackDuration);

        // Limpa o laser de dano e reseta a coroutine (fim do ciclo)
        ClearLaser();
    }
    
    // =================================================================
    // VISUALIZAÇÃO NO EDITOR (Gizmos)
    // =================================================================

    private void OnDrawGizmosSelected()
    {
        // Desenha o Gizmo apenas se a tolerância ou a largura for maior que zero
        if (horizontalTolerance <= 0 && attackWidth <= 0) return;

        // --- CÁLCULO DA POSIÇÃO E EXTENSÃO ---

        Vector3 eyePosition = transform.position;

        // Extensão horizontal do Gizmo: 
        // Se attackWidth for 0, desenha uma linha longa (50f) para indicar "infinito".
        // Se attackWidth > 0, desenha apenas na largura definida.
        float gizmoExtent = attackWidth > 0 ? attackWidth : 50f;

        // A posição do Olho é o centro da área de detecção.

        // --- DESENHO DO RETÂNGULO DE DETECÇÃO ---
        Gizmos.color = Color.cyan;

        // Largura total para desenhar (da esquerda para a direita do centro do Boss)
        Vector3 boxCenter = eyePosition;
        Vector3 boxSize = new Vector3(gizmoExtent * 2, horizontalTolerance * 2, 0.01f);

        // Se você quer que o retângulo comece a partir do Boss e vá para a frente:

        // 1. Determinar o lado que o Boss está olhando (para a lógica de ataque)
        float directionFactor = 1f; // Assumindo que o lado positivo é a direção do ataque.

        // Vamos usar a lógica mais simples de BoxCentral (Simétrico) para cobrir o X da verificação:
        Gizmos.DrawWireCube(boxCenter, boxSize);

        // --- Desenhar o centro do laser (Posição de Spawn) ---
        Gizmos.color = Color.yellow;

        // Obtém o fator de direção (se o Boss estiver virado)
        // float directionFactor = transform.localScale.x > 0 ? 1f : -1f; 

        // Aplica o offset na direção correta
        Vector3 laserCenterPosition = new Vector3(eyePosition.x + (laserOffsetDistance * directionFactor), eyePosition.y, eyePosition.z);

        Gizmos.DrawWireSphere(laserCenterPosition, 0.2f);

        // Opcional: Desenhar um círculo para a posição do olho
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(eyePosition, 0.3f);

    }
}

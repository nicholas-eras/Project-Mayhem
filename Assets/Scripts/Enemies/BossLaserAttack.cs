// Assumindo a estrutura do BossLaserAttack que você está usando (baseado em coroutines)

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
    [SerializeField] private float warningDuration = 2f; 
    [Tooltip("Duração do laser ativo.")]
    [SerializeField] private float attackDuration = 1f;

    [Header("Offset do Laser")]
    [Tooltip("O quanto o centro do laser deve ser movido para compensar a âncora (tamanho_laser / 2).")]
    [SerializeField] private float laserOffsetDistance = 5f; // Exemplo: Se o laser tem 10 unidades de largura
    [Tooltip("Se o olho está no lado esquerdo ou direito. Afeta a direção do offset.")]
    [SerializeField] private bool isRightSideEye = true;
    
    private GameObject currentLaserInstance;
    private Coroutine attackRoutine;

    // --- MÉTODOS DE CONTROLE ---

    /// <summary>
    /// Inicia o ciclo de ataque: Aviso (2s) -> Dano Ativo (1s)
    /// </summary>
    public void BeginAttack()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(LaserAttackRoutine());
    }

    // Limpa qualquer laser ativo. Chamado por OnAttackEnd
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
    }

    private IEnumerator LaserAttackRoutine()
    {
        Vector3 spawnPos = transform.position; // Posição atual do Olho
        
        // 1. CALCULAR O OFFSET DA ÂNCORA
        // Se isRightSideEye é True, o laser deve ir para a esquerda (Offset negativo no X).
        // Se isRightSideEye é False, o laser deve ir para a direita (Offset positivo no X).
        float finalOffset = isRightSideEye ? -laserOffsetDistance : laserOffsetDistance;
        spawnPos.x += finalOffset;
        
        // ----------------------------------------------------

        // A. FASE DE AVISO (Sombra)
        currentLaserInstance = Instantiate(warningLaserPrefab, spawnPos, Quaternion.identity, transform.parent);
        
        // Ajuste de Z (para aparecer na frente do corpo, mas atrás do Olho)
        currentLaserInstance.transform.localPosition = new Vector3(currentLaserInstance.transform.localPosition.x, currentLaserInstance.transform.localPosition.y, -0.05f);

        yield return new WaitForSeconds(warningDuration);

        // Limpa o aviso
        Destroy(currentLaserInstance);

        // B. FASE DE DANO ATIVO
        currentLaserInstance = Instantiate(damageLaserPrefab, spawnPos, Quaternion.identity, transform.parent);
        
        // Garante que o laser de dano esteja na frente do aviso (se houver Z-fighting)
        currentLaserInstance.transform.localPosition = new Vector3(currentLaserInstance.transform.localPosition.x, currentLaserInstance.transform.localPosition.y, -0.05f);

        // 3. Espera a duração do ataque (dano)
        yield return new WaitForSeconds(attackDuration);

        // Limpa o laser de dano e reseta
        ClearLaser();
    }
}

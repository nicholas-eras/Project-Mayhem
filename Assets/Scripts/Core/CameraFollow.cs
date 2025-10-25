using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arraste o objeto do Jogador para este campo.")]
    public Transform target; // Vamos manter público para debug, mas usaremos o método abaixo
    [Tooltip("Arraste o marcador do canto inferior esquerdo do mapa.")]
    public Transform minBounds; 
    [Tooltip("Arraste o marcador do canto superior direito do mapa.")]
    public Transform maxBounds; 

    [Header("Configuração")]
    [Tooltip("Quão suave será o movimento da câmera. Valores menores = mais lento e suave.")]
    [Range(0.01f, 1.0f)]
    public float smoothSpeed = 0.125f;
    
    private float halfHeight;
    private float halfWidth;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;
    }
    
    // --- NOVO MÉTODO PÚBLICO ---
    /// <summary>
    /// Chamado pelo PlayerSpawner para dizer à câmera quem seguir.
    /// </summary>
    public void AssignTarget(Transform newTarget)
    {
        target = newTarget;
    }
    // --- FIM DO NOVO MÉTODO ---

    void LateUpdate()
    {
        // MODIFICADO: Adicionada checagem para target (pois ele começa nulo)
        if (target == null)
        {
            // Se o jogador morrer e for destruído, ou ainda não foi assignado
            return; 
        }
        
        if (minBounds == null || maxBounds == null)
        {
            Debug.LogWarning("Limites da câmera (MinBounds/MaxBounds) não configurados!");
            FollowWithoutBounds(); // Segue o jogador sem limites
            return;
        }

        // 1. CALCULAR A POSIÇÃO IDEAL
        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // 2. APLICAR OS LIMITES (Clamping)
        float clampedX = Mathf.Clamp(smoothedPosition.x, minBounds.position.x + halfWidth, maxBounds.position.x - halfWidth);
        float clampedY = Mathf.Clamp(smoothedPosition.y, minBounds.position.y + halfHeight, maxBounds.position.y - halfHeight);

        // 3. APLICAR A POSIÇÃO FINAL E SEGURA
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    // --- NOVO MÉTODO DE FALLBACK ---
    /// <summary>
    /// Método de emergência para seguir o jogador se os limites não estiverem definidos.
    /// </summary>
    private void FollowWithoutBounds()
    {
        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    // (Opcional, mas MUITO útil) Desenha os limites da câmera na Scene View para facilitar o debug
    void OnDrawGizmosSelected()
    {
        if (minBounds == null || maxBounds == null) return;
        
        Gizmos.color = Color.green;
        float camHeight = GetComponent<Camera>().orthographicSize * 2;
        float camWidth = camHeight * GetComponent<Camera>().aspect;

        // Desenha um retângulo mostrando a área exata onde o centro da câmera pode se mover
        Vector3 center = (minBounds.position + maxBounds.position) / 2f;
        Vector3 size = new Vector3(maxBounds.position.x - minBounds.position.x - camWidth, maxBounds.position.y - minBounds.position.y - camHeight, 0);
        Gizmos.DrawWireCube(center, size);
    }
}
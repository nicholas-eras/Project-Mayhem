using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arraste o objeto do Jogador para este campo.")]
    public Transform target;
    [Tooltip("Arraste o marcador do canto inferior esquerdo do mapa.")]
    public Transform minBounds; // NOVO: Marcador de limite mínimo
    [Tooltip("Arraste o marcador do canto superior direito do mapa.")]
    public Transform maxBounds; // NOVO: Marcador de limite máximo

    [Header("Configuração")]
    [Tooltip("Quão suave será o movimento da câmera. Valores menores = mais lento e suave.")]
    [Range(0.01f, 1.0f)]
    public float smoothSpeed = 0.125f;
    
    // Variáveis para guardar o tamanho da câmera
    private float halfHeight;
    private float halfWidth;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        // Calcula a altura e largura da visão da câmera em unidades do mundo
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;
    }

    void LateUpdate()
    {
        if (target == null || minBounds == null || maxBounds == null)
        {
            return;
        }

        // 1. CALCULAR A POSIÇÃO IDEAL (como antes)
        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // 2. APLICAR OS LIMITES (Clamping)
        // Criamos os limites para o *centro* da câmera, levando em conta seu tamanho.
        float clampedX = Mathf.Clamp(smoothedPosition.x, minBounds.position.x + halfWidth, maxBounds.position.x - halfWidth);
        float clampedY = Mathf.Clamp(smoothedPosition.y, minBounds.position.y + halfHeight, maxBounds.position.y - halfHeight);

        // 3. APLICAR A POSIÇÃO FINAL E SEGURA
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
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
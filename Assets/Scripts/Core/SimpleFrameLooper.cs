using UnityEngine;

// O AnimationData (ScriptableObject) é necessário.
// O SpriteRenderer é necessário para trocar os sprites.
[RequireComponent(typeof(SpriteRenderer))]
public class SimpleFrameLooper : MonoBehaviour
{
    [Header("Animação")]
    [Tooltip("O Asset com a lista de frames e a velocidade.")]
    public AnimationData animationData; // Usamos apenas UM AnimationData

    [Header("Auto Ajuste")]
    [Tooltip("Se marcado, tenta ajustar o Collider2D do objeto pai ao tamanho do sprite.")]
    [SerializeField] private bool adjustColliderToSprite = false;

    private SpriteRenderer spriteRenderer;

    private float frameTimer;
    private int currentFrameIndex;

    private CircleCollider2D circleCollider;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (adjustColliderToSprite)
        {
            circleCollider = GetComponentInParent<CircleCollider2D>(); // Ou GetComponent<CircleCollider2D>();
        }
    }

    void Start()
    {
        if (animationData == null || animationData.frames.Length == 0)
        {
            Debug.LogError("O objeto '" + gameObject.name + "' não tem o AnimationData configurado!", this);
            enabled = false; // Desativa o script se não houver animação
            return;
        }
        
        // Inicia no primeiro frame
        spriteRenderer.sprite = animationData.frames[0];
    }

    void Update()
    {
        // Verifica a segurança
        if (animationData == null || animationData.frames.Length == 0)
        {
            return;
        }

        // Tempo necessário para cada frame (1 / FPS)
        float frameDuration = 1f / animationData.framesPerSecond;

        // Diminui o timer
        frameTimer += Time.deltaTime;

        if (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration; // Reduz o tempo gasto, mantendo a precisão

            // Avança para o próximo frame
            currentFrameIndex++;

            if (currentFrameIndex >= animationData.frames.Length)
            {
                // Verifica se deve repetir (loop)
                if (animationData.loop)
                {
                    currentFrameIndex = 0; // Volta para o primeiro frame
                }
                else
                {
                    // Se não for loop (bom para efeitos de explosão)
                    currentFrameIndex = animationData.frames.Length - 1;
                    enabled = false; // Para a animação

                    // OPCIONAL: Destruir o objeto após a animação única
                    // Destroy(gameObject); 
                }
            }

            // Atualiza o Sprite Renderer
            spriteRenderer.sprite = animationData.frames[currentFrameIndex];

            // NO SimpleFrameLooper.cs

void Update()
{
    // ... (lógica de avanço de frame) ...

    if (frameTimer >= frameDuration)
    {
        // ... (atualiza o spriteRenderer.sprite) ...

        if (adjustColliderToSprite && circleCollider != null)
        {
            Sprite currentSprite = spriteRenderer.sprite;

            if (currentSprite != null)
            {
                // 1. Calcula o raio base (Tamanho do sprite em unidades do mundo com a escala do objeto visual)
                Vector2 boundsSize = spriteRenderer.bounds.size;
                float radiusFromSprite = Mathf.Max(boundsSize.x, boundsSize.y) / 2f;
                
                // 2. O FATOR DE COMPENSAÇÃO: Pega a escala local do objeto que contém o COLISOR.
                // Isso DESFAZ a multiplicação que o Transform do colisor aplica automaticamente.
                float colliderLocalScaleX = circleCollider.transform.localScale.x;
                
                // Se a escala for 20, dividimos por 20 para normalizar o raio.
                float finalRadius = radiusFromSprite / colliderLocalScaleX; 

                // 3. Aplica o raio compensado
                circleCollider.radius = finalRadius; 
                
                // 4. Centraliza o Offset
                // O offset deve ser o centro do bounds do sprite em coordenadas locais do colisor.
                circleCollider.offset = (Vector2)(spriteRenderer.bounds.center - circleCollider.transform.position);

                // IMPORTANTE: Se o objeto que tem o SimpleFrameLooper for FILHO do objeto que tem o Collider, 
                // você precisará de GetComponentInParent<CircleCollider2D>().
            }
        }
    }
}
        }
    }
}
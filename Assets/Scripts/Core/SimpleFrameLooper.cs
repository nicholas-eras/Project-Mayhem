using UnityEngine;

// O AnimationData (ScriptableObject) é necessário.
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

    private CircleCollider2D circleCollider; // Assumindo que você usa CircleCollider
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (adjustColliderToSprite)
        {
            // Tenta obter o colisor no próprio objeto, ou no pai.
            // Para ser mais seguro em cenários de Boss, é melhor pegar no objeto pai.
            circleCollider = GetComponentInParent<CircleCollider2D>(); 
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
                    // Se for um VFX que deve sumir, descomente a linha abaixo e remova Destroy em outro lugar.
                    // Destroy(gameObject); 
                }
            }

            // Atualiza o Sprite Renderer
            spriteRenderer.sprite = animationData.frames[currentFrameIndex];

            // === LÓGICA DE AJUSTE DO COLISOR (APÓS A ATUALIZAÇÃO DO SPRITE) ===
            if (adjustColliderToSprite && circleCollider != null)
            {
                Sprite currentSprite = spriteRenderer.sprite;

                if (currentSprite != null)
                {
                    // 1. Calcula o raio base (Tamanho do sprite em unidades do mundo)
                    // Usa a dimensão máxima (bounds size) para garantir que o círculo cubra o sprite
                    Vector2 boundsSize = spriteRenderer.bounds.size;
                    float radiusFromSprite = Mathf.Max(boundsSize.x, boundsSize.y) / 2f;
                    
                    // 2. COMPENSAÇÃO DE ESCALA LOCAL DO COLISOR
                    // Se este script estiver no objeto VISUAL (filho) e o COLISOR estiver no PAI, 
                    // o raio precisa ser normalizado pela escala do objeto pai (circleCollider.transform).
                    float colliderLocalScaleX = circleCollider.transform.localScale.x;
                    float finalRadius = radiusFromSprite / colliderLocalScaleX; 

                    // 3. Aplica o raio compensado
                    circleCollider.radius = finalRadius; 
                    
                    // 4. Centraliza o Offset
                    // Calcula o deslocamento do centro do bounds do sprite para o centro do transform do colisor.
                    circleCollider.offset = (Vector2)(spriteRenderer.bounds.center - circleCollider.transform.position);
                }
            }
            // === FIM LÓGICA DE AJUSTE DO COLISOR ===
        }
    }
}

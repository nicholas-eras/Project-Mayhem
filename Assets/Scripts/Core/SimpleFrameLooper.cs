using UnityEngine;

// O AnimationData (ScriptableObject) é necessário.
// O SpriteRenderer é necessário para trocar os sprites.
[RequireComponent(typeof(SpriteRenderer))]
public class SimpleFrameLooper : MonoBehaviour
{
    [Header("Animação")]
    [Tooltip("O Asset com a lista de frames e a velocidade.")]
    public AnimationData animationData; // Usamos apenas UM AnimationData

    private SpriteRenderer spriteRenderer;

    private float frameTimer;
    private int currentFrameIndex;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        }
    }
}
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // [SerializeField] faz a variável aparecer no Inspector do Unity
    [Header("Stats de Movimento")]
    // A velocidade base que será usada para o cálculo inicial
    [SerializeField] private float baseMoveSpeed = 5f;

    // A velocidade atual que é usada no FixedUpdate
    public float currentMoveSpeed;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    [HideInInspector] public HealthSystem healthSystem;

    private Vector2 externalMoveInput = Vector2.zero; // Inicialmente zero
    
    // Awake é chamado uma vez, antes do Start
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        healthSystem = GetComponent<HealthSystem>(); // Pega a referência do sistema de vida
        
        // Inicializa a velocidade atual com a base
        currentMoveSpeed = baseMoveSpeed;
    }

    void Update()
    {
        // *** BLOQUEIO DE INPUT #1: Zera o vetor no Update ***
        if (UpgradeManager.IsShopOpen) 
        {
            // Se a loja está aberta, forçamos o vetor de input a ser zero.
            // Isso anula qualquer input que o JoystickMove possa ter injetado no FixedUpdate anterior.
            moveInput = Vector2.zero;
            externalMoveInput = Vector2.zero;
            return; // Sai do Update, ignorando o WASD/Joystick
        }
        
        // --- Lógica Normal de Input ---
        
        // 1. Prioriza o Input Externo (Joystick)
        if (externalMoveInput.sqrMagnitude > 0.01f) 
        {
            moveInput = externalMoveInput;
        }
        else
        {
            // 2. Fallback para WASD se o Joystick não estiver ativo
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
        }
    }

void FixedUpdate()
{
    rb.MovePosition(rb.position + moveInput.normalized * currentMoveSpeed * Time.fixedDeltaTime);
}

    public void SetExternalMovementInput(Vector2 direction)
    {
        // Recebe a direção contínua (-1 a 1) do joystick
        externalMoveInput = direction;
    }

    public void IncreaseSpeedMultiplier(float percentage)
    {
        // Aplica o multiplicador (ex: 0.1 para +10%)
        currentMoveSpeed += percentage;
    }
}
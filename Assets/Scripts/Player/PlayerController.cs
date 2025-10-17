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
    
    // Awake é chamado uma vez, antes do Start
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        healthSystem = GetComponent<HealthSystem>(); // Pega a referência do sistema de vida
        
        // Inicializa a velocidade atual com a base
        currentMoveSpeed = baseMoveSpeed;
    }

    // Update é chamado a cada frame
    void Update()
    {
        // Lê o input do teclado (WASD ou setas)
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
    }

    // FixedUpdate é chamado em um intervalo de tempo fixo, ideal para física
    void FixedUpdate()
    {
        // Normalizamos o vetor para que o movimento na diagonal não seja mais rápido
        rb.MovePosition(rb.position + moveInput.normalized * currentMoveSpeed * Time.fixedDeltaTime);
    }

    public void IncreaseSpeedMultiplier(float percentage)
    {
        // Aplica o multiplicador (ex: 0.1 para +10%)
        currentMoveSpeed += percentage;
    }
}
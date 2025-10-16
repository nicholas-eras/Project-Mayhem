using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // [SerializeField] faz a variável aparecer no Inspector do Unity
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    // Awake é chamado uma vez, antes do Start
    void Awake()
    {
        // Pega a referência do componente Rigidbody2D no mesmo GameObject
        rb = GetComponent<Rigidbody2D>();
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
        rb.MovePosition(rb.position + moveInput.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}
using UnityEngine;
using System.Collections;

public class Coin : MonoBehaviour
{
    [Header("Valores da Moeda")]
    public int value = 1;

    [Header("Configuração de Vida Útil")]
    public float lifeTime = 5f;
    public float popForce = 5f;

    [Header("Efeito de Ímã")]
    public float magnetRange = 3f;
    public float attractionSpeed = 8f;

    // Variáveis privadas
    private Rigidbody2D rb;
    private Transform playerTransform; // O alvo (jogador mais próximo)
    private bool isAttracted = false;
    private bool isCollected = false; // Previne coleta dupla

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // REMOVEMOS a lógica de FindGameObjectWithTag daqui.
        
        // Efeito "pop" (continua igual)
        Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        rb.AddForce(randomDirection * popForce, ForceMode2D.Impulse);
        StartCoroutine(SelfDestructRoutine());
    }

    IEnumerator SelfDestructRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!isCollected)
        {
            Destroy(gameObject);
        }
    }

    // --- LÓGICA DO ÍMÃ (MODIFICADA) ---
    void Update()
    {
        // Se já estamos sendo atraídos ou fomos coletados, não fazemos nada.
        if (isAttracted || isCollected)
        {
            return;
        }

        // Se o PlayerManager (lista telefônica) não existe, não fazemos nada.
        if (PlayerManager.Instance == null) return;

        // 1. Pergunta ao Manager qual é o jogador mais próximo
        Transform closestPlayer = PlayerManager.Instance.GetClosestPlayer(transform.position);

        // 2. Se não houver jogadores (ex: morreram), não faz nada.
        if (closestPlayer == null) return;

        // 3. Calcula a distância até ESSE jogador
        float distanceToPlayer = Vector2.Distance(transform.position, closestPlayer.position);

        // 4. Se ele entrou no alcance...
        if (distanceToPlayer <= magnetRange)
        {
            // ... ativamos o modo de atração e DEFINIMOS O ALVO!
            isAttracted = true;
            playerTransform = closestPlayer; // <-- Define o alvo
            
            rb.drag = 0;
            rb.velocity = Vector2.zero;
        }
    }

    // FixedUpdate (Movimento)
    // (Este código já está correto e funciona com a nova lógica do Update)
    void FixedUpdate()
    {
        if (!isAttracted || isCollected || playerTransform == null)
        {
            return;
        }
        
        float step = attractionSpeed * Time.fixedDeltaTime;
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, step);
    }

    // OnTriggerEnter2D (Coleta)
    // (Este código já está correto para dinheiro individual)
    void OnTriggerEnter2D(Collider2D other)
    {
        // Não precisa mais do WalletManager, apenas do Wallet individual
        if (other.CompareTag("Player"))
        {
            PlayerWallet wallet = other.GetComponent<PlayerWallet>();

            if (wallet != null)
            {
                // O dinheiro só vai para o saldo INDIVIDUAL da carteira que tocou.
                if (!isCollected) // Garante que não foi coletada
                {
                    isCollected = true;
                    wallet.AddMoney(value);
                    AudioManager.Instance.PlaySFX("Coin");
                    Destroy(gameObject);
                }
            }
        }
    }


    // ForceAttract (Modificado para usar PlayerManager)
    public void ForceAttract()
    {
        if (isCollected) return;
        
        if (playerTransform != null) { isAttracted = true; }
        else
        {
            if (PlayerManager.Instance == null) { Destroy(gameObject); return; }
            playerTransform = PlayerManager.Instance.GetClosestPlayer(transform.position);
            if (playerTransform == null) { Destroy(gameObject); return; }
            isAttracted = true;
        }
        if (rb != null) { rb.drag = 0; rb.velocity = Vector2.zero; }
    }
}
using UnityEngine;
using System.Collections; // PRECISA DESTA LINHA para usar Coroutines
public class Coin : MonoBehaviour
{
    [Header("Valores da Moeda")]
    [Tooltip("Quanto esta moeda vale?")]
    public int value = 1;

    [Header("Configuração de Vida Útil")] // NOVO HEADER
    [Tooltip("Tempo em segundos antes que a moeda se autodestrua.")]
    public float lifeTime = 5f; // NOVO CAMPO: O tempo de vida
    
    [Tooltip("Força com que a moeda 'pula' ao ser criada.")]
    public float popForce = 5f;

    [Header("Efeito de Ímã")]
    [Tooltip("A distância em que o jogador começa a atrair a moeda.")]
    public float magnetRange = 3f;

    [Tooltip("A velocidade com que a moeda voa em direção ao jogador.")]
    public float attractionSpeed = 8f;

    // Variáveis privadas
    private Rigidbody2D rb;
    private Transform playerTransform;
    private bool isAttracted = false; // Flag para saber se a moeda está sendo atraída

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Encontra o jogador pela tag e guarda seu Transform para economizar processamento.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Erro: Jogador não encontrado! A moeda não saberá quem seguir.");
        }

        // Efeito "pop": Dá um impulso inicial em uma direção aleatória.
        Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        rb.AddForce(randomDirection * popForce, ForceMode2D.Impulse);
        StartCoroutine(SelfDestructRoutine());
    }

    IEnumerator SelfDestructRoutine()
    {
        // Espera pelo tempo definido em lifeTime
        yield return new WaitForSeconds(lifeTime);
        
        // Destroi a moeda (se não tiver sido coletada antes)
        Destroy(gameObject); 
    }

    // Update é chamado a cada frame, ideal para checar distâncias constantemente.
    void Update()
    {
        // Se não encontramos o jogador ou já estamos sendo atraídos, não precisamos checar a distância.
        if (playerTransform == null || isAttracted)
        {
            return;
        }

        // Calcula a distância entre a moeda e o jogador.
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Se o jogador entrou no alcance do ímã...
        if (distanceToPlayer <= magnetRange)
        {
            // ... ativamos o modo de atração!
            isAttracted = true;
            
            // Opcional, mas recomendado: Zera o "arrasto" do Rigidbody para que
            // o movimento de atração seja mais rápido e direto.
            rb.drag = 0;
            rb.velocity = Vector2.zero; // Cancela qualquer movimento restante do "pop".
        }
    }

    // FixedUpdate é bom para movimento, especialmente se for baseado em física.
    // Mas para um movimento direto como este, podemos fazer no Update também.
    // Vamos usar FixedUpdate para consistência.
    void FixedUpdate()
    {
        // Se a moeda não está sendo atraída, não fazemos nada aqui.
        if (!isAttracted || playerTransform == null)
        {
            return;
        }

        // Move a moeda em direção ao jogador usando a velocidade de atração.
        // Usamos MoveTowards pois é um movimento direto e não físico.
        float step = attractionSpeed * Time.fixedDeltaTime;
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, step);
    }

    // A lógica de coleta continua a mesma.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerWallet wallet = other.GetComponent<PlayerWallet>();
            if (wallet != null)
            {
                wallet.AddMoney(value);
                AudioManager.Instance.PlaySFX("Coin");
                Destroy(gameObject);
            }
        }
    }

    // Adicione um novo método público
    public void ForceAttract()
    {
        // Verifica se o playerTransform foi encontrado em Start()
        if (playerTransform == null)
        {
            Destroy(gameObject); // Apenas destrói se não souber onde ir
            return;
        }

        // Ativa o modo de atração, ignorando a distância
        isAttracted = true;
        
        // Zera a física restante (pop) para garantir que voe reto
        if (rb != null)
        {
            rb.drag = 0;
            rb.velocity = Vector2.zero;
        }
        
        // O FixedUpdate cuidará do movimento daqui em diante.
    }
}
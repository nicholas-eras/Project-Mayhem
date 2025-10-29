using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private List<HealthSystem> activePlayerHealths = new List<HealthSystem>();

    // --- NOVAS VARIÁVEIS AQUI ---
    [Header("World Health Bars")]
    [Tooltip("O Prefab da barra de vida (Slider com script FollowTargetUI)")]
    [SerializeField] private GameObject healthBarPrefab;

    [Tooltip("O Canvas 'Screen Space - Overlay' onde as barras serão criadas")]
    [SerializeField] private Transform healthBarCanvas;

    // Dicionário para rastrear qual barra de vida pertence a qual HealthSystem
    private Dictionary<HealthSystem, GameObject> healthBarInstances = new Dictionary<HealthSystem, GameObject>();
    // --- FIM DAS NOVAS VARIÁVEIS ---

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// Registra um jogador/bot no gerenciador. Deve ser chamado APENAS quando o jogador entra no jogo.
    /// </summary>
    public void RegisterPlayer(HealthSystem playerHealth)
    {
        if (playerHealth == null) return;
        if (activePlayerHealths.Contains(playerHealth)) return; // Já registrado

        activePlayerHealths.Add(playerHealth);

        // --- LÓGICA PARA CRIAR A BARRA DE VIDA ---

        // Se for um jogador humano, NÃO crie a barra flutuante.
        // Assumimos que ele já tem a UI estática (healthBarP1, etc)
        if (IsHumanPlayer(playerHealth.gameObject))
        {
            return;
        }

        // Se chegou aqui, é um BOT.
        // Verifica se temos os prefabs e se já não existe uma barra para ele
        if (healthBarPrefab != null && healthBarCanvas != null && !healthBarInstances.ContainsKey(playerHealth))
        {
            // 1. Instancia a barra de vida como filha do Canvas
            GameObject barInstance = Instantiate(healthBarPrefab, healthBarCanvas);

            // 2. Pega o script de seguir
            FollowTargetUI barUI = barInstance.GetComponent<FollowTargetUI>();

            if (barUI != null)
            {
                // 3. Configura a barra (dizendo quem ela deve seguir e ouvir)
                barUI.Setup(playerHealth);

                // 4. Armazena a referência para destruí-la depois
                healthBarInstances.Add(playerHealth, barInstance);
            }
            else
            {
                Debug.LogError("PlayerManager: O Prefab da barra de vida não tem o script 'FollowTargetUI'!");
                Destroy(barInstance); // Limpa o objeto inútil
            }
        }
    }

    /// <summary>
    /// Remove um jogador da lista. Deve ser chamado APENAS quando o jogador SAI DO JOGO (disconnect, etc).
    /// NÃO deve ser chamado quando o jogador morre temporariamente!
    /// </summary>
    public void UnregisterPlayer(HealthSystem playerHealth)
    {
        if (playerHealth == null) return;

        if (activePlayerHealths.Contains(playerHealth))
        {
            activePlayerHealths.Remove(playerHealth);

            // --- LÓGICA PARA DESTRUIR A BARRA DE VIDA ---
            if (healthBarInstances.ContainsKey(playerHealth))
            {
                // 1. Pega a referência da barra
                GameObject barInstance = healthBarInstances[playerHealth];

                // 2. Remove do dicionário
                healthBarInstances.Remove(playerHealth);

                // 3. Destrói o GameObject da barra de vida
                if (barInstance != null)
                {
                    Destroy(barInstance);
                }
            }
            // --- FIM DA LÓGICA ---


            if (activePlayerHealths.Count == 0)
            {
                Debug.LogWarning("[PlayerManager] Não há mais jogadores na partida!");
            }
        }
    }    

    // *** MÉTODOS NOVOS PARA IDENTIFICAR JOGADORES HUMANOS ***

    /// <summary>
    /// Encontra o jogador humano VIVO mais próximo de uma posição.
    /// PRIORIZA jogadores humanos sobre bots!
    /// </summary>
    public Transform GetClosestHumanPlayer(Vector3 position)
    {
        if (activePlayerHealths.Count == 0) return null;

        // Primeiro tenta encontrar jogadores humanos vivos
        var aliveHumanPlayers = activePlayerHealths
            .Where(h => h != null && h.CurrentHealth > 0 && IsHumanPlayer(h.gameObject))
            .ToList();

        if (aliveHumanPlayers.Count > 0)
        {
            return aliveHumanPlayers
                .OrderBy(h => (h.transform.position - position).sqrMagnitude)
                .First().transform;
        }

        // Se não há humanos vivos, retorna null
        Debug.LogWarning("[PlayerManager] Nenhum jogador humano vivo encontrado!");
        return null;
    }

    /// <summary>
    /// Retorna o Transform do jogador humano local (se existir)
    /// </summary>
    public Transform GetLocalHumanPlayer()
    {
        // Busca por AgentManager que não seja bot
        foreach (var health in activePlayerHealths)
        {
            if (health != null && health.gameObject.activeInHierarchy)
            {
                AgentManager agent = health.GetComponent<AgentManager>();
                if (agent != null && !agent.IsPlayerBot())
                {
                    return health.transform;
                }
            }
        }

        // Fallback: busca por objetos com PlayerController ativo
        var playerControllers = FindObjectsOfType<PlayerController>();
        foreach (var pc in playerControllers)
        {
            if (pc != null && pc.enabled && pc.gameObject.activeInHierarchy)
            {
                return pc.transform;
            }
        }

        return null;
    }


    /// <summary>
    /// Verifica se um GameObject é um jogador humano (não bot)
    /// </summary>
    public bool IsHumanPlayer(GameObject playerObject)
    {
        if (playerObject == null) return false;
        
        // Método 1: Verifica pela tag
        if (playerObject.CompareTag("Player")) return true;
        if (playerObject.CompareTag("Bot")) return false;
        
        // Método 2: Verifica pelo AgentManager (mais confiável)
        AgentManager agent = playerObject.GetComponent<AgentManager>();
        if (agent != null)
        {
            return !agent.IsPlayerBot();
        }
        
        // Método 3: Fallback - verifica se tem BotController ativo
        BotController botCtrl = playerObject.GetComponent<BotController>();
        if (botCtrl != null && botCtrl.enabled) return false;
        
        // Se não conseguiu determinar, assume que é humano
        return true;
    }

    /// <summary>
    /// Retorna o número de jogadores HUMANOS na partida
    /// </summary>
    public int GetHumanPlayerCount()
    {
        return activePlayerHealths.Count(h => h != null && IsHumanPlayer(h.gameObject));
    }

    // *** MÉTODOS EXISTENTES (mantidos para compatibilidade) ***

    /// <summary>
    /// [MANTIDO] Encontra o jogador/bot VIVO mais próximo de uma posição.
    /// </summary>
    public Transform GetClosestPlayer(Vector3 position)
    {
        if (activePlayerHealths.Count == 0) return null;

        var alivePlayers = activePlayerHealths.Where(h => h != null && h.CurrentHealth > 0).ToList();
        if (alivePlayers.Count == 0) return null;
        if (alivePlayers.Count == 1) return alivePlayers[0].transform;

        return alivePlayers.OrderBy(h => (h.transform.position - position).sqrMagnitude)
                           .First().transform;
    }

    /// <summary>
    /// Retorna o número TOTAL de jogadores/bots na partida (vivos ou mortos).
    /// Use este método para escalar dificuldade de waves!
    /// </summary>
    public int GetActivePlayerCount()
    {
        return activePlayerHealths.Count(h => h != null);
    }

    /// <summary>
    /// Retorna quantos jogadores/bots estão ATUALMENTE VIVOS (CurrentHealth > 0).
    /// </summary>
    public int GetAlivePlayerCount()
    {
        return activePlayerHealths.Count(h => h != null && h.CurrentHealth > 0);
    }

    /// <summary>
    /// Verifica se TODOS os jogadores/bots morreram e dispara o Game Over.
    /// </summary>
    public void CheckForAllPlayersDead()
    {
        if (activePlayerHealths.Count > 0 && GetAlivePlayerCount() <= 0)
        {
            GameManager.Instance?.TriggerGameOverFromPlayerManager();
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private List<HealthSystem> activePlayerHealths = new List<HealthSystem>();

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
        if (!activePlayerHealths.Contains(playerHealth))
        {
            activePlayerHealths.Add(playerHealth);
        }
    }

    /// <summary>
    /// Remove um jogador da lista. Deve ser chamado APENAS quando o jogador SAI DO JOGO (disconnect, etc).
    /// NÃO deve ser chamado quando o jogador morre temporariamente!
    /// </summary>
    public void UnregisterPlayer(HealthSystem playerHealth)
    {
        if (activePlayerHealths.Contains(playerHealth))
        {
            activePlayerHealths.Remove(playerHealth);

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
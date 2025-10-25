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
            Debug.Log($"[PlayerManager] Registrado: {playerHealth.gameObject.name}. Total: {activePlayerHealths.Count}");
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
            Debug.Log($"[PlayerManager] Desregistrado: {playerHealth.gameObject.name}. Restantes: {activePlayerHealths.Count}");

            // Verifica se todos saíram do jogo
            if (activePlayerHealths.Count == 0)
            {
                Debug.LogWarning("[PlayerManager] Não há mais jogadores na partida!");
            }
        }
    }

    /// <summary>
    /// Encontra o jogador VIVO mais próximo de uma posição.
    /// </summary>
    public Transform GetClosestPlayer(Vector3 position)
    {
        if (activePlayerHealths.Count == 0) return null;

        // Filtra apenas os que ainda estão vivos (CurrentHealth > 0)
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
        // Conta TODOS os jogadores registrados (vivos ou mortos)
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
    /// Deve ser chamado pelo HealthSystem.Die() de cada jogador.
    /// </summary>
    public void CheckForAllPlayersDead()
    {
        // Se há jogadores registrados mas NENHUM está vivo
        if (activePlayerHealths.Count > 0 && GetAlivePlayerCount() <= 0)
        {
            Debug.Log("[PlayerManager] Todos os jogadores/bots estão mortos! Avisando GameManager.");
            GameManager.Instance?.TriggerGameOverFromPlayerManager();
        }
    }
} 
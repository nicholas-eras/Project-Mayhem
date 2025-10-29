using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Marca este GameObject como um alvo válido para inimigos atacarem.
/// Também gerencia o registro/desregistro no PlayerManager.
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class PlayerTargetable : MonoBehaviour
{
    private HealthSystem healthSystem;

    void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
    }

    void OnEnable()
    {
        if (healthSystem == null)
        {
            Debug.LogError($"[PlayerTargetable] {gameObject.name} - HealthSystem é NULL no OnEnable!");
            return;
        }

        if (PlayerManager.Instance != null)
        {
            // Registra o jogador quando entra no jogo
            PlayerManager.Instance.RegisterPlayer(healthSystem);
        }
        // else
        // {
        //     Debug.LogWarning($"[PlayerTargetable] {gameObject.name} - PlayerManager.Instance é NULL!");
        // }
    }

    void OnDisable()
    {
        if (healthSystem == null) return;

        // --- CHECAGEM DE REDE/SHUTDOWN ---
        // Se o NetworkManager estiver desligando, não fazemos nada
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ShutdownInProgress)
        {
            return;
        }

        // --- CHECAGEM SE É MORTE REAL ---
        // Se o jogador morreu (vida <= 0), apenas verificamos Game Over
        if (healthSystem.CurrentHealth <= 0)
        {
            if (PlayerManager.Instance != null)
            {
                // ❌ REMOVIDO: UnregisterPlayer (não remove da lista quando morre!)
                // ✅ NOVO: Apenas verifica se todos morreram
                PlayerManager.Instance.CheckForAllPlayersDead();
            }
        }
        // Se o OnDisable foi chamado por outro motivo (troca de cena, etc)
        // E o jogador NÃO está morto, então sim desregistramos
        else
        {
            if (PlayerManager.Instance != null)
            {
                // Desregistra apenas se não for morte (ex: saiu do jogo)
                PlayerManager.Instance.UnregisterPlayer(healthSystem);
            }
        }
    }
}
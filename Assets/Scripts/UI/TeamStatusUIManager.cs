using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Para o VerticalLayoutGroup (Opcional)

/// <summary>
/// Este Singleton gere a criação e destruição dos painéis de status da equipa.
/// Coloque isto no seu Canvas.
/// </summary>
public class TeamStatusUIManager : MonoBehaviour
{
    public static TeamStatusUIManager Instance { get; private set; }

    [Header("Configuração da UI")]
    [Tooltip("O prefab 'PlayerStatusPanel' que tem o script PlayerStatusUI.")]
    [SerializeField] private GameObject playerStatusPanelPrefab;
    
    [Tooltip("O 'container' (ex: um painel com Vertical Layout Group) onde os painéis serão criados.")]
    [SerializeField] private Transform panelContainer;

    // Dicionário para rastrear qual painel pertence a qual jogador
    private Dictionary<AgentManager, GameObject> statusPanels = new Dictionary<AgentManager, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Chamado pelo AgentManager (OnNetworkSpawn) de cada jogador quando ele entra.
    /// </summary>
    public void RegisterPlayer(AgentManager agent)
    {
        if (agent == null || playerStatusPanelPrefab == null || panelContainer == null)
        {
            Debug.LogError("TeamStatusUI: Registo falhou. Prefab ou Container não configurado.");
            return;
        }

        // Não criar um painel duplicado se ele já existir
        if (statusPanels.ContainsKey(agent)) return;

        // 1. Cria uma nova instância do painel de status
        GameObject panelInstance = Instantiate(playerStatusPanelPrefab, panelContainer);

        // 2. Configura o painel para seguir este 'agent'
        PlayerStatusUI statusUI = panelInstance.GetComponent<PlayerStatusUI>();
        if (statusUI != null)
        {
            statusUI.Setup(agent); // Passa o 'AgentManager' inteiro
        }

        // 3. Guarda a referência para o podermos destruir mais tarde
        statusPanels.Add(agent, panelInstance);
    }

    /// <summary>
    /// Chamado pelo AgentManager (OnNetworkDespawn) quando um jogador sai/morre.
    /// </summary>
    public void UnregisterPlayer(AgentManager agent)
    {
        if (agent == null) return;

        // Encontra o painel de UI associado a este 'agent' e destrói-o
        if (statusPanels.TryGetValue(agent, out GameObject panelInstance))
        {
            Destroy(panelInstance);
            statusPanels.Remove(agent);
        }
    }
}
using UnityEngine;
using System.Collections.Generic; // Para List<>
using Unity.Netcode; // <-- Adicione para checar Host/Cliente

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; } // Singleton

    [Header("Referências")]
    [Tooltip("Arraste o Asset 'SkinDatabase'.")]
    [SerializeField] private SkinDatabase skinDatabase;
    [Tooltip("Arraste os 4 GameObjects 'SlotUI' filhos do SlotContainer.")]
    [SerializeField] private List<SlotUI> slotUIs; // Deve ter exatamente 4

    // --- ESTADO DO LOBBY (SIMULADO POR ENQUANTO) ---
    private SlotState[] slotStates = new SlotState[4];
    private string[] playerNames = new string[4]; // Nomes dos jogadores (se Humano)

    // --- FIM DA SIMULAÇÃO ---

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    void Start()
    {
        if (slotUIs == null || slotUIs.Count != 4)
        {
            Debug.LogError("LobbyManager: Configure a lista 'Slot UIs' com exatamente 4 slots!", this);
            return;
        }
        if (skinDatabase == null)
        {
             Debug.LogError("LobbyManager: SkinDatabase não configurado!", this);
            return;
        }

        // Inicializa cada SlotUI
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
            {
                slotUIs[i].Initialize(i, skinDatabase);
            }
        }
    }

    /// <summary>
    /// Configura o estado inicial do lobby (simplificado).
    /// </summary>
    private void InitializeLobbyState()
    {
        for (int i = 0; i < 4; i++)
        {
            slotStates[i] = SlotState.Empty;
            playerNames[i] = "";
        }

        // Se a rede está ativa e somos o Host OU se a rede não está ativa (Single Player implícito)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost || NetworkManager.Singleton == null)
        {
            slotStates[0] = SlotState.Human;
            playerNames[0] = "JogadorHost"; // Placeholder
        }
        // (Clientes terão seu slot preenchido quando conectarem - lógica futura)
    }

    /// <summary>
    /// Verifica se o jogador local é o Host da sessão.
    /// </summary>
    public bool IsLocalPlayerHost() // <-- NOVO HELPER
    {
        // Retorna true se a rede está ativa e somos o Host, OU se a rede não está ativa
        return (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost) || NetworkManager.Singleton == null;
    }

    /// <summary>
    /// Chamado pelo SlotUI quando um slot é clicado.
    /// </summary>
    public void HandleSlotClick(int slotIndex) // <-- NOVO MÉTODO
    {
        // 1. Verifica Permissões: Só o Host pode clicar, e não no próprio slot.
        if (!IsLocalPlayerHost() || slotIndex == 0)
        {
            return;
        }

        // 2. Verifica Estado Atual: Só pode alternar se estiver Vazio ou Bot.
        if (slotStates[slotIndex] == SlotState.Human)
        {
            return; // Não pode chutar ou transformar humano em bot por aqui
        }

        // 3. Alterna o Estado: Vazio -> Bot, Bot -> Vazio
        if (slotStates[slotIndex] == SlotState.Empty)
        {
            slotStates[slotIndex] = SlotState.Bot;
            playerNames[slotIndex] = ""; // Garante que não tenha nome
        }
        else if (slotStates[slotIndex] == SlotState.Bot)
        {
            slotStates[slotIndex] = SlotState.Empty;
        }

        // 4. Atualiza a UI de TODOS os slots
        UpdateAllSlotDisplays();
    }


    public void UpdateAllSlotDisplays()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
            {
                bool canControl = ShouldLocalPlayerControlSlot(i);
                slotUIs[i].UpdateSlotDisplay(slotStates[i], playerNames[i], canControl);
            }
        }
    }

    /// <summary>
    /// Verifica se o jogador local pode controlar o carrossel de skin de um slot.
    /// </summary>
    private bool ShouldLocalPlayerControlSlot(int slotIndex) // <-- LÓGICA ATUALIZADA
    {
        // Se a rede não estiver ativa (Single Player implícito), só controlamos o slot 0
        if (NetworkManager.Singleton == null)
        {
            return slotIndex == 0;
        }

        // Se a rede está ativa:
        if (NetworkManager.Singleton.IsHost)
        {
            // O Host controla seu próprio slot (0) OU qualquer slot que seja um Bot
            return slotIndex == 0 || slotStates[slotIndex] == SlotState.Bot;
        }
        else // Sou um Cliente
        {
            // O Cliente SÓ controla seu próprio slot
            // (Precisamos saber qual é o slot do cliente - lógica futura do Netcode)
            // Por enquanto, vamos assumir que ele NUNCA controla outros slots além do seu
            // (que ainda não sabemos qual é, então retornamos false por segurança para > 0)
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            // TODO: Mapear ClientId para SlotIndex
            int mySlotIndex = -1; // Placeholder - O Netcode dirá isso
            return slotIndex == mySlotIndex;
        }
    }

    /// <summary>
    /// Retorna o ID da skin selecionada para um slot específico.
    /// Chamado antes de iniciar o jogo.
    /// </summary>
    public int GetSkinIDForSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotUIs.Count && slotUIs[slotIndex] != null)
        {
            return slotUIs[slotIndex].GetSelectedSkinID();
        }
        return 0; // Retorna 0 (padrão) se o slot for inválido
    }

    /// <summary>
    /// (NOVO) Chamado pelo NetworkConnect QUANDO o Host é iniciado com sucesso.
    /// Configura o estado inicial e atualiza a UI.
    /// </summary>
    public void SetupLobbyForHost()
    {
        // Limpa tudo (garantia)
        for (int i = 0; i < 4; i++)
        {
            slotStates[i] = SlotState.Empty;
            playerNames[i] = "";
        }

        // Define o Slot 0 como Humano (Host)
        slotStates[0] = SlotState.Human;
        playerNames[0] = "JogadorHost"; // Placeholder - No futuro, pegue um nome real
                                        // Ou use NetworkManager.Singleton.LocalClientId?

        // Atualiza a UI para refletir este estado inicial
        UpdateAllSlotDisplays();
    }


    // --- FUNÇÕES DE SIMULAÇÃO (Para Teste - Chame via botões de debug se quiser) ---
    public void SimulatePlayerJoin(int slotIndex, string name)
    {
        if (slotIndex > 0 && slotIndex < 4 && slotStates[slotIndex] == SlotState.Empty)
        {
            slotStates[slotIndex] = SlotState.Human;
            playerNames[slotIndex] = name;
            UpdateAllSlotDisplays();
        }
    }
    public void SimulatePlayerLeave(int slotIndex)
    {
        if (slotIndex > 0 && slotIndex < 4)
        {
            slotStates[slotIndex] = SlotState.Empty;
            playerNames[slotIndex] = "";
            UpdateAllSlotDisplays();
        }
    }
    
    /// <summary>
    /// Chamado pelo MapSelectorUI ANTES de carregar a cena do jogo.
    /// Salva o estado atual dos slots e skins na classe estática LobbyData.
    /// </summary>
    public void CacheLobbyDataForSceneLoad()
    {
        // Garante que temos 4 slots configurados
        if (slotUIs == null || slotUIs.Count != 4)
        {
            Debug.LogError("Tentando salvar dados do Lobby, mas os slots não estão configurados!");
            return;
        }

        // Cria arrays temporários (ou usa os que você já tem se `slotStates` for membro da classe)
        SlotState[] currentStates = new SlotState[4];
        int[] currentSkinIDs = new int[4];

        for (int i = 0; i < 4; i++)
        {
            if (slotUIs[i] != null)
            {
                // Pega o estado e a skin ID de cada SlotUI
                currentStates[i] = slotUIs[i].CurrentState; // Assume que SlotUI guarda seu estado
                currentSkinIDs[i] = slotUIs[i].GetSelectedSkinID();
            }
            else
            {
                // Slot inválido, marca como vazio
                currentStates[i] = SlotState.Empty;
                currentSkinIDs[i] = 0; // Skin padrão
            }
        }

        // Salva nos campos estáticos para a próxima cena ler
        LobbyData.SlotStates = currentStates;
        LobbyData.SkinIDs = currentSkinIDs;
    }
}
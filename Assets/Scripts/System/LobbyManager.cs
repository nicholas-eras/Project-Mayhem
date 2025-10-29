using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections; // Para FixedString
using System.Linq; // <-- ADICIONE ESTA LINHA

// 1. Mude para NetworkBehaviour
public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Referências")]
    [SerializeField] private SkinDatabase skinDatabase;
    [SerializeField] private List<SlotUI> slotUIs;

    // 2. Defina uma struct para os dados do slot (é mais limpo)
    // INetworkSerializable permite que ela seja enviada pela rede
    public struct LobbySlotData : INetworkSerializable, System.IEquatable<LobbySlotData>
    {
        public SlotState State;
        public FixedString64Bytes PlayerName; // Use FixedString em vez de string
        public int SkinID;
        public ulong ClientId; // Para saber quem é o dono do slot

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref State);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref SkinID);
            serializer.SerializeValue(ref ClientId);
        }

        // Necessário para a NetworkList
        public bool Equals(LobbySlotData other)
        {
            return State == other.State &&
                   PlayerName == other.PlayerName &&
                   SkinID == other.SkinID &&
                   ClientId == other.ClientId;
        }
    }

    // 3. Use uma NetworkList para sincronizar o estado
    // Esta lista é a "fonte da verdade" e só pode ser alterada pelo Host.
    private NetworkList<LobbySlotData> networkSlots;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        networkSlots = new NetworkList<LobbySlotData>();
    }

    // 4. OnNetworkSpawn é chamado quando o objeto entra na rede
    // Em LobbyManager.cs

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            networkSlots.Clear(); // Limpa sempre

            // --- SUBSTITUA A LÓGICA DE RESTAURO PELA SEGUINTE ---

            // Verifica se temos uma configuração de lobby guardada
            if (GameManager.Instance != null && GameManager.Instance.LastLobbySlotStates != null)
            {
                // ESTAMOS A VOLTAR AO LOBBY
                Debug.Log("[LobbyManager] A restaurar estado do lobby guardado (incluindo skins de clientes).");

                var states = GameManager.Instance.LastLobbySlotStates;
                var skins = GameManager.Instance.LastLobbySkinIDs;
                var clientIds = GameManager.Instance.LastLobbyClientIds;

                for (int i = 0; i < 4; i++)
                {
                    SlotState state = states[i];
                    int skin = skins[i];
                    ulong clientId = clientIds[i]; // O ClientId guardado

                    if (i == 0) // Slot 0 (Host)
                    {
                        networkSlots.Add(new LobbySlotData
                        {
                            State = SlotState.Human,
                            PlayerName = "Host",
                            SkinID = skin, // Restaura a skin do Host
                            ClientId = NetworkManager.Singleton.LocalClientId
                        });
                    }
                    else if (state == SlotState.Bot) // Era um Bot
                    {
                        networkSlots.Add(new LobbySlotData
                        {
                            State = SlotState.Bot,
                            PlayerName = "Bot",
                            SkinID = skin, // Restaura a skin do Bot
                            ClientId = 0
                        });
                    }
                    else if (state == SlotState.Human) // Era um Cliente
                    {
                        // Verifica se o cliente com aquele ID guardado AINDA está ligado
                        if (NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId))
                        {
                            // Sim, ele ainda está cá. Restaura-o com a skin!
                            networkSlots.Add(new LobbySlotData
                            {
                                State = SlotState.Human,
                                PlayerName = $"Jogador {clientId}",
                                SkinID = skin, // RESTAURA A SKIN DO CLIENTE
                                ClientId = clientId
                            });
                        }
                        else
                        {
                            // O cliente (dono daquele slot) saiu. Adiciona um slot vazio.
                            networkSlots.Add(new LobbySlotData { State = SlotState.Empty });
                        }
                    }
                    else // Era um slot vazio
                    {
                        networkSlots.Add(new LobbySlotData { State = SlotState.Empty });
                    }
                }

                // Limpa a memória
                GameManager.Instance.LastLobbySlotStates = null;
                GameManager.Instance.LastLobbySkinIDs = null;
                GameManager.Instance.LastLobbyClientIds = null;
            }
            else
            {
                // ESTAMOS A INICIAR O LOBBY (Primeira vez)
                Debug.Log("[LobbyManager] A criar novo estado de lobby.");

                // Slot 0 é o Host (com skin padrão)
                networkSlots.Add(new LobbySlotData
                {
                    State = SlotState.Human,
                    PlayerName = "Host",
                    SkinID = 0,
                    ClientId = NetworkManager.Singleton.LocalClientId
                });
                // Slots 1, 2, 3 começam vazios
                for (int i = 1; i < 4; i++)
                {
                    networkSlots.Add(new LobbySlotData { State = SlotState.Empty });
                }
            }

            // --- FIM DA SUBSTITUIÇÃO ---

            // Escuta por NOVAS conexões de clientes
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }

        // TODOS (Host e Clientes) se inscrevem no evento de mudança da lista
        networkSlots.OnListChanged -= OnNetworkListChanged;
        networkSlots.OnListChanged += OnNetworkListChanged;

        InitializeAllSlotUIs();
        RefreshAllSlotDisplays();
    }    

    // 6.b. Desliga os callbacks quando o Host sai
    public override void OnNetworkDespawn()
    {
        // Limpa as inscrições de eventos
        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
        networkSlots.OnListChanged -= OnNetworkListChanged;
    }

    // 7. A UI é atualizada SEMPRE que a lista muda
    private void OnNetworkListChanged(NetworkListEvent<LobbySlotData> changeEvent)
    {
        RefreshAllSlotDisplays();
    }

    // Configuração inicial da UI (apenas 1 vez)
    private void InitializeAllSlotUIs()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
            {
                // Passa o SkinDatabase E uma referência a este LobbyManager
                // para o SlotUI poder chamar de volta (ex: OnChangeSkin)
                // Assumindo que você vai modificar o SlotUI.Initialize
                slotUIs[i].Initialize(i, skinDatabase/*, this*/); 
            }
        }
    }

    private void RefreshAllSlotDisplays()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null && i < networkSlots.Count)
            {
                LobbySlotData data = networkSlots[i];
                bool canControl = ShouldLocalPlayerControlSlot(i);

                // ATUALIZE ESTA LINHA:
                slotUIs[i].UpdateSlotDisplay(data.State, data.PlayerName.ToString(), canControl, data.SkinID);
            }
        }
    }    
    
    /// <summary>
    /// Chamado pelo SlotUI quando o jogador (dono do slot) muda a skin
    /// </summary>
    public void OnPlayerChangeSkin(int slotIndex, int newSkinID)
    {
        // Precisamos enviar um comando ao servidor
        ChangeSkinServerRpc(slotIndex, newSkinID);
    }

    // 8. ServerRpc: Clientes pedem ao Host para mudar a skin
    [ServerRpc(RequireOwnership = false)] // RequireOwnership = false permite que Clientes chamem
    private void ChangeSkinServerRpc(int slotIndex, int newSkinID, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        LobbySlotData currentData = networkSlots[slotIndex];

        // Validação: O Host controla bots, ou o Cliente controla seu próprio slot
        if ((IsHost && currentData.State == SlotState.Bot) || currentData.ClientId == senderClientId)
        {
            currentData.SkinID = newSkinID;
            networkSlots[slotIndex] = currentData; // Atualiza a lista, sincronizando para todos
        }
    }

    // 9. Host controla cliques de Bot (como antes, mas agora na NetworkList)
    public void HandleSlotClick(int slotIndex)
    {
        if (!IsHost || slotIndex == 0) return;

        LobbySlotData currentData = networkSlots[slotIndex];
        if (currentData.State == SlotState.Human) return; // Não mexe em slots de humanos

        if (currentData.State == SlotState.Empty)
        {
            currentData.State = SlotState.Bot;
            currentData.PlayerName = "Bot";
        }
        else if (currentData.State == SlotState.Bot)
        {
            currentData.State = SlotState.Empty;
            currentData.PlayerName = "";
        }
        
        currentData.SkinID = 0; // Reseta a skin
        networkSlots[slotIndex] = currentData; // Atualiza a lista
    }

    // Lógica de controle de carrossel (lê da NetworkList)
    private bool ShouldLocalPlayerControlSlot(int slotIndex)
    {
        if (!NetworkManager.Singleton.IsListening) // Offline
        {
            return slotIndex == 0;
        }

        if (networkSlots.Count <= slotIndex) return false; // Lista ainda não populada
        
        LobbySlotData data = networkSlots[slotIndex];
        
        if (IsHost)
        {
            // Host controla seu slot (0) ou slots de Bot
            return slotIndex == 0 || data.State == SlotState.Bot;
        }
        else // Sou Cliente
        {
            // Cliente só controla o slot que tem o seu ClientId
            return data.ClientId == NetworkManager.Singleton.LocalClientId;
        }
    }
    
    // 10. (HOST) Gerencia conexões
    private void HandleClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            return;
        }
        // Encontra o primeiro slot vazio
        for (int i = 1; i < 4; i++) // Começa em 1 (0 é o Host)
        {
            if (networkSlots[i].State == SlotState.Empty)
            {
                networkSlots[i] = new LobbySlotData
                {
                    State = SlotState.Human,
                    PlayerName = $"Jogador {clientId}", // TODO: Pegar nome real
                    SkinID = 0,
                    ClientId = clientId
                };
                return; // Achou um slot
            }
        }
        // TODO: Tratar lobby cheio (chutar o cliente?)
    }

    private void HandleClientDisconnect(ulong clientId)
    {
         // Encontra o slot do cliente que saiu
        for (int i = 1; i < 4; i++)
        {
            if (networkSlots[i].ClientId == clientId)
            {
                // Reseta o slot
                networkSlots[i] = new LobbySlotData { State = SlotState.Empty };
                return;
            }
        }
    }

    // 11. Salva os dados para a próxima cena (lendo da NetworkList)
    public void CacheLobbyDataForSceneLoad()
    {
        if (networkSlots.Count != 4)
        {
            Debug.LogError("Tentando salvar dados, mas a NetworkList não tem 4 slots!");
            return;
        }

        SlotState[] currentStates = new SlotState[4];
        int[] currentSkinIDs = new int[4];

        for (int i = 0; i < 4; i++)
        {
            currentStates[i] = networkSlots[i].State;
            currentSkinIDs[i] = networkSlots[i].SkinID;
        }
        
        // --- ADICIONE ESTA LÓGICA ---
        ulong[] currentClientIds = new ulong[4];
        for (int i = 0; i < 4; i++)
        {
            // (Assumindo que já populou currentStates e currentSkinIDs)
            currentClientIds[i] = networkSlots[i].ClientId;
        }
        // --- FIM DA ADIÇÃO ---

        LobbyData.SlotStates = currentStates;
        LobbyData.SkinIDs = currentSkinIDs;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LastLobbySlotStates = currentStates;
            GameManager.Instance.LastLobbySkinIDs = currentSkinIDs;
            GameManager.Instance.LastLobbyClientIds = currentClientIds;
        }
    }
    
    // Removemos SetupLobbyForHost - a lógica agora está em OnNetworkSpawn
    // Removemos IsLocalPlayerHost - use a propriedade "IsHost" do NetworkBehaviour
    // Removemos GetSkinIDForSlot - a lógica agora está em CacheLobbyData...
}
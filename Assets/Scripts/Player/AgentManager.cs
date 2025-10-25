using UnityEngine;
using Unity.Netcode; // Essencial para qualquer script de rede

/// <summary>
/// O "cérebro" do prefab de jogador/bot (PlayerAgent).
/// Gerencia qual controle está ativo (Humano ou IA), qual skin está sendo exibida,
/// e realiza as conexões locais necessárias para o jogador humano dono deste objeto.
/// </summary>
public class AgentManager : NetworkBehaviour
{
    [Header("Base de Dados")]
    [Tooltip("O Asset ScriptableObject que contém a lista de todas as skins.")]
    [SerializeField] private SkinDatabase skinDatabase;

    [Header("Referências de Componentes Internos (Do Prefab)")]
    [Tooltip("Arraste o componente SpriteRenderer (geralmente de um objeto filho 'Visuals').")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Arraste o componente PlayerController (do mesmo objeto).")]
    [SerializeField] private PlayerController playerControl;

    [Tooltip("Arraste o componente BotController (do mesmo objeto).")]
    [SerializeField] private BotController botControl;

    // --- VARIÁVEIS DE REDE ---
    // Sincronizadas do Servidor (Host) para todos os Clientes.

    /// <summary>
    /// O ID da skin (índice da lista no SkinDatabase) que este agente está usando.
    /// </summary>
    public NetworkVariable<int> SkinID = new NetworkVariable<int>(
        0, // Valor padrão (skin 0)
        NetworkVariableReadPermission.Everyone, // Todos podem ler o valor
        NetworkVariableWritePermission.Server // Apenas o Servidor/Host pode alterar o valor
    );

    /// <summary>
    /// Define se este agente é controlado por IA (true) ou por um humano (false).
    /// </summary>
    public NetworkVariable<bool> IsBot = new NetworkVariable<bool>(
        false, // Valor padrão (é um humano)
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // --- VARIÁVEIS LOCAIS ---
    private const string SKIN_PREF_KEY = "Player_ChosenSkinID"; // Chave do PlayerPrefs para skin escolhida
    private bool isSinglePlayer = false; // Flag para saber se foi spawnado pelo SinglePlayerManager

    // --- SETUP DE REDE ---

    /// <summary>
    /// Chamado automaticamente pelo Netcode quando este objeto é spawnado na rede.
    /// Roda em todas as máquinas (Host e Clientes).
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // --- CHECAGEM SINGLE PLAYER ---
        // Se este objeto foi spawnado pelo SinglePlayerManager, ele já foi inicializado.
        // A flag 'isSinglePlayer' nos diz para pular a lógica de rede/conexão local.
        if (isSinglePlayer)
        {
            return;
        }
        // --- FIM DA CHECAGEM ---

        // *** APLICA CONFIGURAÇÕES TEMPORÁRIAS SE FOR BOT ***
        if (temporaryIsBot && IsServer)
        {
            IsBot.Value = true;
            SkinID.Value = temporarySkinID;
            temporaryIsBot = false; // Reseta a flag
        }
    

        // --- LÓGICA DE REDE PADRÃO ---
        // Se inscreve nos eventos de mudança das NetworkVariables
        SkinID.OnValueChanged += OnSkinChanged;
        IsBot.OnValueChanged += OnControllerChanged;

        // Aplica o estado inicial (importante para quem já estava na sala ou para o estado inicial vindo do servidor)
        OnSkinChanged(SkinID.Value, SkinID.Value); // Chama com o valor atual para aplicar a skin
        OnControllerChanged(IsBot.Value, IsBot.Value); // Chama com o valor atual para ativar o controle correto
        
        // --- CONEXÕES LOCAIS (APENAS PARA O DONO DO OBJETO) ---
        if (IsOwner && NetworkObject.IsPlayerObject) // Se sou EU (o jogador local) e NÃO sou um Bot
        {

            // --- A. PEGA OS COMPONENTES DESTE JOGADOR ---
            // (Já temos as referências _playerControl, _botControl, etc. via Inspector)
            JoystickMove moveScript = GetComponent<JoystickMove>();
            HealthSystem healthSystem = GetComponent<HealthSystem>();
            PlayerWeaponManager weaponManager = GetComponent<PlayerWeaponManager>();

            // --- B. ENCONTRA OS OBJETOS DA CENA ---
            // Usar FindObjectOfType pode ser lento se houver muitos objetos,
            // mas para Singletons ou objetos únicos na cena, é aceitável no Start/OnNetworkSpawn.
            CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
            PlayerHealthUI healthUI = FindObjectOfType<PlayerHealthUI>(); // Assume que só há uma UI de vida principal
            Joystick joystick = FindObjectOfType<Joystick>();             // Assume que só há um Joystick principal

            // --- C. REALIZA AS CONEXÕES ---

            // Conecta Joystick E ATIVA
            if (moveScript != null && joystick != null)
            {
                moveScript.AssignJoystick(joystick);
                moveScript.enabled = true; // Garante que o script de input esteja ativo
            }
            else { Debug.LogError($"[AgentManager] {gameObject.name}: Falha ao conectar Joystick. moveScript Nulo? {moveScript == null}, joystick Nulo? {joystick == null}"); }

            // Conecta Barra de Vida
            if (healthSystem != null && healthUI != null)
            {
                healthUI.Setup(healthSystem);
            }
            else { Debug.LogError($"[AgentManager] {gameObject.name}: Falha ao conectar HealthUI. healthSystem Nulo? {healthSystem == null}, healthUI Nulo? {healthUI == null}"); }

            // Conecta Câmera
            if (cameraFollow != null)
            {
                cameraFollow.AssignTarget(this.transform); // Faz a câmera seguir o objeto raiz do prefab
            }
            else { Debug.LogError($"[AgentManager] {gameObject.name}: Falha ao encontrar CameraFollow na cena."); }

            // Conecta Upgrade Manager (que é um Singleton)
            if (UpgradeManager.Instance != null)
            {
                // Pega o GameObject visual do Joystick para poder escondê-lo na loja
                GameObject joyVisual = (joystick != null) ? joystick.gameObject : null;
                UpgradeManager.Instance.RegisterPlayer(this.gameObject, moveScript, joyVisual);
            }
            else { Debug.LogError($"[AgentManager] {gameObject.name}: Falha ao registrar no UpgradeManager (Instance nulo). Verifique se o UpgradeManager está ativo na cena."); }

            // Equipa a Arma Inicial do Mapa (usando o Singleton MapDataManager)
            if (weaponManager != null && MapDataManager.Instance != null)
            {
                weaponManager.InitializeStartingWeapon(MapDataManager.Instance.startingWeaponPrefab);
            }
            else { Debug.LogError($"[AgentManager] {gameObject.name}: Falha ao equipar arma inicial. weaponManager Nulo? {weaponManager == null}, MapDataManager Nulo? {MapDataManager.Instance == null}"); }

            // --- BÔNUS: CONEXÃO DO CONTADOR DE MOEDAS ---
            PlayerWallet wallet = GetComponent<PlayerWallet>(); // A carteira DESTE jogador
            PlayerHUDUI hudUI = FindObjectOfType<PlayerHUDUI>();

            if (wallet != null && hudUI != null)
            {
                // O HUD só é conectado à carteira do jogador local (IsOwner)
                hudUI.ConnectWallet(wallet);
            }
            else
            {
                Debug.LogError($"Falha ao conectar contador de moedas. Wallet Nulo? {wallet == null}, HUDUI Nulo? {hudUI == null}");
            }
    

            // Garante que o PlayerController esteja ativo (OnControllerChanged já faz isso, mas é uma segurança extra)
            if (playerControl != null) playerControl.enabled = true;

            // Pede ao Servidor para usar a Skin escolhida no Lobby (lida do PlayerPrefs)
            int chosenSkinID = PlayerPrefs.GetInt(SKIN_PREF_KEY, 0);
            RequestSkinServerRpc(chosenSkinID);
        }
    }

    /// <summary>
    /// Chamado automaticamente pelo Netcode quando este objeto é destruído/despawnado.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        // Limpa as inscrições para evitar erros de referência se o objeto for reutilizado ou destruído
        if (!isSinglePlayer) // Só desinscreve se estava inscrito (não no Single Player)
        {
            SkinID.OnValueChanged -= OnSkinChanged;
            IsBot.OnValueChanged -= OnControllerChanged;
        }
    }

public void SetupBotInitialState(int skinID)
{
    // 1. Aplica a Skin IMEDIATAMENTE (sem esperar pela rede)
    if (skinDatabase != null && skinID >= 0 && skinID < skinDatabase.skins.Count)
    {
        spriteRenderer.sprite = skinDatabase.skins[skinID].skinSprite;
    }

    // 2. *** CORREÇÃO: Não modifica NetworkVariables antes do spawn ***
    // Em vez disso, vamos usar uma variável local temporária e aplicar no OnNetworkSpawn
    temporaryIsBot = true;
    temporarySkinID = skinID;

    // 3. Marca no HealthSystem que é bot
    HealthSystem health = GetComponent<HealthSystem>();
    if (health != null)
    {
        health.IsBotAgent = true;
    }
}

// *** ADICIONE ESTAS VARIÁVEIS TEMPORÁRIAS ***
private bool temporaryIsBot = false;
private int temporarySkinID = 0;
    // --- FUNÇÃO DE SETUP SINGLE PLAYER ---
    /// <summary>
    /// Chamado EXCLUSIVAMENTE pelo SinglePlayerManager para "ligar" o agente
    /// no modo offline, pulando a lógica de rede.
    /// </summary>
    public void InitializeForSinglePlayer(int defaultSkinID = 0)
    {
        isSinglePlayer = true; // Marca como modo offline

        // Lê a skin escolhida no lobby (ou usa a padrão)
        int chosenSkinID = PlayerPrefs.GetInt(SKIN_PREF_KEY, defaultSkinID);

        // Chama manualmente a lógica de definir a skin ESCOLHIDA
        OnSkinChanged(0, chosenSkinID); // Passa valores antigos irrelevantes

        // Chama manualmente a lógica de definir o controle (sempre como humano no Single Player)
        OnControllerChanged(false, false); // Passa valores antigos irrelevantes
    }

    // --- LÓGICA DE ATIVAÇÃO DE CONTROLE ---

    /// <summary>
    /// Chamado automaticamente (pelo Netcode ou manualmente) quando 'IsBot' muda ou na inicialização.
    /// Ativa/Desativa os scripts PlayerController e BotController.
    /// </summary>
    // No AgentManager, modifique o método OnControllerChanged:
    private void OnControllerChanged(bool oldVal, bool isBot)
    {
        if (playerControl == null || botControl == null)
        {
            Debug.LogError($"[AgentManager] {gameObject.name}: PlayerController ou BotController não estão referenciados!", this);
            return;
        }

        if (isSinglePlayer)
        {
            playerControl.enabled = !isBot;
            botControl.enabled = isBot;
            return;
        }

        // Lógica de Rede:
        if (isBot)
        {
            playerControl.enabled = false;
            botControl.enabled = IsServer;

            // Marca no Health System que é bot
            HealthSystem health = GetComponent<HealthSystem>();
            if (health != null)
            {
                health.IsBotAgent = true;
            }
        }
        else // É um Jogador Humano
        {
            botControl.enabled = false;
            playerControl.enabled = IsOwner;

            // Marca no Health System que NÃO é bot
            HealthSystem health = GetComponent<HealthSystem>();
            if (health != null)
            {
                health.IsBotAgent = false;
            }
        }
    }


    public bool IsPlayerBot()
    {
        // No modo single player, usa a lógica local
        if (isSinglePlayer)
        {
            return botControl != null && botControl.enabled;
        }

        // No modo multiplayer, usa a NetworkVariable
        return IsBot.Value;
    }

    // --- LÓGICA DE MUDANÇA DE SKIN ---

    /// <summary>
    /// Chamado automaticamente (pelo Netcode ou manualmente) quando 'SkinID' muda ou na inicialização.
    /// Aplica o Sprite correto ao SpriteRenderer.
    /// </summary>
    private void OnSkinChanged(int oldID, int newID)
    {
        // Validações
        if (skinDatabase == null || skinDatabase.skins == null)
        {
            Debug.LogError($"[AgentManager] {gameObject.name}: SkinDatabase não configurado.", this);
            return;
        }
         if (newID < 0 || newID >= skinDatabase.skins.Count)
        {
            Debug.LogError($"[AgentManager] {gameObject.name}: Tentando aplicar SkinID inválido {newID}. Máximo é {skinDatabase.skins.Count - 1}.", this);
            newID = 0; // Tenta usar a skin padrão como fallback
        }
        if (spriteRenderer == null)
        {
            Debug.LogError($"[AgentManager] {gameObject.name}: SpriteRenderer não está configurado no Inspector!", this);
            return;
        }

        // Pega o sprite correto do banco de dados usando o novo ID
        Sprite newSprite = skinDatabase.skins[newID].skinSprite;

        // Aplica o novo sprite ao componente SpriteRenderer
        spriteRenderer.sprite = newSprite;
    }

    // --- FUNÇÕES DE CONTROLE (Chamadas Externas) ---

    /// <summary>
    /// (APENAS SERVIDOR) Usado pelo Host/GameSceneManager para forçar a configuração
    /// de um Agente (ex: ao spawnar um Bot). Define IsBot e SkinID.
    /// </summary>
    public void InitializeAgent(bool isBot, int skinID)
    {
        // Apenas o Host/Servidor pode mudar essas variáveis diretamente
        if (!IsServer) return;

        IsBot.Value = isBot;
        SkinID.Value = skinID;
    }

    /// <summary>
    /// (APENAS CLIENTE/OWNER) Usado por um jogador Humano (IsOwner == true) para pedir
    /// uma mudança de skin ao Servidor. O Cliente "pede", o Servidor "executa" a mudança,
    /// que é então replicada para todos via NetworkVariable.
    /// </summary>
    [ServerRpc] // Indica que esta função é chamada pelo Cliente mas executa no Servidor
    public void RequestSkinServerRpc(int skinID)
    {
        // O Servidor pode adicionar validações aqui (ex: checar se a skin está destravada)

        // O Servidor atualiza a NetworkVariable. A mudança será enviada
        // automaticamente para todos os clientes, e o OnValueChanged() fará o resto.
        SkinID.Value = skinID;
    }
}
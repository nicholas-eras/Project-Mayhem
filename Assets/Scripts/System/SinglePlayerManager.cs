using UnityEngine;
using Unity.Netcode; // Precisamos disso para checar o modo de jogo

public class SinglePlayerManager : MonoBehaviour
{
    [Header("Configuração do Jogador")]
    [Tooltip("Arraste seu 'PlayerAgent_Prefab' aqui.")]
    [SerializeField] private GameObject playerPrefab;
    
    [Tooltip("Arraste o script 'PlayerSpawn' (marcador) do P1.")]
    [SerializeField] private PlayerSpawn playerSpawnPoint;

    [Header("Dependências da Cena (Arraste da Hierarquia)")]
    [SerializeField] private CameraFollow mainCameraFollow;
    [SerializeField] private Joystick joystickP1; 
    [SerializeField] private PlayerHealthUI healthBarP1; // <-- Conexão da Vida

    // --- Variável para guardar a arma ---
    private GameObject mapSpecificWeapon;

    void Start()
    {
        // 1. CHECAGEM DE MODO DE JOGO
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            gameObject.SetActive(false); // Desativa este manager
            return; 
        }

        if (GameModeManager.Instance == null || GameModeManager.Instance.CurrentMode != GameMode.SinglePlayer)
        {
            gameObject.SetActive(false); // Desativa este manager
            return;
        }

        // 2. PEGA A ARMA DO MAPA
        if (MapDataManager.Instance != null)
        {
            mapSpecificWeapon = MapDataManager.Instance.startingWeaponPrefab;
        }
        else
        {
            Debug.LogError("[SinglePlayerManager] MapDataManager.Instance não encontrado!");
        }

        // 3. MODO SINGLE PLAYER CONFIRMADO
        SpawnLocalPlayer();
    }

    private void SpawnLocalPlayer()
    {
        if (playerPrefab == null || playerSpawnPoint == null)
        {
            Debug.LogError("[SinglePlayerManager] Prefab do Jogador ou Ponto de Spawn não configurados!");
            return;
        }

        // 1. Instancia o jogador
        Transform spawnPoint = playerSpawnPoint.transform;
        GameObject playerGO = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // 2. Inicializa o Agente (Define a Skin e o Controle)
        AgentManager agent = playerGO.GetComponent<AgentManager>();
        if (agent != null)
        {
            agent.InitializeForSinglePlayer(0); // Usa a skin padrão 0
        }
        else
        {
            Debug.LogError("O PlayerAgent_Prefab não tem o script AgentManager.cs!");
            return;
        }

        // 3. Conecta o Joystick E ATIVA O SCRIPT
        JoystickMove moveScript = playerGO.GetComponent<JoystickMove>();
        if (moveScript != null && joystickP1 != null)
        {
            moveScript.AssignJoystick(joystickP1);
            moveScript.enabled = true; // <-- CORREÇÃO DO JOYSTICK
        }

        // 4. Conecta a Barra de Vida (Health UI)
        HealthSystem healthSystem = playerGO.GetComponent<HealthSystem>();
        if (healthSystem != null && healthBarP1 != null)
        {
            healthBarP1.Setup(healthSystem); // <-- CONEXÃO DA VIDA
        }

        // 5. Conecta a Câmera
        if (mainCameraFollow != null) 
        {
            mainCameraFollow.AssignTarget(playerGO.transform); // <-- CONEXÃO DA CÂMERA
        }
        
        // 6. Conecta o Upgrade Manager
        if (UpgradeManager.Instance != null)
        {
            GameObject joyVisual = (joystickP1 != null) ? joystickP1.gameObject : null;
            UpgradeManager.Instance.RegisterPlayer(playerGO, moveScript, joyVisual);
        }

        // 7. Equipa a Arma do Mapa
        PlayerWeaponManager weaponManager = playerGO.GetComponent<PlayerWeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.InitializeStartingWeapon(mapSpecificWeapon); 
        }

        // 8. Força a ativação do PlayerController (garantia final)
        PlayerController controller = playerGO.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = true;
        }
    }
}
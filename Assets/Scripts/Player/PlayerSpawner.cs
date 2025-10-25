using UnityEngine;
// Importe o namespace do seu Joystick
// Ex: using UnityEngine.InputSystem.OnScreen;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Prefabs dos Jogadores")]
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;

    [Header("Pontos de Spawn")]
    [SerializeField] private Transform player1SpawnPoint;
    [SerializeField] private Transform player2SpawnPoint;

    // --- SEÇÃO DE DEPENDÊNCIAS SEPARADA ---

    [Header("Dependências da Cena - P1")]
    [Tooltip("O Joystick que o P1 vai usar.")]
    [SerializeField] private Joystick joystickP1; 
    
    [Tooltip("A UI de Barra de Vida do P1.")]
    [SerializeField] private PlayerHealthUI healthBarP1; // <-- NOVO CAMPO AQUI

    [Header("Dependências da Cena - P2")]
    [Tooltip("O Joystick que o P2 vai usar (pode ser o mesmo do P1).")]
    [SerializeField] private Joystick joystickP2; 
    
    [Tooltip("A UI de Barra de Vida do P2 (se houver).")]
    [SerializeField] private PlayerHealthUI healthBarP2; // <-- NOVO CAMPO AQUI

    // --- NOVO CAMPO PARA A CÂMERA ---
    [Header("Dependências da Cena - Câmera")] // <-- NOVO
    [Tooltip("Arraste a Câmera Principal (que tem o script CameraFollow).")] // <-- NOVO
    [SerializeField] private CameraFollow mainCameraFollow; // <-- NOVO

    void Start()
    {
        // --- CHECAGEM DA CÂMERA (PLANO B) ---
        if (mainCameraFollow == null) // <-- NOVO
        {
            mainCameraFollow = FindObjectOfType<CameraFollow>();
            if (mainCameraFollow == null)
            {
                Debug.LogError("PlayerSpawner: Câmera com script 'CameraFollow' não foi encontrada!");
            }
        }

        // (O resto do seu Start() continua o mesmo)
        if (GameModeManager.Instance == null)
        {
            // SpawnSinglePlayer();
            SpawnMultiplayer();
            return;
        }

        GameMode mode = GameModeManager.Instance.CurrentMode;

        if (mode == GameMode.SinglePlayer)
        {
            SpawnSinglePlayer();
        }
        else if (mode == GameMode.Multiplayer)
        {
            SpawnMultiplayer();
        }
    }

    private void SpawnSinglePlayer()
    {
        if (player1Prefab == null || player1SpawnPoint == null) return;

        // 1. Instancia
        GameObject playerGO = Instantiate(player1Prefab, player1SpawnPoint.position, player1SpawnPoint.rotation);

        // 2. Conecta Joystick
        JoystickMove moveScript = playerGO.GetComponent<JoystickMove>();
        if (moveScript != null && joystickP1 != null)
        {
            moveScript.AssignJoystick(joystickP1);
        }

        // 3. Conecta Barra de Vida
        HealthSystem healthSystem = playerGO.GetComponent<HealthSystem>();
        if (healthSystem != null && healthBarP1 != null)
        {
            healthBarP1.Setup(healthSystem);
        }

        // 4. CONECTA A CÂMERA
        if (mainCameraFollow != null) // <-- NOVO
        {
            mainCameraFollow.AssignTarget(playerGO.transform); // <-- NOVO
        }

        // --- 5. CONECTA O UPGRADE MANAGER --- // <-- NOVO
        if (UpgradeManager.Instance != null)
        {
            // Pega o objeto visual do joystick (se ele existir)
            GameObject joyVisual = (joystickP1 != null) ? joystickP1.gameObject : null;

            // Registra o jogador e seus controles
            UpgradeManager.Instance.RegisterPlayer(playerGO, moveScript, joyVisual);
        }
        else
        {
            Debug.LogWarning("PlayerSpawner: UpgradeManager.Instance não encontrado. A loja não funcionará.");
        }        
    }

    private void SpawnMultiplayer()
    {
        // --- Spawna e Configura P1 ---
        if (player1Prefab != null && player1SpawnPoint != null)
        {
            GameObject playerGO_P1 = Instantiate(player1Prefab, player1SpawnPoint.position, player1SpawnPoint.rotation);
            
            // Conecta Joystick P1
            JoystickMove moveScriptP1 = playerGO_P1.GetComponent<JoystickMove>();
            if (moveScriptP1 != null && joystickP1 != null)
            {
                moveScriptP1.AssignJoystick(joystickP1);
            }

            // Conecta Barra de Vida P1
            HealthSystem healthSystemP1 = playerGO_P1.GetComponent<HealthSystem>();
            if (healthSystemP1 != null && healthBarP1 != null)
            {
                healthBarP1.Setup(healthSystemP1);
            }

            // 4. CONECTA A CÂMERA
            if (mainCameraFollow != null) // <-- NOVO
            {
                mainCameraFollow.AssignTarget(playerGO_P1.transform); // <-- NOVO
            }

            // --- 5. CONECTA O UPGRADE MANAGER --- // <-- NOVO
            if (UpgradeManager.Instance != null)
            {
                // Pega o objeto visual do joystick (se ele existir)
                GameObject joyVisual = (joystickP1 != null) ? joystickP1.gameObject : null;

                // Registra o jogador e seus controles
                UpgradeManager.Instance.RegisterPlayer(playerGO_P1, moveScriptP1, joyVisual);
            }
            else
            {
                Debug.LogWarning("PlayerSpawner: UpgradeManager.Instance não encontrado. A loja não funcionará.");
            }      
        }

        // --- Spawna e Configura P2 ---
        if (player2Prefab != null && player2SpawnPoint != null)
        {
            GameObject playerGO_P2 = Instantiate(player2Prefab, player2SpawnPoint.position, player2SpawnPoint.rotation);
            
            // Conecta Joystick P2
            JoystickMove moveScriptP2 = playerGO_P2.GetComponent<JoystickMove>();
            if (moveScriptP2 != null && joystickP2 != null)
            {
                // Se o joystickP2 não for definido, P2 não se move pelo joystick
                moveScriptP2.AssignJoystick(joystickP2);
            }

            // Conecta Barra de Vida P2
            HealthSystem healthSystemP2 = playerGO_P2.GetComponent<HealthSystem>();
            if (healthSystemP2 != null && healthBarP2 != null)
            {
                // Se a healthBarP2 não for definida, P2 não terá UI de vida
                healthBarP2.Setup(healthSystemP2);
            }
        }
    }
}
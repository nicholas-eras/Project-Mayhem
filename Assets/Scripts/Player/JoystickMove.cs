using UnityEngine;

public class JoystickMove : MonoBehaviour
{
    // 1. MODIFICADO: De 'public' para 'private'
    // Não precisa mais ser público, pois o Spawner vai injetar.
    private Joystick movementJoystick; 

    private PlayerController playerController;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("PlayerController não encontrado. O joystick não funcionará.", this);
        }
        
        // Não procuramos mais o joystick aqui, esperamos o Spawner nos entregar.
    }

    // --- NOVO MÉTODO PÚBLICO ---
    /// <summary>
    /// Chamado pelo PlayerSpawner para entregar a referência do Joystick.
    /// </summary>
    public void AssignJoystick(Joystick joy)
    {
        movementJoystick = joy;
    }

    private void FixedUpdate()
    {
        // 3. ADICIONE UMA VERIFICAÇÃO DE NULO
        // Essencial! O script pode rodar antes do joystick ser assignado.
        if (movementJoystick == null)
        {
            return; // Pula este frame
        }
        
        // 1. Verifique o status da loja
        if (UpgradeManager.IsShopOpen)
        {
            if (playerController != null)
            {
                playerController.SetExternalMovementInput(Vector2.zero);
            }
            return;
        }

        // 2. Lógica normal de envio (agora segura)
        Vector2 direction = new Vector2(movementJoystick.Direction.x, movementJoystick.Direction.y);

        if (playerController != null)
        {
            playerController.SetExternalMovementInput(direction);
        }
    }
}
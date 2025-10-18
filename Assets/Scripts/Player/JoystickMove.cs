using UnityEngine;

public class JoystickMove : MonoBehaviour
{
    public Joystick movementJoystick;

    private PlayerController playerController; 

    private void Start()
    {
        playerController = GetComponent<PlayerController>(); 

        if (playerController == null)
        {
            Debug.LogError("PlayerController não encontrado. O joystick não funcionará.", this);
        }
    }

// NO JoystickMove.cs

private void FixedUpdate()
{
    // 1. Verifique o status da loja
    if (UpgradeManager.IsShopOpen)
    {
        // Se a loja está aberta, force o input a ser zero
        if (playerController != null)
        {
             playerController.SetExternalMovementInput(Vector2.zero);
        }
        return; 
    }
    
    // 2. Lógica normal de envio
    Vector2 direction = new Vector2(movementJoystick.Direction.x, movementJoystick.Direction.y);

    if (playerController != null)
    {
        playerController.SetExternalMovementInput(direction);
        
        
    }
}
}
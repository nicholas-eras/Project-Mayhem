using UnityEngine;

// Enum para definir os modos de jogo
public enum GameMode
{
    SinglePlayer,
    Multiplayer
}

public class GameModeManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static GameModeManager Instance { get; private set; }

    public GameMode CurrentMode { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // <-- A MÁGICA!
        }
    }
    // --- Fim do Singleton ---

    // Método para ser chamado pelos seus botões de UI
    public void SetGameMode(GameMode newMode)
    {
        CurrentMode = newMode;
    }

    // Você pode usar int se preferir, para ligar no Inspector
    // 0 = SinglePlayer, 1 = Multiplayer
    public void SetGameModeFromInt(int modeIndex)
    {
        if (modeIndex == 0)
        {
            SetGameMode(GameMode.SinglePlayer);
        }
        else if (modeIndex == 1)
        {
            SetGameMode(GameMode.Multiplayer);
        }
    }
}
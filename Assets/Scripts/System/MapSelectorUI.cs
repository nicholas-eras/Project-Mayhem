using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; 
using Unity.Netcode;                

public class MapSelectorUI : MonoBehaviour
{
    [SerializeField] private Image mapImage;
    [SerializeField] private TextMeshProUGUI mapNameText;
    [SerializeField] private Button selectButton;

    private MapData currentMapData;

    public void Setup(MapData data)
    {
        currentMapData = data;
        mapImage.sprite = data.mapSprite;
        mapNameText.text = data.mapName;
        
        selectButton.onClick.RemoveAllListeners(); 
        selectButton.onClick.AddListener(OnMapSelected);
    }

    private void OnMapSelected()
    {
        // ... (verificações de currentMapData) ...
        PlayerPrefs.SetString("SelectedGameScenePath", currentMapData.gameScenePath);

        GameMode mode = GameMode.SinglePlayer;
        if (GameModeManager.Instance != null) mode = GameModeManager.Instance.CurrentMode;

        if (mode == GameMode.Multiplayer)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                // --- AQUI ESTÁ A MUDANÇA ---
                // 1. Pede ao LobbyManager para salvar os dados ANTES de sair da cena
                if (LobbyManager.Instance != null)
                {
                    LobbyManager.Instance.CacheLobbyDataForSceneLoad();
                }
                else
                {
                    Debug.LogError("LobbyManager.Instance não encontrado! Não foi possível salvar os dados do lobby.");
                    // Considerar não carregar a cena se isso for crítico
                }
                // --- FIM DA MUDANÇA ---

                // 2. Carrega a cena para todos (como antes)
                NetworkManager.Singleton.SceneManager.LoadScene(currentMapData.gameScenePath, LoadSceneMode.Single);
            }
        }
        else // Single Player
        {
            // --- ADICIONAL SINGLE PLAYER ---
            // Simula o salvamento de dados para o jogador 1
            int skinId = PlayerPrefs.GetInt("SlotSkinID_0", 0); // Pega a skin do Slot 0
            LobbyData.SlotStates = new SlotState[] { SlotState.Human, SlotState.Empty, SlotState.Empty, SlotState.Empty };
            LobbyData.SkinIDs = new int[] { skinId, 0, 0, 0 };
            // --- FIM ADICIONAL ---
            SceneManager.LoadScene(currentMapData.gameScenePath);
        }
    }
    
}
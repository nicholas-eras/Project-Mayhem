using UnityEngine;
using TMPro;
using UnityEngine.UI; 

public enum SlotState
{
    Empty,
    Human,
    Bot
}

public class SlotUI : MonoBehaviour
{
    [Header("Referências Internas")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private SkinSelectorUI skinSelector;
    [SerializeField] private Button slotButton;

    public int SlotIndex { get; private set; } = -1;
    public SlotState CurrentState { get; private set; } = SlotState.Empty;

    public void Initialize(int index, SkinDatabase db)
    {
        SlotIndex = index;
        if (skinSelector != null)
        {
            skinSelector.Initialize(index, db);
            
            // --- MUDANÇA 1: Escutar o evento de mudança do seletor ---
            // (Isso assume que SkinSelectorUI tem um evento OnSkinChanged)
            skinSelector.OnSkinChanged += OnSkinSelectorChanged;
        }
        else Debug.LogError($"SlotUI {SlotIndex}: SkinSelectorUI não encontrado!", this);

        if (slotButton == null)
        {
            slotButton = GetComponent<Button>();
        }

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnSlotClicked);
        }
        else
        {
            Debug.LogError($"SlotUI {SlotIndex}: Button não encontrado ou não configurado!", this);
        }
    }

    /// <summary>
    /// Chamado quando este slot é clicado (pelo Host, para Bots).
    /// </summary>
    private void OnSlotClicked()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.HandleSlotClick(SlotIndex);
        }
    }
    
    // --- NOVO MÉTODO (MUDANÇA 1) ---
    /// <summary>
    /// Chamado pelo SkinSelectorUI quando o jogador local muda a skin.
    /// </summary>
    private void OnSkinSelectorChanged(int newSkinID)
    {
        // Avisa o LobbyManager (que enviará um ServerRpc)
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnPlayerChangeSkin(SlotIndex, newSkinID);
        }
    }

    // --- MUDANÇA 2: A assinatura do método mudou ---
    /// <summary>
    /// Atualiza o texto de status, a interatividade e a skin VISUAL.
    /// Chamado pelo LobbyManager sempre que a NetworkList muda.
    /// </summary>
    public void UpdateSlotDisplay(SlotState newState, string playerName, bool isLocalPlayerControl, int currentSkinID)
    {
        CurrentState = newState;
        string displayText = $"Slot {SlotIndex + 1}: ";

        switch (newState)
        {
            case SlotState.Empty:
                displayText += "[Vazio]";
                break;
            case SlotState.Human:
                displayText += playerName;
                if (isLocalPlayerControl) displayText += " (Você)";
                break;
            case SlotState.Bot:
                displayText += "[BOT]";
                break;
        }

        if (statusText != null)
        {
            statusText.text = displayText;
        }

        if (skinSelector != null)
        {
            // Ativa/Desativa o carrossel (controle)
            skinSelector.SetInteractable(isLocalPlayerControl);
            
            // Esconde/Mostra o seletor
            skinSelector.gameObject.SetActive(newState != SlotState.Empty);
            
            // --- MUDANÇA 2 (Continuação): Força a skin visual correta ---
            // (Isso assume que SkinSelectorUI tem um método SetSkin)
            skinSelector.SetSkin(currentSkinID);
        }

        // --- MUDANÇA 3: Corrigido para usar .IsHost ---
        bool canBeToggledByHost = (LobbyManager.Instance != null && 
                                   LobbyManager.Instance.IsHost && // <-- CORRIGIDO
                                   SlotIndex != 0 && 
                                   newState != SlotState.Human);
                                   
        if (slotButton != null)
        {
            slotButton.interactable = canBeToggledByHost;
        }
    }

    /// <summary>
    /// Retorna o ID da skin selecionada neste slot.
    /// </summary>
    public int GetSelectedSkinID()
    {
        return skinSelector != null ? skinSelector.GetCurrentSkinID() : 0;
    }
    
    // --- NOVO: Limpa o evento ao ser destruído ---
    private void OnDestroy()
    {
        if (skinSelector != null)
        {
            skinSelector.OnSkinChanged -= OnSkinSelectorChanged;
        }
    }
}
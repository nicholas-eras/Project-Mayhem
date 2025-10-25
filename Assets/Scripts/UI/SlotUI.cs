using UnityEngine;
using TMPro;
using UnityEngine.UI; // <-- Adicione para Button
// Enum para definir o estado de um slot no lobby
public enum SlotState
{
    Empty,    // Ninguém no slot
    Human,    // Um jogador humano (Host ou Cliente)
    Bot       // Controlado pela IA
}

public class SlotUI : MonoBehaviour
{
    [Header("Referências Internas")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private SkinSelectorUI skinSelector;
    // (Adicione aqui um Button se quiser que o Host clique para adicionar/remover Bot)
    [SerializeField] private Button slotButton; // <-- NOVA REFERÊNCIA

    public int SlotIndex { get; private set; } = -1;
    public SlotState CurrentState { get; private set; } = SlotState.Empty;

    public void Initialize(int index, SkinDatabase db)
    {
        SlotIndex = index;
        if (skinSelector != null) skinSelector.Initialize(index, db);
        else Debug.LogError($"SlotUI {SlotIndex}: SkinSelectorUI não encontrado!", this);

        // --- LIGA O BOTÃO ---
        if (slotButton == null)
        {
            // Tenta pegar o botão no mesmo objeto se não foi arrastado
            slotButton = GetComponent<Button>();
        }

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners(); // Limpa listeners antigos
            slotButton.onClick.AddListener(OnSlotClicked); // Adiciona o novo listener
        }
        else
        {
            Debug.LogError($"SlotUI {SlotIndex}: Button não encontrado ou não configurado!", this);
        }
        // --- FIM ---
    }
    
    /// <summary>
    /// Chamado quando este slot é clicado.
    /// </summary>
    private void OnSlotClicked() // <-- NOVO MÉTODO
    {
        // Avisa o LobbyManager que este slot (pelo índice) foi clicado
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.HandleSlotClick(SlotIndex);
        }
    }

    /// <summary>
    /// Atualiza o texto de status e a interatividade do carrossel.
    /// </summary>
    public void UpdateSlotDisplay(SlotState newState, string playerName, bool isLocalPlayerControl)
    {
        CurrentState = newState;
        string displayText = $"Slot {SlotIndex + 1}: "; // Mostra 1-4 em vez de 0-3

        switch (newState)
        {
            case SlotState.Empty:
                displayText += "[Vazio]";
                break;
            case SlotState.Human:
                displayText += playerName; // Ex: "JogadorHost", "Amigo123"
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

        // Ativa/Desativa o carrossel baseado em quem controla
        if (skinSelector != null)
        {
            skinSelector.SetInteractable(isLocalPlayerControl);
            skinSelector.gameObject.SetActive(newState != SlotState.Empty); // Esconde se vazio
        }

        // --- CONTROLA INTERATIVIDADE DO BOTÃO --- // <-- NOVO
        // O slot SÓ é clicável pelo Host E se NÃO for o slot do próprio Host
        // E se NÃO for um slot já ocupado por outro humano (no futuro)
        bool canBeToggledByHost = (LobbyManager.Instance != null && LobbyManager.Instance.IsLocalPlayerHost() && SlotIndex != 0 && newState != SlotState.Human);
        if (slotButton != null)
        {
            slotButton.interactable = canBeToggledByHost;
        }
        // --- FIM ---
    }

    /// <summary>
    /// Retorna o ID da skin selecionada neste slot.
    /// </summary>
    public int GetSelectedSkinID()
    {
        return skinSelector != null ? skinSelector.GetCurrentSkinID() : 0;
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System; // <-- Adicionado para 'Action'

public class SkinSelectorUI : MonoBehaviour
{
    // --- NOVO EVENTO ---
    // Avisa o SlotUI quando o jogador local (que controla este slot) muda a skin
    public event Action<int> OnSkinChanged; 
    // --- FIM ---

    private SkinDatabase skinDatabase;

    [Header("Referências da UI")]
    [SerializeField] private Image skinPreviewImage;
    [SerializeField] private TextMeshProUGUI skinNameText;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    public int SlotIndex { get; private set; } = -1;
    private List<SkinData> availableSkins;
    private int currentSkinIndex = 0;

    // --- PlayerPrefs REMOVIDO ---
    // A NetworkList do LobbyManager é agora a fonte da verdade.

    public void Initialize(int index, SkinDatabase db)
    {
        SlotIndex = index;
        skinDatabase = db;

        if (skinDatabase == null || skinDatabase.skins == null || skinDatabase.skins.Count == 0)
        {
            Debug.LogError($"SkinSelectorUI (Slot {SlotIndex}): SkinDatabase inválido!", this);
            gameObject.SetActive(false);
            return;
        }

        availableSkins = skinDatabase.skins;

        // --- LÓGICA DO PlayerPrefs REMOVIDA ---
        currentSkinIndex = 0; // Sempre começa no 0. A rede corrigirá se necessário.

        previousButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();
        previousButton.onClick.AddListener(ShowPreviousSkin);
        nextButton.onClick.AddListener(ShowNextSkin);

        UpdateDisplay();
    }

    public void SetInteractable(bool interactable)
    {
        previousButton.interactable = interactable;
        nextButton.interactable = interactable;
    }

    // --- NOVO MÉTODO ---
    /// <summary>
    /// Chamado externamente (pelo SlotUI) para FORÇAR a exibição de uma skin.
    /// Isso é usado para sincronizar a UI com o estado da rede.
    /// NÃO dispara o evento OnSkinChanged (para evitar loops).
    /// </summary>
    public void SetSkin(int skinID)
    {
        if (availableSkins == null || availableSkins.Count == 0) return;

        // Valida o skinID
        if (skinID < 0 || skinID >= availableSkins.Count)
        {
            skinID = 0;
        }

        currentSkinIndex = skinID;
        UpdateDisplay();
    }
    // --- FIM NOVO MÉTODO ---

    public void ShowPreviousSkin()
    {
        if (availableSkins == null || availableSkins.Count == 0) return;
        currentSkinIndex = (currentSkinIndex - 1 + availableSkins.Count) % availableSkins.Count;
        
        UpdateDisplay();
        
        // --- ALTERADO ---
        // Em vez de salvar no PlayerPrefs, disparamos o evento
        OnSkinChanged?.Invoke(currentSkinIndex);
    }

    public void ShowNextSkin()
    {
        if (availableSkins == null || availableSkins.Count == 0) return;
        currentSkinIndex = (currentSkinIndex + 1) % availableSkins.Count;
        
        UpdateDisplay();
        
        // --- ALTERADO ---
        // Em vez de salvar no PlayerPrefs, disparamos o evento
        OnSkinChanged?.Invoke(currentSkinIndex);
    }

    private void UpdateDisplay()
    {
        if (availableSkins == null || currentSkinIndex < 0 || currentSkinIndex >= availableSkins.Count) return;

        SkinData currentSkin = availableSkins[currentSkinIndex];

        if (skinPreviewImage != null)
        {
            skinPreviewImage.sprite = currentSkin.skinSprite;
            skinPreviewImage.preserveAspect = true;
        }
        if (skinNameText != null)
        {
            skinNameText.text = currentSkin.skinName;
        }
    }

    public int GetCurrentSkinID()
    {
        return currentSkinIndex;
    }
}
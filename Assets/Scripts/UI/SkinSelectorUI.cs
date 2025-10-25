using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SkinSelectorUI : MonoBehaviour
{
    // Removido SerializeField para SkinDatabase, será passado pelo SlotUI
    private SkinDatabase skinDatabase;

    [Header("Referências da UI")]
    [SerializeField] private Image skinPreviewImage;
    [SerializeField] private TextMeshProUGUI skinNameText;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    // --- NOVAS VARIÁVEIS ---
    public int SlotIndex { get; private set; } = -1; // Índice do slot (0-3)
    private List<SkinData> availableSkins;
    private int currentSkinIndex = 0;
    // --- FIM NOVAS VARIÁVEIS ---

    // Chave base para o PlayerPrefs
    private const string SKIN_PREF_KEY_BASE = "SlotSkinID_"; // Ex: SlotSkinID_0, SlotSkinID_1

    // Não usa mais Start(), a inicialização vem de fora
    // void Start() { ... }

    /// <summary>
    /// Chamado pelo SlotUI para configurar este seletor.
    /// </summary>
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

        // Carrega a skin salva para ESTE slot
        currentSkinIndex = PlayerPrefs.GetInt(GetPlayerPrefsKey(), 0);
        if (currentSkinIndex < 0 || currentSkinIndex >= availableSkins.Count)
        {
            currentSkinIndex = 0;
        }

        previousButton.onClick.RemoveAllListeners(); // Limpa listeners
        nextButton.onClick.RemoveAllListeners();
        previousButton.onClick.AddListener(ShowPreviousSkin);
        nextButton.onClick.AddListener(ShowNextSkin);

        UpdateDisplay();
    }

    /// <summary>
    /// Ativa ou desativa a interatividade dos botões.
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        previousButton.interactable = interactable;
        nextButton.interactable = interactable;
    }

    public void ShowPreviousSkin()
    {
        if (availableSkins == null || availableSkins.Count == 0) return;
        currentSkinIndex = (currentSkinIndex - 1 + availableSkins.Count) % availableSkins.Count; // Wrap around
        UpdateDisplay();
        SaveSelection();
    }

    public void ShowNextSkin()
    {
        if (availableSkins == null || availableSkins.Count == 0) return;
        currentSkinIndex = (currentSkinIndex + 1) % availableSkins.Count; // Wrap around
        UpdateDisplay();
        SaveSelection();
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

    /// <summary>
    /// Gera a chave única do PlayerPrefs para este slot.
    /// </summary>
    private string GetPlayerPrefsKey()
    {
        return SKIN_PREF_KEY_BASE + SlotIndex;
    }

    private void SaveSelection()
    {
        if (SlotIndex == -1) return; // Não salva se não foi inicializado

        PlayerPrefs.SetInt(GetPlayerPrefsKey(), currentSkinIndex);
        // PlayerPrefs.Save(); // Opcional: Forçar salvamento (pode causar pequeno lag)

        // --- IMPORTANTE PARA REDE (FUTURO) ---
        // Se este slot for controlado pelo jogador local,
        // ele precisará notificar o servidor sobre a mudança.
        // Ex: FindObjectOfType<LobbyManager>().NotifySkinChange(SlotIndex, currentSkinIndex);
    }

    /// <summary>
    /// Retorna o ID da skin atualmente selecionada neste carrossel.
    /// </summary>
    public int GetCurrentSkinID()
    {
        return currentSkinIndex;
    }
}
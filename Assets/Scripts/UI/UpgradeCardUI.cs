using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button purchaseButton;

    // Mude de [private UpgradeData currentUpgrade;] para:
    private UpgradeData _currentUpgrade;

    // TORNA A INFORMAÇÃO ACESSÍVEL PARA LEITURA EXTERNA
    public UpgradeData CurrentUpgrade => _currentUpgrade;
    private UpgradeManager manager;

    public void Setup(UpgradeData upgrade, UpgradeManager upgradeManager, int dynamicCost)    {
        _currentUpgrade = upgrade;
        manager = upgradeManager; 

        iconImage.sprite = upgrade.icon;
        nameText.text = upgrade.upgradeName;
        descriptionText.text = upgrade.description;
        
        // NOVO: Usa o custo dinâmico passado pelo manager
        costText.text = "Custo: " + dynamicCost.ToString(); 

        // Adiciona o listener ao botão
        purchaseButton.onClick.AddListener(OnPurchase);
    }

    private void OnPurchase()
    {
        manager.PurchaseUpgrade(_currentUpgrade, this); // <--- CORRIGE o erro de referência na linha 37    }
    }
    public void UpdateCostDisplay(int newCost)
    {
        costText.text = "Custo: " + newCost.ToString();
    }
}

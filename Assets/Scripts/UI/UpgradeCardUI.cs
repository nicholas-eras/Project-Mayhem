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

    private UpgradeData currentUpgrade;
    private UpgradeManager manager;

    public void Setup(UpgradeData upgrade, UpgradeManager upgradeManager)
    {
        currentUpgrade = upgrade;
        manager = upgradeManager;

        iconImage.sprite = upgrade.icon;
        nameText.text = upgrade.upgradeName;
        descriptionText.text = upgrade.description;
        costText.text = "Custo: " + upgrade.cost.ToString();

        // Adiciona o listener ao botão
        purchaseButton.onClick.AddListener(OnPurchase);
    }

    private void OnPurchase()
    {
        manager.PurchaseUpgrade(currentUpgrade, this);
    }
}

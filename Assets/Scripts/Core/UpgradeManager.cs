using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Referências")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject upgradeCardPrefab;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private PlayerWeaponManager playerWeaponManager;

    [Header("Configuração")]
    [Tooltip("Todos os upgrades possíveis no jogo.")]
    [SerializeField] private List<UpgradeData> allUpgrades;
    [SerializeField] private int optionsToShow = 4;

    // Evento para avisar ao WaveManager que a loja fechou e o jogo pode continuar.
    public static UnityAction OnShopClosed;

    void Awake()
    {
        Instance = this;
    }

    public void ShowUpgradeScreen()
    {
        // Pausa o jogo
        Time.timeScale = 0f;
        upgradePanel.SetActive(true);

        // Limpa as cartas antigas
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        // --- NOVA LÓGICA DE ESCOLHA ---
        List<UpgradeData> upgradesPool = new List<UpgradeData>(allUpgrades);

        if (upgradesPool.Count == 0) return; // Nenhuma opção disponível.

        List<UpgradeData> chosenUpgrades = new List<UpgradeData>();

        // Se o número de upgrades for menor que o que queremos mostrar, repetimos.
        for (int i = 0; i < optionsToShow; i++)
        {
            // Garante que a lista não está vazia (nunca deve estar se o count > 0)
            if (upgradesPool.Count > 0)
            {
                // Escolhe um upgrade aleatório do pool (sem remover para permitir repetição)
                UpgradeData randomUpgrade = upgradesPool[Random.Range(0, upgradesPool.Count)];
                chosenUpgrades.Add(randomUpgrade);
            }
            else
            {
                // Caso de falha (embora improvável com esta lógica)
                break;
            }
        }
        // --- FIM DA NOVA LÓGICA DE ESCOLHA ---
        // Cria as novas cartas
        foreach (var upgrade in chosenUpgrades)
        {
            GameObject cardInstance = Instantiate(upgradeCardPrefab, cardContainer);
            cardInstance.GetComponent<UpgradeCardUI>().Setup(upgrade, this);
        }
    }

    public void PurchaseUpgrade(UpgradeData upgrade, UpgradeCardUI cardUI)    {
        // Tenta gastar o dinheiro
        if (playerWallet.SpendMoney(upgrade.cost))
        {
            // Se conseguir, aplica o upgrade
            ApplyUpgrade(upgrade);
            Destroy(cardUI.gameObject);
        }
        else
        {
            // Feedback de que não tem dinheiro (som, etc.)
            Debug.Log("Dinheiro insuficiente!");
            // AudioManager.Instance.PlaySFX("Purchase_Fail");
        }
    }

    private void ApplyUpgrade(UpgradeData upgrade)
    {
        switch (upgrade.type)
        {
            case UpgradeType.AddNewWeapon:
                if (upgrade.weaponPrefab != null)
                {
                    playerWeaponManager.AddWeapon(upgrade.weaponPrefab);
                }
                break;
            // Adicionar outros tipos de upgrade aqui no futuro
        }
    }

    // Função para o botão "Pular"
    public void CloseShop()
    {
        Time.timeScale = 1f; // Despausa o jogo
        upgradePanel.SetActive(false);
        OnShopClosed?.Invoke(); // Avisa que o jogo pode continuar
    }
}

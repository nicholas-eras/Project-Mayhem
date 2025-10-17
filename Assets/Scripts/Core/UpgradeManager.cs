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
    [SerializeField] private PlayerController playerController;
    [SerializeField] private HealthSystem playerHealthSystem;
    [SerializeField] private PlayerWeaponManager playerWeaponManager;

    [Header("Configuração")]
    [Tooltip("Todos os upgrades possíveis no jogo.")]
    [SerializeField] private List<UpgradeData> allUpgrades;
    [SerializeField] private int optionsToShow = 4;

    // Evento para avisar ao WaveManager que a loja fechou e o jogo pode continuar.
    public static UnityAction OnShopClosed;

    private Dictionary<UpgradeType, int> upgradeLevels = new Dictionary<UpgradeType, int>();
    
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
            int dynamicCost = GetCurrentCost(upgrade); // <--- Calcula o custo
            
            GameObject cardInstance = Instantiate(upgradeCardPrefab, cardContainer);
            
            // NOVO: Você precisará de uma nova função Setup para passar o custo, 
            // OU apenas atualizar a função Setup existente no UpgradeCardUI para chamar GetCurrentCost.
            
            UpgradeCardUI cardUI = cardInstance.GetComponent<UpgradeCardUI>();
            cardUI.Setup(upgrade, this, dynamicCost); // <--- Chama o Setup com o novo custo
        }
    }

    public void PurchaseUpgrade(UpgradeData upgrade, UpgradeCardUI cardUI)
    {
        int requiredCost = GetCurrentCost(upgrade); // <--- Usa o custo dinâmico!

        // Tenta gastar o dinheiro
        if (playerWallet.SpendMoney(requiredCost)) // <--- Usa o custo dinâmico!
        {
            // 1. Aplica o upgrade
            ApplyUpgrade(upgrade);

            // 2. CRUCIAL: Aumenta o nível para o próximo cálculo de custo
            upgradeLevels[upgrade.type]++;
            UpdateAllVisibleCardCosts(upgrade.type);
            
            Destroy(cardUI.gameObject);
        }
        else
        {
            // Feedback de que não tem dinheiro (som, etc.)
            Debug.Log("Dinheiro insuficiente! Custo: " + requiredCost);
        }
    }
    
    public int GetCurrentCost(UpgradeData upgrade)
    {
        // 1. Obtém o nível atual (ou 0 se for a primeira vez)
        if (!upgradeLevels.ContainsKey(upgrade.type))
        {
            upgradeLevels.Add(upgrade.type, 0);
        }
        int currentLevel = upgradeLevels[upgrade.type];

        // 2. Calcula o custo progressivo
        // Custo = CustoBase * (1 + (AumentoPorNível * NívelAtual))
        float costMultiplier = 1f + (upgrade.priceIncreasePerLevel * currentLevel);
        
        // Arredonda para o inteiro mais próximo
        return Mathf.RoundToInt(upgrade.baseCost * costMultiplier);
    }

    private void ApplyUpgrade(UpgradeData upgrade)
    {
        // Verificações de segurança
        if (playerHealthSystem == null || playerController == null || playerWeaponManager == null)
        {
            Debug.LogError("Referências de Stats/Health/Weapons incompletas no UpgradeManager! Verifique o Inspector.");
            return;
        }

        switch (upgrade.type)
        {
            case UpgradeType.AddNewWeapon:
                if (upgrade.weaponPrefab != null)
                {
                    playerWeaponManager.AddWeapon(upgrade.weaponPrefab);
                }
                break;

            case UpgradeType.IncreaseMoveSpeed:
                playerController.IncreaseSpeedMultiplier(upgrade.value);
                break;

            case UpgradeType.IncreaseHealth:
                playerHealthSystem.IncreaseMaxHealth(upgrade.value);
                break;

            case UpgradeType.IncreaseDamage:
                playerWeaponManager.IncreaseDamageMultiplier(upgrade.value);
                break;

            case UpgradeType.IncreaseRegen:
                playerHealthSystem.IncreaseRegenRate(upgrade.value);
                break;

            case UpgradeType.IncreaseFireRate:
                playerWeaponManager.IncreaseFireRateMultiplier(upgrade.value);
                break;

            case UpgradeType.IncreaseRange:
                playerWeaponManager.IncreaseRangeMultiplier(upgrade.value);
                break;

            case UpgradeType.IncreaseInvulnerabilityTime:
                playerHealthSystem.IncreaseInvulnerabilityTime(upgrade.value);
                break;

            default:
                Debug.LogWarning("Tipo de Upgrade não implementado: " + upgrade.type);
                break;
        }
    }

    // Função para o botão "Pular"
    public void CloseShop()
    {
        Time.timeScale = 1f; // Despausa o jogo
        upgradePanel.SetActive(false);
        OnShopClosed?.Invoke(); // Avisa que o jogo pode continuar
    }

    private void UpdateAllVisibleCardCosts(UpgradeType purchasedType)
    {
        UpgradeCardUI[] activeCards = cardContainer.GetComponentsInChildren<UpgradeCardUI>();

        foreach (UpgradeCardUI card in activeCards)
        {
            // CORREÇÃO: Acessa a informação de upgrade via a nova propriedade pública 'CurrentUpgrade'
            // Isso corrige os erros CS1061 na linha 194 e 197.
            if (card.CurrentUpgrade.type == purchasedType)
            {
                // 1. Recalcula o novo custo para este upgrade.
                int newCost = GetCurrentCost(card.CurrentUpgrade); 

                // 2. Atualiza o display da carta.
                card.UpdateCostDisplay(newCost);
            }
        }
    }
}

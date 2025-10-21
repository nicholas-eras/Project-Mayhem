using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UpgradeManager : MonoBehaviour
{
    // --- FIX 1: ADD THE STATIC INSTANCE PROPERTY ---
    // This property will hold the single instance of the UpgradeManager.
    public static UpgradeManager Instance { get; private set; }

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

    public event UnityAction OnShopClosed;

    private Dictionary<UpgradeData, int> upgradeLevelsByAsset = new Dictionary<UpgradeData, int>();
    [SerializeField] private PlayerStatusUI playerStatusUI;

    // NOVO: Referência ao script de input do joystick (JoystickMove ou PlayerController se ele for o único input)
    [Header("Input Control")]
    [Tooltip("O script de Joystick/Movimento para desativar durante a loja.")]
    [SerializeField] private JoystickMove movementInputScript; // <-- MUDAR O TIPO AQUI
    [SerializeField] private GameObject joystickVisual;
    public static bool IsShopOpen { get; private set; } = false; // NOVO: Flag de estado da loja

    // --- FIX 2: ADD THE AWAKE METHOD FOR SINGLETON LOGIC ---
    // This method runs before any Start() methods and ensures
    // that 'Instance' is set correctly.
    private void Awake()
    {
        // Standard singleton pattern to ensure only one instance exists.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void ShowUpgradeScreen()
    {
        Time.timeScale = 0f;
        upgradePanel.SetActive(true);
        IsShopOpen = true; // Define a flag

        // AÇÃO 1: DESATIVA O INPUT DE MOVIMENTO
        if (movementInputScript != null)
        {
            movementInputScript.enabled = false;
        }
        if (joystickVisual != null)
        {
            joystickVisual.SetActive(false); // DEVE DESATIVAR O VISUAL
        }
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        List<UpgradeData> upgradesPool = new List<UpgradeData>(allUpgrades);
        if (upgradesPool.Count == 0) return;

        List<UpgradeData> chosenUpgrades = new List<UpgradeData>();
        for (int i = 0; i < optionsToShow; i++)
        {
            if (upgradesPool.Count > 0)
            {
                UpgradeData randomUpgrade = upgradesPool[Random.Range(0, upgradesPool.Count)];
                chosenUpgrades.Add(randomUpgrade);
            }
            else
            {
                break;
            }
        }

        foreach (var upgrade in chosenUpgrades)
        {
            int dynamicCost = GetCurrentCost(upgrade);
            GameObject cardInstance = Instantiate(upgradeCardPrefab, cardContainer);
            UpgradeCardUI cardUI = cardInstance.GetComponent<UpgradeCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(upgrade, this, dynamicCost);
            }
        }

        // NOVO: Configura e atualiza o painel de status do jogador
        if (playerStatusUI != null)
        {
            playerStatusUI.Setup(playerController, playerHealthSystem, playerWeaponManager);
        }
    }

    public void PurchaseUpgrade(UpgradeData upgrade, UpgradeCardUI cardUI)
    {
        // 1. Calcula o custo dinâmico DESSA carta.
        int requiredCost = GetCurrentCost(upgrade); 

        if (playerWallet.SpendMoney(requiredCost))
        {
            // --- Lógica de Compra e Aplicação ---
            
            ApplyUpgrade(upgrade);
            
            // 2. CRUCIAL: AUMENTA O NÍVEL DO ASSET ESPECÍFICO
            
            // Tenta obter o nível atual. Se não existir, retorna 0.
            int currentLevel = 0;
            upgradeLevelsByAsset.TryGetValue(upgrade, out currentLevel); 

            // Incrementa o nível e salva na chave do Asset.
            upgradeLevelsByAsset[upgrade] = currentLevel + 1;
            
            // Opcional: Se você precisa rastrear o número total de upgrades por tipo:
            // if (!upgradeLevelsByType.ContainsKey(upgrade.type)) { upgradeLevelsByType.Add(upgrade.type, 0); }
            // upgradeLevelsByType[upgrade.type]++;

            // 3. Atualiza o painel de status do jogador
            if (playerStatusUI != null)
            {
                playerStatusUI.UpdateDisplay();
            }
            
            // 4. Atualiza o custo de OUTRAS cartas que usam o MESMO TIPO (Ex: Dano Global)
            UpdateAllVisibleCardCosts(upgrade.type);
            
            // 5. Destrói a carta comprada
            Destroy(cardUI.gameObject);
        }
        else
        {
            Debug.Log("Dinheiro insuficiente! Custo: " + requiredCost);
            // AudioManager.Instance.PlaySFX("Purchase_Fail");
        }
    }
    public int GetCurrentCost(UpgradeData upgrade)
    {
        // 1. Obtém o nível atual (ou 0 se for a primeira vez)
        int currentLevel = 0;
        upgradeLevelsByAsset.TryGetValue(upgrade, out currentLevel); // Pega o nível pelo Asset

        // 2. Calcula o custo progressivo
        float costMultiplier = 1f + (upgrade.priceIncreasePerLevel * currentLevel);
        
        // Arredonda para o inteiro mais próximo
        return Mathf.RoundToInt(upgrade.baseCost * costMultiplier);
    }

    private void ApplyUpgrade(UpgradeData upgrade)
    {
        if (playerHealthSystem == null || playerController == null || playerWeaponManager == null)
        {
            Debug.LogError("Referências de Player/Health/Weapons incompletas no UpgradeManager! Verifique o Inspector.");
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
    
    public void CloseShop()
    {
        if (movementInputScript != null)
        {
            movementInputScript.enabled = true;
        }
        IsShopOpen = false; // Reseta a flag
        if (joystickVisual != null)
        {
            joystickVisual.SetActive(true); // <--- ESTA É A LINHA QUE FALTAVA
        }       
        Time.timeScale = 1f;
        upgradePanel.SetActive(false);
        OnShopClosed?.Invoke();
    }

    private void UpdateAllVisibleCardCosts(UpgradeType purchasedType)
    {
        UpgradeCardUI[] activeCards = cardContainer.GetComponentsInChildren<UpgradeCardUI>();
        foreach (UpgradeCardUI card in activeCards)
        {
            if (card.CurrentUpgrade != null && card.CurrentUpgrade.type == purchasedType)
            {
                int newCost = GetCurrentCost(card.CurrentUpgrade);
                card.UpdateCostDisplay(newCost);
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UpgradeManager : MonoBehaviour
{
    // --- FIX 1: ADD THE STATIC INSTANCE PROPERTY ---
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
    
    // --- NOVO: Custo para atualizar (reroll) as opções da loja ---
    [SerializeField] private int rerollCost = 50; 

    public event UnityAction OnShopClosed;

    private Dictionary<UpgradeData, int> upgradeLevelsByAsset = new Dictionary<UpgradeData, int>();
    [SerializeField] private PlayerStatusUI playerStatusUI;

    [Header("Input Control")]
    [Tooltip("O script de Joystick/Movimento para desativar durante a loja.")]
    [SerializeField] private JoystickMove movementInputScript;
    [SerializeField] private GameObject joystickVisual;
    public static bool IsShopOpen { get; private set; } = false;

    // --- FIX 2: ADD THE AWAKE METHOD FOR SINGLETON LOGIC ---
    private void Awake()
    {
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
        IsShopOpen = true;
        
        // Configura e atualiza o painel de status do jogador
        if (playerStatusUI != null)
        {
            playerStatusUI.Setup(playerController, playerHealthSystem, playerWeaponManager);
        }
        
        // AÇÃO 1: DESATIVA O INPUT DE MOVIMENTO
        if (movementInputScript != null)
        {
            movementInputScript.enabled = false;
        }
        if (joystickVisual != null)
        {
            joystickVisual.SetActive(false);
        }
        
        // --- MUDANÇA: A lógica de geração de cartas foi movida para um método separado ---
        GerarNovasCartas();

        
    }

    // --- NOVO: MÉTODO PÚBLICO PARA O BOTÃO DE ATUALIZAR (REROLL) ---
    public void AtualizarOpcoes()
    {
        // 1. Verifica se o jogador tem dinheiro para pagar o custo do reroll
        if (playerWallet.SpendMoney(rerollCost))
        {
            // 2. Se sim, gasta o dinheiro e gera um novo conjunto de cartas
            GerarNovasCartas();
            
            // Opcional: Tocar um som de "reroll" bem-sucedido
            // AudioManager.Instance.PlaySFX("Shop_Reroll");
        }
        else
        {
            // 3. Se não, avisa o jogador (e talvez toque um som de falha)
            Debug.Log("Dinheiro insuficiente para atualizar a loja! Custo: " + rerollCost);
            // AudioManager.Instance.PlaySFX("Purchase_Fail");
        }
    }

    public void PurchaseUpgrade(UpgradeData upgrade, UpgradeCardUI cardUI)
    {
        int requiredCost = GetCurrentCost(upgrade); 

        if (playerWallet.SpendMoney(requiredCost))
        {
            ApplyUpgrade(upgrade);
            
            int currentLevel = 0;
            upgradeLevelsByAsset.TryGetValue(upgrade, out currentLevel); 
            upgradeLevelsByAsset[upgrade] = currentLevel + 1;
            
            if (playerStatusUI != null)
            {
                playerStatusUI.UpdateDisplay();
            }
            
            UpdateAllVisibleCardCosts(upgrade.type);
            
            Destroy(cardUI.gameObject);
        }
        else
        {
            Debug.Log("Dinheiro insuficiente! Custo: " + requiredCost);
        }
    }
    
    public int GetCurrentCost(UpgradeData upgrade)
    {
        int currentLevel = 0;
        upgradeLevelsByAsset.TryGetValue(upgrade, out currentLevel); 
        float costMultiplier = 1f + (upgrade.priceIncreasePerLevel * currentLevel);
        return Mathf.RoundToInt(upgrade.baseCost * costMultiplier);
    }

    // --- NOVO: MÉTODO PRIVADO QUE CONTÉM A LÓGICA DE GERAR CARTAS ---
    // Isto é chamado tanto por ShowUpgradeScreen() quanto por AtualizarOpcoes()
    private void GerarNovasCartas()
    {
        // 1. Limpa todas as cartas que já estão no container
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Cria um "pool" de onde escolher os upgrades
        List<UpgradeData> upgradesPool = new List<UpgradeData>(allUpgrades);
        if (upgradesPool.Count == 0)
        {
            Debug.LogWarning("A lista 'allUpgrades' está vazia. Não há upgrades para mostrar.");
            return;
        }

        // 3. Escolhe os upgrades
        List<UpgradeData> chosenUpgrades = new List<UpgradeData>();
        for (int i = 0; i < optionsToShow; i++)
        {
            if (upgradesPool.Count > 0)
            {
                // Escolhe um upgrade aleatório
                int index = Random.Range(0, upgradesPool.Count);
                UpgradeData randomUpgrade = upgradesPool[index];
                
                chosenUpgrades.Add(randomUpgrade);
                
                // IMPORTANTE: Remove o upgrade escolhido do pool
                // Isso impede que o *mesmo* upgrade apareça duas vezes na *mesma* tela
                upgradesPool.RemoveAt(index); 
            }
            else
            {
                // Para o loop se o pool acabar (ex: só tem 2 upgrades e optionsToShow é 4)
                break;
            }
        }

        // 4. Instancia as cartas na UI
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
    }

    private void ApplyUpgrade(UpgradeData upgrade)
    {
        if (playerHealthSystem == null || playerController == null || playerWeaponManager == null)
        {
            Debug.LogError("Referências de Player/Health/Weapons incompletas no UpgradeManager!");
            return;
        }

        switch (upgrade.type)
        {
            case UpgradeType.AddNewWeapon:
                if (upgrade.weaponPrefab != null) { playerWeaponManager.AddWeapon(upgrade.weaponPrefab); }
                break;
            case UpgradeType.IncreaseMoveSpeed:
                playerController.IncreaseSpeedMultiplier(upgrade.value);
                break;
            case UpgradeType.IncreaseHealth:
                playerHealthSystem.IncreaseMaxHealth(upgrade.value);
                break;
            case UpgradeType.IncreaseDamage:
                playerWeaponManager.IncreaseDamage(upgrade.value);
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
        IsShopOpen = false;
        if (joystickVisual != null)
        {
            joystickVisual.SetActive(true);
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
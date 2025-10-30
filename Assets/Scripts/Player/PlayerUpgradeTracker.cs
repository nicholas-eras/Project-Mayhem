using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coloque este componente em cada Jogador e Bot.
/// Ele rastreia os níveis de upgrade e custos individualmente
/// e aplica os upgrades aos componentes locais.
/// </summary>
public class PlayerUpgradeTracker : MonoBehaviour
{
    // Dicionário INDIVIDUAL de níveis
    private Dictionary<UpgradeData, int> upgradeLevelsByAsset = new Dictionary<UpgradeData, int>();

    // Referências locais (pegas automaticamente)
    private PlayerWallet playerWallet;
    private PlayerController playerController;
    private HealthSystem playerHealthSystem;
    private PlayerWeaponManager playerWeaponManager;

    // Propriedade pública para o Bot saber quanto dinheiro tem
    public PlayerWallet Wallet => playerWallet;

    void Awake()
    {
        // Pega as referências locais
        playerWallet = GetComponent<PlayerWallet>();
        playerController = GetComponent<PlayerController>();
        playerHealthSystem = GetComponent<HealthSystem>();
        playerWeaponManager = GetComponent<PlayerWeaponManager>();

        if (playerWallet == null || playerController == null || playerHealthSystem == null || playerWeaponManager == null)
        {
            Debug.LogError($"[PlayerUpgradeTracker] Falha ao encontrar todos os componentes em {gameObject.name}!");
        }
    }

    /// <summary>
    /// Calcula o custo de um upgrade para ESTE jogador, com base no seu nível.
    /// </summary>
    public int GetCurrentCost(UpgradeData upgrade)
    {
        upgradeLevelsByAsset.TryGetValue(upgrade, out int currentLevel);
        float costMultiplier = 1f + (upgrade.priceIncreasePerLevel * currentLevel);
        return Mathf.RoundToInt(upgrade.baseCost * costMultiplier);
    }

    /// <summary>
    /// Tenta comprar um upgrade. Retorna true se foi bem-sucedido.
    /// </summary>
    public bool TryPurchaseUpgrade(UpgradeData upgrade)
    {
        int requiredCost = GetCurrentCost(upgrade);

        if (playerWallet.SpendMoney(requiredCost))
        {
            ApplyUpgrade(upgrade);
            
            // Incrementa o nível localmente
            upgradeLevelsByAsset.TryGetValue(upgrade, out int currentLevel);
            upgradeLevelsByAsset[upgrade] = currentLevel + 1;
            return true;
        }
        
        // Dinheiro insuficiente
        return false;
    }

    /// <summary>
    /// Aplica os stats do upgrade aos componentes deste GameObject.
    /// </summary>
    private void ApplyUpgrade(UpgradeData upgrade)
    {
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
}
using UnityEngine;

// Certifique-se de que seu arquivo UpgradeType.cs (ou o enum em UpgradeData.cs) está assim:
public enum UpgradeType
{
    AddNewWeapon,
    IncreaseMoveSpeed,
    IncreaseHealth,
    IncreaseDamage,               // Dano da Arma
    IncreaseRegen,                // Regeneração do HealthSystem
    IncreaseFireRate,           // Cadência da Arma
    IncreaseRange,              // Alcance da Arma
    IncreaseInvulnerabilityTime // Cooldown de Dano do HealthSystem
}

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Game/Upgrade Data")]
public class UpgradeData : ScriptableObject
{ 
    [Header("Info")]
    public UpgradeType type;
    public string upgradeName;
    [TextArea] public string description;
    public Sprite icon;
    
    [Header("Configuração de Custo")]
    public int baseCost = 50; // <--- Renomear 'cost' para 'baseCost'
    [Tooltip("Percentual de aumento de custo por nível comprado (Ex: 0.1 para +10%)")]
    public float priceIncreasePerLevel = 0.25f; // Aumenta 25% a cada compra
    [Header("Upgrade Specifics")]
    [Tooltip("Use isto se o 'Type' for AddNewWeapon.")]
    public GameObject weaponPrefab;

    [Tooltip("Use isto para upgrades de stats. Ex: 0.1 para +10% de velocidade.")]
    public float value;
}

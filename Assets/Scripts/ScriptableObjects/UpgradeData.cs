using UnityEngine;

// Enum para definir de forma clara o que cada upgrade faz.
public enum UpgradeType
{
    AddNewWeapon,
    IncreaseMoveSpeed,
    IncreaseHealth,
    IncreaseDamage // Exemplo para o futuro
}

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Game/Upgrade Data")]
public class UpgradeData : ScriptableObject
{ 
    [Header("Info")]
    public UpgradeType type;
    public string upgradeName;
    [TextArea] public string description;
    public Sprite icon;
    public int cost = 50;

    [Header("Upgrade Specifics")]
    [Tooltip("Use isto se o 'Type' for AddNewWeapon.")]
    public GameObject weaponPrefab;

    [Tooltip("Use isto para upgrades de stats. Ex: 0.1 para +10% de velocidade.")]
    public float value;
}

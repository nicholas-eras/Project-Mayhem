using UnityEngine;

// Enum para os diferentes tipos de tiro
public enum WeaponFireType
{
    Single,     // Apenas um projétil no centro
    Spread,     // Três projéteis com dispersão
    Double,     // Dois projéteis lado a lado, centrados na mira
    Constant    // Tiro contínuo (ex: Laser)
}

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName;

    [Header("Stats")]
    public float damage = 5f;
    public float fireRateInterval = 2f; // Tiros por segundo
    public float range = 15f;
    public GameObject projectilePrefab; // Usado para Single, Spread, Double

    [Header("Fire Type")] // <<<--- NOVA SEÇÃO
    public WeaponFireType fireType = WeaponFireType.Single; // Define o tipo de tiro

    [Header("Type-Specific Settings")]
    [Tooltip("Ângulo de dispersão para o tiro Spread")]
    public float spreadAngle = 30f; 

    [Tooltip("Distância de separação para os tiros Double (distância do centro)")]
    public float doubleShotSeparation = 0.5f; 

    [Tooltip("O comprimento do efeito do tiro Constant (Laser)")]
    public float laserLength = 20f; // Usado para Constant

    [Header("Audio")]
    [Tooltip("O nome do som de tiro (deve corresponder a um nome no AudioManager).")]
    public string shootSoundName;
}
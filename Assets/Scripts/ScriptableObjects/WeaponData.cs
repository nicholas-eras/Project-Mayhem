using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName;

    [Header("Stats")]
    public float damage = 5f;
    public float fireRate = 2f; // Tiros por segundo
    public float range = 15f;
    public GameObject projectilePrefab;

    [Header("Audio")] // <<<--- NOVA SEÇÃO
    [Tooltip("O nome do som de tiro (deve corresponder a um nome no AudioManager).")]
    public string shootSoundName; // <<<--- NOVA LINHA
}
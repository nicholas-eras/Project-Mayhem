using UnityEngine;

/// <summary>
/// Este script fica em um objeto em cada cena de jogo (Cyberpunk, Jungle, etc.)
/// e armazena as configurações específicas daquele mapa.
/// </summary>
public class MapDataManager : MonoBehaviour
{
    // O Singleton "Instance" permite que outros scripts
    // o encontrem facilmente (ex: MapDataManager.Instance).
    public static MapDataManager Instance { get; private set; }

    [Header("Configuração Específica do Mapa")]
    [Tooltip("Arraste o PREFAB da arma que deve ser a inicial neste mapa.")]
    public GameObject startingWeaponPrefab;
    
    // (Você pode adicionar outras coisas aqui no futuro,
    // como "Música do Mapa", "Chefe do Mapa", etc.)

    void Awake()
    {
        // Configuração do Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
}
using UnityEngine;

// Não precisa de [CreateAssetMenu] pois será criado via script.
public class MapData : ScriptableObject
{
    // O nome da pasta (ex: "Floresta")
    public string mapName; 
    
    // O nome COMPLETO da cena de jogo (ex: "Assets/Scenes/Maps/Floresta/Game.unity")
    public string gameScenePath; 
    
    // O sprite (Map.png) que será exibido no carrossel.
    public Sprite mapSprite;
}
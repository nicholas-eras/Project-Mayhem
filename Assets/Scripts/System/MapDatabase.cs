using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MapDatabase", menuName = "Game/Map Database")]
public class MapDatabase : ScriptableObject
{
    [System.Serializable]
    public class MapEntry
    {
        public string mapName;
        [Tooltip("O caminho COMPLETO para a cena do jogo (ex: Assets/Scenes/Maps/Floresta/Game.unity)")]
        public string scenePath; 
        public Sprite mapImage;
    }

    public List<MapEntry> maps = new List<MapEntry>();
}
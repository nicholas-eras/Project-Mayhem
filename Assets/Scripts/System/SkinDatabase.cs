using UnityEngine;
using System.Collections.Generic;

// Permite criar este asset no menu 'Assets > Create'
[CreateAssetMenu(fileName = "SkinDatabase", menuName = "MyGame/Skin Database")]
public class SkinDatabase : ScriptableObject
{
    // Você vai arrastar todas as suas skins/animadores para esta lista
    public List<SkinData> skins;
}
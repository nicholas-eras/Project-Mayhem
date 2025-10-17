using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Necessário para carregar a cena

public class MapSelectorUI : MonoBehaviour
{
    [SerializeField] private Image mapImage;
    [SerializeField] private TextMeshProUGUI mapNameText;
    [SerializeField] private Button selectButton;

    private MapData currentMapData;

    public void Setup(MapData data)
    {
        currentMapData = data;
        
        mapImage.sprite = data.mapSprite;
        mapNameText.text = data.mapName;
        
        // Adiciona o listener para o botão de seleção
        selectButton.onClick.AddListener(OnMapSelected);
    }

    private void OnMapSelected()
    {
        // 1. Salvar o nome da cena a ser carregada (Opcional, mas útil se o Manager precisar saber)
        PlayerPrefs.SetString("SelectedGameScenePath", currentMapData.gameScenePath);
        
        // 2. Carregar a cena do jogo
        // NOTA: A cena 'Game' DEVE estar nas Build Settings!
        // Como você forneceu o caminho completo, usaremos o método por string.
        SceneManager.LoadScene(currentMapData.gameScenePath);
        
        // Se a cena Game tiver o mesmo nome em todas as pastas, você pode usar:
        // SceneManager.LoadScene(currentMapData.mapName + "/Game");
    }
}
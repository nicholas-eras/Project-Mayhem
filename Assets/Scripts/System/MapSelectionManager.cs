using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSelectionManager : MonoBehaviour
{
    [Header("Configuração de Dados")]
    [Tooltip("O Asset que contém a lista de todos os mapas disponíveis.")]
    [SerializeField] private MapDatabase mapDatabase;

    [Header("Configuração de UI")]
    [Tooltip("O Prefab da carta de seleção de mapa.")]
    [SerializeField] private GameObject mapCardPrefab;
    
    [Tooltip("O objeto pai (Container) onde os cartões serão instanciados (dentro do Scroll View).")]
    [SerializeField] private Transform cardContainer;

    void Start()
    {
        if (mapDatabase == null)
        {
            Debug.LogError("Map Database não está configurado! Por favor, adicione o asset.");
            return;
        }

        PopulateMapCarousel();
    }

    public void QuitGame()
    {
        Debug.Log("Saindo do Jogo...");

        // 1. Comando principal para fechar a aplicação (funciona em builds)
        Application.Quit();

        // 2. Comando para parar a execução SE você estiver no Editor da Unity.
        // Isso facilita o teste do botão.
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    private void PopulateMapCarousel()
    {
        // Limpa qualquer UI antiga
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var entry in mapDatabase.maps)
        {
            // 1. Cria o objeto MapData (temporário) para a carta
            MapData mapData = ScriptableObject.CreateInstance<MapData>();
            mapData.mapName = entry.mapName;
            mapData.gameScenePath = entry.scenePath;
            mapData.mapSprite = entry.mapImage;
            
            // 2. Instancia a carta de UI
            GameObject cardInstance = Instantiate(mapCardPrefab, cardContainer);
            
            // 3. Configura o cartão
            MapSelectorUI cardUI = cardInstance.GetComponent<MapSelectorUI>();
            if (cardUI != null)
            {
                cardUI.Setup(mapData);
            }
            else
            {
                Debug.LogError("O Prefab do Map Card não tem o script MapSelectorUI!");
            }
        }
    }

    // Função para ser ligada ao botão "Voltar" (Opcional)
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // Altere para o nome da sua cena de menu
    }
}
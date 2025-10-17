using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string mapSelectSceneName = "MapSelectScene";
    public static GameManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        StartCoroutine(LoadMapSelectAfterDelay(3.0f));
    }

    private IEnumerator LoadMapSelectAfterDelay(float delay)
    {
    yield return new WaitForSecondsRealtime(delay); 

        // NOVO: LOCALIZA E DESTRÓI OS MANAGERS ESPECÍFICOS DE CENA
        // Isso é necessário para que o WaveManager e UpgradeManager morram antes da cena.
        // Usamos FindObjectsOfType para garantir que pegamos as instâncias corretas.
        UpgradeManager upgradeManager = FindObjectOfType<UpgradeManager>();
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        
        if (upgradeManager != null)
        {
            // Ao destruir, o OnDisable será chamado, limpando eventos e corrotinas.
            Destroy(upgradeManager.gameObject);
        }
        if (waveManager != null)
        {
            Destroy(waveManager.gameObject);
        }
        
        Time.timeScale = 1.0f;

        bool canLoad = Application.CanStreamedLevelBeLoaded(mapSelectSceneName);

        if (canLoad)
        {
            SceneManager.LoadScene(mapSelectSceneName);
        }
        else
        {
            Debug.LogError($"[GM] FALHA: a cena '{mapSelectSceneName}' não pode ser carregada. Verifique Build Settings e nome.");
        }
    }
}

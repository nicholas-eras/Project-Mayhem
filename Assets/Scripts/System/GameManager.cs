using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para recarregar a cena

public class GameManager : MonoBehaviour
{
    public void GameOver()
    {
        Debug.Log("GAME OVER");
        // Recarrega a cena atual após um pequeno delay
        Invoke(nameof(ReloadScene), 1.5f);
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
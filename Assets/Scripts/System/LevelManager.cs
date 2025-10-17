// LevelManager.cs
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Referências dos Sistemas da Cena")]
    [Tooltip("Arraste o objeto com o script WaveManager para cá.")]
    [SerializeField] private WaveManager waveManager;

    [Tooltip("Arraste o objeto com o script UpgradeManager para cá.")]
    [SerializeField] private UpgradeManager upgradeManager;

    // --- CORREÇÃO 1: ADICIONE A REFERÊNCIA PARA O UIMANAGER ---
    [Tooltip("Arraste o objeto com o script UIManager para cá.")]
    [SerializeField] private UIManager uiManager;

    void Awake()
    {
        // Validação para garantir que você não esqueceu de arrastar as referências no Inspector
        if (waveManager == null || upgradeManager == null || uiManager == null) // Adicionado uiManager à verificação
        {
            Debug.LogError("Referências do WaveManager, UpgradeManager ou UIManager não foram configuradas no LevelManager! Verifique o Inspector.", this.gameObject);
            return;
        }

        // --- CORREÇÃO 2: INICIALIZE O UIMANAGER ---
        // Agora o LevelManager está conectando o UIManager ao WaveManager.
        // Ele está dizendo ao UIManager: "Ei, ouça os eventos DESTE WaveManager".
        uiManager.Initialize(waveManager);
        
        // A linha abaixo (se ainda existir) pode ser mantida se o WaveManager precisar de uma referência
        // ao UpgradeManager, mas ela não é relevante para o problema da UI.
        // waveManager.Initialize(upgradeManager); // Mantenha se precisar.
    }
}
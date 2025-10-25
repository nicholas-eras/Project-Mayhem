using UnityEngine;
using TMPro;

public class CoinCounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;
    private PlayerWallet targetWallet;

    void Awake()
    {
        // Tenta pegar o TextMeshPro no próprio objeto se não foi arrastado
        if (coinText == null)
        {
            coinText = GetComponent<TextMeshProUGUI>();
        }
    }
    
    /// <summary>
    /// Chamado pelo AgentManager ou Spawner do jogador local.
    /// </summary>
    public void Setup(PlayerWallet wallet)
    {
        if (targetWallet != null)
        {
            // Desinscreve-se da carteira antiga, se houver (boa prática)
            targetWallet.OnMoneyChanged -= UpdateDisplay;
        }

        targetWallet = wallet;
        
        if (targetWallet != null)
        {
            // 1. Inscreve-se no evento da CARTEIRA DESTE JOGADOR
            targetWallet.OnMoneyChanged += UpdateDisplay;

            // 2. Chama a função para atualizar imediatamente com o valor atual
            UpdateDisplay(targetWallet.CurrentMoney);
        }
    }

    private void UpdateDisplay(int newMoney)
    {
        if (coinText != null)
        {
            coinText.text = $"Coins: {newMoney}"; // Ou o formato que você usa
        }
    }

    void OnDestroy()
    {
        // Limpa a inscrição para evitar erros quando o objeto for destruído
        if (targetWallet != null)
        {
            targetWallet.OnMoneyChanged -= UpdateDisplay;
        }
    }
}
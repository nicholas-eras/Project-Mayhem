// PlayerWallet.cs
using UnityEngine;
using UnityEngine.Events;

public class PlayerWallet : MonoBehaviour
{
    // Dinheiro é INDIVIDUAL, não precisa de WalletManager para isso.
    private int currentMoney = 0; 
    public int CurrentMoney => currentMoney; 
    
    // Evento NÃO-ESTÁTICO para a UI do Owner se inscrever
    public UnityAction<int> OnMoneyChanged; 

    void Awake()
    {
        currentMoney = 0;
    }

    /// <summary>
    /// Adiciona dinheiro. Chamado pela Coin.
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        OnMoneyChanged?.Invoke(currentMoney);
    }
    
    /// <summary>
    /// Gasta dinheiro. Chamado pelo UpgradeManager.
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return false;

        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            OnMoneyChanged?.Invoke(currentMoney);
            return true;
        }
        return false;
    }
}
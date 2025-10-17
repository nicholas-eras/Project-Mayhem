using UnityEngine;
using UnityEngine.Events; // Importante para os eventos

public class PlayerWallet : MonoBehaviour
{
    // Propriedade para acessar o dinheiro. Só pode ser alterada por este script.
    public int CurrentMoney { get; private set; }

    // Evento que avisa a UI (e outros sistemas) que o dinheiro mudou.
    // O <int> significa que vamos enviar o novo valor total.
    public static UnityAction<int> OnMoneyChanged;

    void Awake()
    {
        CurrentMoney = 0;
    }

    public void AddMoney(int amount)
    {
        CurrentMoney += amount;

        // Dispara o evento para notificar a UI.
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

     // NOVA FUNÇÃO para gastar dinheiro
    public bool SpendMoney(int amount)
    {
        if (CurrentMoney >= amount)
        {
            CurrentMoney -= amount;
            OnMoneyChanged?.Invoke(CurrentMoney);
            return true; // Compra bem-sucedida
        }
        else
        {
            return false; // Dinheiro insuficiente
        }
    }
}
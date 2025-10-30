using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq; // <-- ADICIONE O LINQ
using System.Collections; // <-- ADICIONE O COLLECTIONS
using Unity.Netcode; // <-- ADICIONE O NETCODE

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Referências")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject upgradeCardPrefab;
    [SerializeField] private Transform cardContainer;
    
    // --- Referências do Jogador Humano (do seu RegisterPlayer) ---
    private PlayerWallet playerWallet;
    private PlayerController playerController;
    private HealthSystem playerHealthSystem;
    private PlayerWeaponManager playerWeaponManager;
    private PlayerUpgradeTracker localHumanPlayerTracker; // <-- ADICIONADO
    // ---

    [Header("Configuração da Loja")]
    [SerializeField] private List<UpgradeData> allUpgrades;
    [SerializeField] private int optionsToShow = 4;
    [SerializeField] private int rerollCost = 50;
    [SerializeField] private float botThinkTime = 0.5f; // Tempo (em segundos reais) que o bot "pensa"
    
    public event UnityAction OnShopClosed;
    
    // --- DICIONÁRIO GLOBAL REMOVIDO ---
    private JoystickMove movementInputScript;
    private GameObject joystickVisual;
    public static bool IsShopOpen { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// O seu método original. Agora ele também guarda o Tracker.
    /// </summary>
    public void RegisterPlayer(GameObject player, JoystickMove joystickScript, GameObject joystickViz)
    {
        if (player == null)
        {
            Debug.LogError("UpgradeManager: Tentativa de registrar um Jogador nulo!");
            return;
        }

        // 1. Pega todos os componentes do jogador humano
        playerWallet = player.GetComponent<PlayerWallet>();
        playerController = player.GetComponent<PlayerController>();
        playerHealthSystem = player.GetComponent<HealthSystem>();
        playerWeaponManager = player.GetComponent<PlayerWeaponManager>();
        
        // --- ADICIONADO ---
        localHumanPlayerTracker = player.GetComponent<PlayerUpgradeTracker>();
        // --- FIM ---
        
        // 2. Armazena as referências do joystick
        movementInputScript = joystickScript;
        joystickVisual = joystickViz;

        // 3. Verifica se tudo foi encontrado
        if (playerWallet == null || playerController == null || playerHealthSystem == null || playerWeaponManager == null || localHumanPlayerTracker == null)
        {
            Debug.LogError($"UpgradeManager: Falha ao pegar todos os componentes do Jogador '{player.name}'!", player);
        }
    }
    
    public void ShowUpgradeScreen()
    {
        // 1. VERIFICAÇÃO DE SEGURANÇA (para o Jogador Humano)
        if (localHumanPlayerTracker == null)
        {
            Debug.LogError("Loja aberta, mas NENHUM JOGADOR HUMANO foi registrado! Chame RegisterPlayer().");
            return; 
        }
        
        // 2. PAUSA O JOGO E PREPARA A UI (para o Humano)
        Time.timeScale = 0f;
        upgradePanel.SetActive(true);
        IsShopOpen = true;
        
        if (movementInputScript != null)
        {
            movementInputScript.enabled = false;
        }
        if (joystickVisual != null)
        {
            joystickVisual.SetActive(false);
        }
        
        // 3. GERA AS CARTAS (para o Humano)
        GerarNovasCartas(localHumanPlayerTracker);

        // --- 4. INICIA A LÓGICA DOS BOTS ---
        // (Assumindo que os bots têm o componente AgentManager)
        AgentManager[] allAgents = FindObjectsOfType<AgentManager>(true);
        
        foreach (AgentManager agent in allAgents)
        {
            if (agent.IsPlayerBot())
            {
                PlayerUpgradeTracker botTracker = agent.GetComponent<PlayerUpgradeTracker>();
                if (botTracker != null)
                {
                    StartCoroutine(RunBotUpgradeLogic(botTracker));
                }
            }
        }
        // --- FIM DA LÓGICA DOS BOTS ---
    }
    
    /// <summary>
    /// Lógica de IA para um Bot comprar upgrades.
    /// Usa WaitForSecondsRealtime para rodar com o Time.timeScale = 0
    /// </summary>
    private IEnumerator RunBotUpgradeLogic(PlayerUpgradeTracker botTracker)
    {
        Debug.Log($"[Bot {botTracker.name}] a verificar a loja...");
        yield return new WaitForSecondsRealtime(botThinkTime); // Pensa por um momento

        // Loop: "Enquanto eu tiver dinheiro, continuo a comprar"
        while (botTracker.Wallet.CurrentMoney > 0)
        {
            // 1. Encontra o upgrade mais barato que o bot pode comprar
            UpgradeData bestUpgrade = ChooseBestUpgradeForBot(botTracker);

            if (bestUpgrade == null)
            {
                // Não há nada para comprar (ou já comprou tudo)
                Debug.Log($"[Bot {botTracker.name}] Não há mais upgrades disponíveis ou acessíveis.");
                break;
            }

            // 2. Tenta comprar
            if (botTracker.TryPurchaseUpgrade(bestUpgrade))
            {
                // Conseguiu!
                Debug.Log($"[Bot {botTracker.name}] Comprou {bestUpgrade.upgradeName}!");
                yield return new WaitForSecondsRealtime(botThinkTime); // Pensa...
            }
            else
            {
                // Falha (provavelmente não tem dinheiro suficiente para o mais barato)
                // Vamos tentar um "reroll" se ele puder pagar.
                if (botTracker.Wallet.SpendMoney(rerollCost))
                {
                     Debug.Log($"[Bot {botTracker.name}] Não gostou das opções, pagou {rerollCost} por um Reroll.");
                     yield return new WaitForSecondsRealtime(botThinkTime); // Pensa...
                     // O loop continua, e vai chamar ChooseBestUpgradeForBot() de novo
                }
                else
                {
                    // Não pode comprar o upgrade E não pode pagar o reroll. Desiste.
                    Debug.Log($"[Bot {botTracker.name}] Sem dinheiro para mais upgrades ou reroll.");
                    break;
                }
            }
        }
        
        Debug.Log($"[Bot {botTracker.name}] Terminou de comprar.");
    }
    
    /// <summary>
    /// Lógica de decisão simples para o Bot.
    /// </summary>
    private UpgradeData ChooseBestUpgradeForBot(PlayerUpgradeTracker botTracker)
    {
        // 1. Pega todos os upgrades possíveis
        List<UpgradeData> possibleUpgrades = new List<UpgradeData>(allUpgrades);

        // 2. Filtra: Remove os que ele não pode pagar
        // Usamos .ToList() para poder modificar a lista enquanto iteramos
        foreach (var upgrade in possibleUpgrades.ToList())
        {
            int cost = botTracker.GetCurrentCost(upgrade);
            if (cost > botTracker.Wallet.CurrentMoney)
            {
                possibleUpgrades.Remove(upgrade);
            }
            // TODO: Adicionar lógica para nível máximo (se existir)
        }

        // 3. Se sobraram opções, escolhe uma aleatoriamente
        if (possibleUpgrades.Count > 0)
        {
            return possibleUpgrades[Random.Range(0, possibleUpgrades.Count)];
        }

        // 4. Não sobrou nada que ele possa pagar
        return null; 
    }

    /// <summary>
    /// Botão Reroll (só para o Humano)
    /// </summary>
    public void AtualizarOpcoes()
    {
        if (localHumanPlayerTracker.Wallet.SpendMoney(rerollCost))
        {
            GerarNovasCartas(localHumanPlayerTracker);
        }
        else
        {
            Debug.Log("Dinheiro insuficiente para atualizar a loja! Custo: " + rerollCost);
        }
    }

    /// <summary>
    /// Botão Comprar (só para o Humano)
    /// </summary>
    public void PurchaseUpgrade(UpgradeData upgrade, UpgradeCardUI cardUI)
    {
        int requiredCost = localHumanPlayerTracker.GetCurrentCost(upgrade); 

        if (localHumanPlayerTracker.TryPurchaseUpgrade(upgrade))
        {
            
            UpdateAllVisibleCardCosts(upgrade.type, localHumanPlayerTracker);
            
            Destroy(cardUI.gameObject);
        }
        else
        {
            Debug.Log("Dinheiro insuficiente! Custo: " + requiredCost);
        }
    }
    
    // --- GetCurrentCost() REMOVIDO DAQUI ---
    // (Agora está no PlayerUpgradeTracker)

    /// <summary>
    /// Gera as cartas de UI para um tracker específico (o do humano).
    /// </summary>
    private void GerarNovasCartas(PlayerUpgradeTracker tracker)
    {
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        List<UpgradeData> upgradesPool = new List<UpgradeData>(allUpgrades);
        if (upgradesPool.Count == 0) return;

        List<UpgradeData> chosenUpgrades = new List<UpgradeData>();
        for (int i = 0; i < optionsToShow; i++)
        {
            if (upgradesPool.Count > 0)
            {
                int index = Random.Range(0, upgradesPool.Count);
                UpgradeData randomUpgrade = upgradesPool[index];
                chosenUpgrades.Add(randomUpgrade);
                upgradesPool.RemoveAt(index); 
            }
            else break;
        }

        foreach (var upgrade in chosenUpgrades)
        {
            // Pega o custo individual do tracker humano
            int dynamicCost = tracker.GetCurrentCost(upgrade);
            GameObject cardInstance = Instantiate(upgradeCardPrefab, cardContainer);
            UpgradeCardUI cardUI = cardInstance.GetComponent<UpgradeCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(upgrade, this, dynamicCost);
            }
        }
    }

    // --- ApplyUpgrade() REMOVIDO DAQUI ---
    // (Agora está no PlayerUpgradeTracker)
    
    public void CloseShop()
    {
        if (movementInputScript != null)
        {
            movementInputScript.enabled = true;
        }
        IsShopOpen = false;
        if (joystickVisual != null)
        {
            joystickVisual.SetActive(true);
        }
        Time.timeScale = 1f;
        upgradePanel.SetActive(false);
        OnShopClosed?.Invoke();
    }

    /// <summary>
    /// Atualiza o custo visual das cartas na loja do humano.
    /// </summary>
    private void UpdateAllVisibleCardCosts(UpgradeType purchasedType, PlayerUpgradeTracker tracker)
    {
        UpgradeCardUI[] activeCards = cardContainer.GetComponentsInChildren<UpgradeCardUI>();
        foreach (UpgradeCardUI card in activeCards)
        {
            if (card.CurrentUpgrade != null && card.CurrentUpgrade.type == purchasedType)
            {
                int newCost = tracker.GetCurrentCost(card.CurrentUpgrade);
                card.UpdateCostDisplay(newCost);
            }
        }
    }
}
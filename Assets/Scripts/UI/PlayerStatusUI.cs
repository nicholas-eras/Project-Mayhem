using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("Visual do Jogador")]
    [SerializeField] public Image playerSpriteImage;

    [Header("Referências de Texto Separadas")]
    [SerializeField] private TextMeshProUGUI healthMaxText;
    [SerializeField] private TextMeshProUGUI regenRateText;
    [SerializeField] private TextMeshProUGUI invulnTimeText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI fireRateText;
    [SerializeField] private TextMeshProUGUI rangeText;

    [Header("Referências do Preview de Armas")]
    [SerializeField] private Transform weaponPreviewHolder;
    [SerializeField] private GameObject weaponIconPrefab;
    [SerializeField] private float uiWeaponRadius = 100f;
    [SerializeField] private Vector2 uiIconSize = new Vector2(16, 16);

    // --- Referências locais ---
    private AgentManager targetAgent;
    private PlayerController playerController;
    private HealthSystem healthSystem;
    private PlayerWeaponManager weaponManager;
    private SpriteRenderer playerRenderer; // <-- NOVA VARIÁVEL

    /// <summary>
    /// Configuração inicial chamada pelo TeamStatusUIManager
    /// </summary>
    public void Setup(AgentManager agent)
    {
        if (agent == null)
        {
            Debug.LogError("PlayerStatusUI: Setup falhou, Agent está nulo!");
            Destroy(gameObject);
            return;
        }

        // 1. Guarda as referências dos componentes
        targetAgent = agent;
        playerController = targetAgent.GetComponent<PlayerController>();
        healthSystem = targetAgent.GetComponent<HealthSystem>();
        weaponManager = targetAgent.GetComponent<PlayerWeaponManager>();

        // 2. GUARDA O RENDERER (mas não o sprite ainda)
        playerRenderer = targetAgent.GetComponentInChildren<SpriteRenderer>(); 

        if (playerController == null || healthSystem == null || weaponManager == null || playerRenderer == null)
        {
             Debug.LogError($"PlayerStatusUI: Falha ao obter componentes do Agente {agent.name}!");
             Destroy(gameObject);
             return;
        }

        // 3. LÓGICA DO SPRITE REMOVIDA DAQUI
        // (Será movida para o UpdateDisplay)

        // 4. Faz a primeira atualização
        UpdateDisplay();
    }

    /// <summary>
    /// Auto-atualiza o painel e auto-destrói-se
    /// </summary>
    void LateUpdate()
    {
        if (targetAgent == null)
        {
            Destroy(gameObject);
            return;
        }

        // Atualiza a UI em tempo real
        UpdateDisplay();
    }

    /// <summary>
    /// Atualiza todos os campos de texto, ícones de armas E O SPRITE DO JOGADOR
    /// </summary>
    public void UpdateDisplay()
    {
        // --- LÓGICA DO SPRITE MOVIDA PARA AQUI ---
        // 0. ATUALIZA O SPRITE DO JOGADOR
        if (playerRenderer != null && playerSpriteImage != null)
        {
            // Se o sprite no renderer for válido E for diferente do que estamos a mostrar...
            if (playerRenderer.sprite != null && playerSpriteImage.sprite != playerRenderer.sprite)
            {
                // ...atualiza a UI.
                playerSpriteImage.sprite = playerRenderer.sprite;
                playerSpriteImage.color = Color.white;
                playerSpriteImage.preserveAspect = true;
            }
            // Se o sprite no renderer for nulo (ex: ainda não carregou)...
            else if (playerRenderer.sprite == null)
            {
                // ...mostra o fallback (transparente).
                playerSpriteImage.sprite = null;
                playerSpriteImage.color = new Color(0, 0, 0, 0);
            }
        }
        // --- FIM DA LÓGICA DO SPRITE ---

        // 1. STATS DE DEFESA (HealthSystem)
        if (healthSystem != null)
        {
            // ... (o seu código original de stats)
            if (healthMaxText != null) healthMaxText.text = $"VIDA MÁX: {healthSystem.MaxHealth.ToString("F0")}";
            if (regenRateText != null) regenRateText.text = $"REGEN: +{healthSystem.regenRate.ToString("F1")}/s";
            if (invulnTimeText != null) invulnTimeText.text = $"INVULN: {healthSystem.damageCooldown.ToString("F2")}s";
        }

        // 2. STATS DE MOVIMENTO (PlayerController)
        if (playerController != null)
        {
            if (speedText != null) speedText.text = $"VELOCIDADE: {playerController.currentMoveSpeed.ToString("F2")}x";
        }

        // 3. STATS DE ARMAS (Textos da Arma Principal)
        if (weaponManager != null)
        {
            // ... (o seu código original de stats de armas)
            GameObject mainWeaponGO = weaponManager.GetEquippedWeapon(0);
            WeaponController mainWeapon = (mainWeaponGO != null) ? mainWeaponGO.GetComponent<WeaponController>() : null;

            if (mainWeapon != null && mainWeapon.weaponData != null)
            {
                if (damageText != null) damageText.text = $"DANO: {mainWeapon.weaponData.damage.ToString("F1")}";
                if (fireRateText != null) fireRateText.text = $"CADÊNCIA: {mainWeapon.weaponData.fireRateInterval.ToString("F2")} TPS";
                if (rangeText != null) rangeText.text = $"ALCANCE: {mainWeapon.weaponData.range.ToString("F1")}";
            }
            else
            {
                if (damageText != null) damageText.text = "DANO: N/A";
                if (fireRateText != null) fireRateText.text = "CADÊNCIA: N/A";
                if (rangeText != null) rangeText.text = "ALCANCE: N/A";
            }
        }

        // 4. ATUALIZA O VISUAL DAS ARMAS
        UpdateWeaponIcons();
    }
    
    /// <summary>
    /// Atualiza os ícones de armas que giram à volta do jogador
    /// </summary>
    private void UpdateWeaponIcons()
    {
        // 1. Verifica se as referências necessárias existem
        if (weaponManager == null || weaponPreviewHolder == null || weaponIconPrefab == null)
        {
            return;
        }

        // 2. Limpa os ícones antigos
        foreach (Transform child in weaponPreviewHolder)
        {
            Destroy(child.gameObject);
        }

        // 3. Pede ao PlayerWeaponManager a lista de ícones atuais
        List<Sprite> equippedIcons = weaponManager.GetEquippedWeaponIcons();
        if (equippedIcons.Count == 0) return;

        // 4. Calcula o posicionamento em círculo
        float angleStep = 360f / equippedIcons.Count;

        // 5. Instancia e posiciona os novos ícones
        for (int i = 0; i < equippedIcons.Count; i++)
        {
            Sprite icon = equippedIcons[i];
            if (icon == null) continue;

            // Calcula a posição no círculo
            float angle = i * angleStep;
            Vector3 weaponPosition = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad),
                0
            ) * uiWeaponRadius;

            // Cria a instância do prefab
            GameObject iconInstance = Instantiate(weaponIconPrefab, weaponPreviewHolder);

            // Pega o RectTransform
            RectTransform iconRect = iconInstance.GetComponent<RectTransform>();
            if (iconRect == null) continue; // Segurança

            // FORÇA as âncoras e pivô para o centro (Ignora o prefab)
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            // FORÇA o tamanho (sizeDelta)
            iconRect.sizeDelta = uiIconSize;

            // Define a Posição Local
            iconRect.anchoredPosition = weaponPosition;

            // Define o Sprite
            Image iconImage = iconInstance.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
            }
        }
    }
}
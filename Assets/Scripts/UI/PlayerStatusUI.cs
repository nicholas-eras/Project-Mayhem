using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // Necessário para List<>

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

    // --- MUDANÇA: Referências para o "Preview" das Armas ---
    [Header("Referências do Preview de Armas")]
    [Tooltip("O 'holder' (GameObject) centrado sobre a imagem do player onde os ícones das armas irão girar.")]
    [SerializeField] private Transform weaponPreviewHolder; // <-- MUDOU O NOME E A FUNÇÃO
    [Tooltip("O prefab de UI (uma simples 'Image') para instanciar como ícone.")]
    [SerializeField] private GameObject weaponIconPrefab;
    [Tooltip("O raio em pixels para posicionar os ícones de arma na UI.")]
    [SerializeField] private float uiWeaponRadius = 100f; // <-- NOVO CAMPO
    [Tooltip("O tamanho (Largura e Altura) para forçar nos ícones da UI.")]
    [SerializeField] private Vector2 uiIconSize = new Vector2(16, 16);
    // --------------------------------------------------


    // Referências dos Managers
    private PlayerController playerController;
    private HealthSystem healthSystem;
    private PlayerWeaponManager weaponManager;

    public void Setup(PlayerController pc, HealthSystem hs, PlayerWeaponManager pwm)
    {
        playerController = pc;
        healthSystem = hs;
        weaponManager = pwm;

        SpriteRenderer playerRenderer = pc.GetComponentInChildren<SpriteRenderer>(); 
        if (playerRenderer != null && playerSpriteImage != null)
        {
            if (playerRenderer.sprite != null)
            {
                playerSpriteImage.sprite = playerRenderer.sprite;
            }
        }

        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        // 1. STATS DE DEFESA (HealthSystem)
        if (healthSystem != null)
        {
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

    // --- MÉTODO 'UpdateWeaponIcons' ATUALIZADO ---
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

            // --- INÍCIO DAS NOVAS LINHAS ---

            // Pega o RectTransform
            RectTransform iconRect = iconInstance.GetComponent<RectTransform>();
            if (iconRect == null) continue; // Segurança

            // FORÇA as âncoras e pivô para o centro (Ignora o prefab)
            // Isso garante que o 'anchoredPosition' funcione corretamente
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            // FORÇA o tamanho (sizeDelta) para o nosso novo campo (ex: 64x64)
            iconRect.sizeDelta = uiIconSize;

            // --- FIM DAS NOVAS LINHAS ---

            // Define a Posição Local
            iconRect.anchoredPosition = weaponPosition;

            // Define o Sprite
            Image iconImage = iconInstance.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = icon;

                // --- BÔNUS: Força o 'Preserve Aspect' em código ---
                // Isso garante que a arma não fique esticada, mesmo se o prefab não tiver isso
                iconImage.preserveAspect = true;
            }
        }
    }
    
}
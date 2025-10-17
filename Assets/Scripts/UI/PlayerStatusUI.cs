using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatusUI : MonoBehaviour
{
    // NOVO: Campos de texto separados
    [Header("Visual do Jogador")]
    [SerializeField] private Image playerSpriteImage; 

    [Header("Referências de Texto Separadas")]
    [SerializeField] private TextMeshProUGUI healthMaxText; // Vida Máxima
    [SerializeField] private TextMeshProUGUI regenRateText; // Taxa de Regeneração
    [SerializeField] private TextMeshProUGUI invulnTimeText; // Tempo de Invulnerabilidade (Cooldown)
    
    [SerializeField] private TextMeshProUGUI speedText; // Velocidade de Movimento
    
    [SerializeField] private TextMeshProUGUI damageText; // Dano da Arma Principal
    [SerializeField] private TextMeshProUGUI fireRateText; // Cadência da Arma Principal
    [SerializeField] private TextMeshProUGUI rangeText; // Alcance da Arma Principal

    // Referências dos Managers
    private PlayerController playerController;
    private HealthSystem healthSystem;
    private PlayerWeaponManager weaponManager;

    public void Setup(PlayerController pc, HealthSystem hs, PlayerWeaponManager pwm)
    {
        playerController = pc;
        healthSystem = hs;
        weaponManager = pwm;
        
        // CORREÇÃO DE SPRITE: Usando o método seguro que funciona se estiver no pai.
        SpriteRenderer playerRenderer = pc.GetComponent<SpriteRenderer>();

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
            if (healthMaxText != null) 
                 healthMaxText.text = $"VIDA MÁX: {healthSystem.MaxHealth.ToString("F0")}";

            if (regenRateText != null) 
                 regenRateText.text = $"REGEN: +{healthSystem.regenRate.ToString("F1")}/s";

            // Para Invulnerabilidade, você precisa acessar o campo damageCooldown (que deve ser público/propriedade)
            // Assumindo que você tornou 'damageCooldown' público no HealthSystem.
            if (invulnTimeText != null) 
                 invulnTimeText.text = $"INVULN: {healthSystem.damageCooldown.ToString("F2")}s";
        }

        // 2. STATS DE MOVIMENTO (PlayerController)
        if (playerController != null)
        {
            // Assumindo que você tornou 'currentMoveSpeed' público ou acessível.
            if (speedText != null) 
                 speedText.text = $"VELOCIDADE: {playerController.currentMoveSpeed.ToString("F2")}x"; 
        }
        
        // 3. STATS DE ARMAS (PlayerWeaponManager)
        if (weaponManager != null)
        {
            GameObject mainWeaponGO = weaponManager.GetEquippedWeapon(0);
            WeaponController mainWeapon = (mainWeaponGO != null) ? mainWeaponGO.GetComponent<WeaponController>() : null;
            
            if (mainWeapon != null && mainWeapon.weaponData != null)
            {
                // Exibe Dano, Cadência e Alcance da Arma Principal
                if (damageText != null)
                     damageText.text = $"DANO: {mainWeapon.weaponData.damage.ToString("F1")}";
                     
                if (fireRateText != null)
                     fireRateText.text = $"CADÊNCIA: {mainWeapon.weaponData.fireRate.ToString("F2")} TPS";

                if (rangeText != null)
                     rangeText.text = $"ALCANCE: {mainWeapon.weaponData.range.ToString("F1")}";
            }
            else
            {
                // Se não houver arma, exibe N/A
                if (damageText != null) damageText.text = "DANO: N/A";
                if (fireRateText != null) fireRateText.text = "CADÊNCIA: N/A";
                if (rangeText != null) rangeText.text = "ALCANCE: N/A";
            }
        }
    }
}
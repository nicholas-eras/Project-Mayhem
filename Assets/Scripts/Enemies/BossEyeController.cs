// BossEyeController.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class BossEyeController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private BossPartComponent bossPart;
    [SerializeField] private EnemyShooter enemyShooter;
    [SerializeField] private BossLaserAttack laserAttack;

    [Header("Configuração do Laser (Probabilidade)")]
    [Tooltip("Com que frequência (em segundos) o Olho verifica se o jogador está na área do laser.")]
    [SerializeField] private float laserCheckInterval = 0.25f; // Timer independente para o Laser
    [Range(0, 100)]
    [SerializeField] private int laserChancePercentage = 50; 
    [SerializeField] private int chanceIncreasePerCycle = 15;
    [SerializeField] private int maxLaserChance = 100;
    
    // Contadores de provocação
    private int consecutiveLaserCyclesInRange = 0; 
    
    // Rotina de bloqueio (Impede o Update de pensar durante o laser)
    private Coroutine laserAttackRoutine; 
    
    // Timers INDEPENDENTES
    private float nextLaserCheckTime;
    private float nextProjectileTime;

    void Start()
    {
        if (bossPart == null) bossPart = GetComponent<BossPartComponent>();
        if (enemyShooter == null) enemyShooter = GetComponent<EnemyShooter>();
        if (laserAttack == null) laserAttack = GetComponent<BossLaserAttack>();
        
        // Inicia os timers independentes
        nextLaserCheckTime = Time.time + laserCheckInterval + Random.Range(0f, 0.5f);
        if (enemyShooter != null)
            nextProjectileTime = Time.time + enemyShooter.fireRateInterval + Random.Range(0f, 1f);
    }

    // O CÉREBRO DO OLHO (com dois timers)
    void Update()
    {
        // Se o Laser estiver ativo (laserAttackRoutine != null), NÃO FAÇA NADA. O Olho está ocupado.
        if (laserAttackRoutine != null) return;
        
        if (enemyShooter == null || enemyShooter.playerTarget == null) return;
        
        // --- CÉREBRO 1: LÓGICA DO PROJÉTIL (Timer do fireRateInterval) ---
        // Este timer funciona o tempo todo, independente do laser.
        if (Time.time >= nextProjectileTime)
        {
            nextProjectileTime = Time.time + enemyShooter.fireRateInterval; // Reinicia o timer do projétil            
        }
        
        // --- CÉREBRO 2: LÓGICA DO LASER (Timer de Verificação/Probabilidade) ---
        // Este timer também funciona o tempo todo.
        if (Time.time >= nextLaserCheckTime)
        {
            nextLaserCheckTime = Time.time + laserCheckInterval; // Reinicia o timer de *verificação*

            bool playerIsInRange = laserAttack.IsPlayerInHorizontalRange();
            bool shouldFireLaser = false;

            if (playerIsInRange)
            {
                // A CADA 'laserCheckInterval' (ex: 0.25s) que o jogador está na área,
                // o contador de provocação aumenta.
                consecutiveLaserCyclesInRange++; 
                
                int currentChance = laserChancePercentage + ((consecutiveLaserCyclesInRange - 1) * chanceIncreasePerCycle);
                currentChance = Mathf.Min(currentChance, maxLaserChance);
                int chanceRoll = Random.Range(0, 100); 
                
                if (chanceRoll < currentChance)
                    shouldFireLaser = true;
            }
            else
            {
                // Se o jogador saiu da área, zera a provocação
                consecutiveLaserCyclesInRange = 0; 
            }

            // Se a probabilidade deu certo:
            if (shouldFireLaser)
            {
                // Zera a provocação APÓS o sucesso
                consecutiveLaserCyclesInRange = 0; 
                // Inicia o Laser (que bloqueará este Update)
                laserAttackRoutine = StartCoroutine(LaserAttackSequence());
            }
        }
    }

    /// <summary>
    /// Rotina que executa a sequência de Laser e bloqueia o Update().
    /// </summary>
    private IEnumerator LaserAttackSequence()
    {
        // O Boss está virado para a direita? (Scale.x é positivo)
        bool isFacingRight = transform.localPosition.x < 0;
        // O ângulo de rotação Z que faz o sprite ficar "reto e para frente".
        // Baseado no seu feedback:
        // - Se estiver virado para a direita (Scale.x > 0), o ângulo correto é 180 graus.
        // - Se estiver virado para a esquerda (Scale.x < 0), o ângulo correto é 0 graus.
        float targetZRotation = isFacingRight ? 0f : 180f;

        // Força o transform do Eye a ter a rotação Z correta (horizontal e para frente)
        transform.rotation = Quaternion.Euler(0, 0, targetZRotation);
        
        // Desativa o AimAtPlayer do EnemyShooter para que ele não tente girar o olho
        // enquanto o laser está ativo.
        if (enemyShooter != null)
        {
            enemyShooter.enabled = false;
        }

        // Inicia o visual de ataque do laser (Agora o sprite estará reto)
        bossPart.SetAttackVisual();
        laserAttack.StartLaserAttack();

        // Espera o laser terminar (aviso + ataque)
        float totalLaserDuration = laserAttack.warningDuration + laserAttack.attackDuration;
        yield return new WaitForSeconds(totalLaserDuration);

        // 3. O olho permanece em rotação Z=0 neste ponto.
        // O próximo ciclo do EnemyShooter.Update (que será reativado) fará o AimAtPlayer 
        // e ajustará a rotação do transform se o alvo estiver na faixa.

        // Reativa tudo
        bossPart.SetDefaultVisual();
        if (enemyShooter != null) enemyShooter.enabled = true; // Reativa o AimAtPlayer

        // Libera o Update() para voltar a "pensar"
        laserAttackRoutine = null;

        // Sincroniza os timers para não serem imediatos
        nextLaserCheckTime = Time.time + laserCheckInterval;
        nextProjectileTime = Time.time + enemyShooter.fireRateInterval;
    }
    
    private void OnDestroy() 
    {
        // Garante que o atirador seja reativado se o olho for destruído
        if (enemyShooter != null) enemyShooter.enabled = true;
        if (laserAttackRoutine != null) StopCoroutine(laserAttackRoutine);
        if (laserAttack != null) laserAttack.ClearLaser();
    }
}
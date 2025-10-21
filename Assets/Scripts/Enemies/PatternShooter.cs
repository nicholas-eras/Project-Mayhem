using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// Definição dos padrões geométricos
public enum ShootingPattern
{
    TowardsTarget, // 0: Tiro único (Padrão)
    Spread_Fan,    // 1: Leque (espalha para frente)
    Cross_Plus,    // 2: Quatro direções fixas (+ forma)
    Cross_X,       // 3: Quatro diagonais fixas (X forma)
    Circle_360,    // 4: Todos os 360 graus (Bullet Hell)
    Spiral_Time,   // 5: Espiral periódica ou infinita
    Column_Wall,   // 6: Coluna com abertura de escape (Aleatório)
    Rotating_Cross_Column, // 7: Cruz de Colunas Rotatória (Escudo Estático)
    Random_Selection // 8: Escolhe aleatoriamente
}

public class PatternShooter : MonoBehaviour
{
    [Header("1. Seleção e Controle")]
    [Tooltip("O padrão de tiro a ser usado (ou Random_Selection).")]
    [SerializeField] private ShootingPattern patternType = ShootingPattern.TowardsTarget;
    [SerializeField] private List<ShootingPattern> randomPatterns;

    // --- Configurações Comuns de Burst ---
    [Header("2. Configuração de Burst/Geometria")]
    [Tooltip("Número de projéteis disparados (Para Cross, Circle, Fan).")]
    [SerializeField] private int burstCount = 8;
    
    // --- Configurações de Spread ---
    [Tooltip("Ângulo total do leque (apenas para Spread/Fan).")]
    [SerializeField] private float spreadAngle = 90f;

    // --- Configurações de Colunas ---
    [Header("3. Configuração de Colunas & Cruz")]
    [Tooltip("Número de projéteis em uma única coluna/braço.")]
    [SerializeField] private int totalProjectilesInColumn = 7;
    [Tooltip("Espaçamento (World Units) entre projéteis na coluna.")]
    [SerializeField] private float columnSpacing = 0.8f;
    [Tooltip("Se o buraco de fuga na coluna deve ser randomizado a cada tiro.")]
    [SerializeField] private bool randomizeEscapeIndex = true;
    [Range(0, 6)]
    [SerializeField] private int fixedEscapeIndex = 3; 

    // --- Configurações de Espiral/Periódico ---
    [Header("4. Configuração de Espiral/Ciclo")]
    [Tooltip("Se for infinito, durará até que o inimigo seja destruído.")]
    [SerializeField] private bool infiniteSpiral = false;
    [Tooltip("Número de voltas completas (360º) para o ciclo não infinito.")]
    [Range(1, 10)]
    [SerializeField] private int requiredRotations = 1;
    [Tooltip("Intervalo de tempo entre cada tiro da Espiral.")]
    [SerializeField] private float spiralInterval = 0.1f;
    [Tooltip("Passo de rotação (graus) a cada tiro.")]
    [SerializeField] private float spiralRotationStep = 15f;
    
    // --- Configurações de Cruz Rotatória (Escudo) ---
    [Header("5. Configuração do Escudo Rotatório")]
    [Tooltip("O objeto pai dos escudos que realiza a rotação (deve ter ShieldRotator.cs).")]
    [SerializeField] private Transform shieldRotationParent; 
    
    [Header("6. Referências")]
    [SerializeField] private EnemyShooter enemyShooter;
    [SerializeField] private Transform firePoint;

    // Variáveis de estado
    private Transform playerTarget;
    private float projectileDamage;
    private float projectileSpeed;
    private float projectileLifetime;
    
    public Coroutine currentPatternRoutine { get; private set; }
    private float spiralCurrentAngle = 0f;
    
    // =================================================================

    void Start()
    {
        if (enemyShooter == null) { enemyShooter = GetComponent<EnemyShooter>(); }
        playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (enemyShooter == null || firePoint == null)
        {
            Debug.LogError("PatternShooter precisa de EnemyShooter e FirePoint configurados!", this);
            enabled = false;
        }
    }
    
    /// <summary>
    /// Inicia o padrão de tiro principal ou aleatório.
    /// Chamado pelo EnemyShooter.
    /// </summary>
    public void ShootPattern(float damage, float speed, float lifetime)
    {
        projectileDamage = damage;
        projectileSpeed = speed;
        projectileLifetime = lifetime;

        // Se uma corrotina estiver rodando (e este nao for o primeiro tiro), ignore.
        if (currentPatternRoutine != null && patternType == ShootingPattern.Spiral_Time) return;

        // Limpa a rotina anterior se for um burst que se sobrepõe
        if (currentPatternRoutine != null)
        {
            StopCoroutine(currentPatternRoutine);
            currentPatternRoutine = null;
        }

        ShootingPattern finalPattern = patternType;
        
        if (finalPattern == ShootingPattern.Random_Selection && randomPatterns.Count > 0)
        {
            finalPattern = randomPatterns[Random.Range(0, randomPatterns.Count)];
        }
        
        // Inicia Corrotina ou Burst Instantâneo
        if (finalPattern == ShootingPattern.Spiral_Time)
        {
            currentPatternRoutine = StartCoroutine(ShootSpiralRoutine());
        }
        else
        {
            ShootBurst(finalPattern);
        }
    }
    
    // =================================================================
    // MÉTODOS DE BURST INSTANTÂNEO
    // =================================================================

    private void ShootBurst(ShootingPattern pattern)
    {
        switch (pattern)
        {
            case ShootingPattern.TowardsTarget:
                ShootSingle(GetAngleToTarget());
                break;
            case ShootingPattern.Spread_Fan:
                ShootSpreadFan();
                break;
            case ShootingPattern.Cross_Plus:
                ShootCross(0f);
                break;
            case ShootingPattern.Cross_X:
                ShootCross(45f);
                break;
            case ShootingPattern.Circle_360:
                ShootCircle360();
                break;
            case ShootingPattern.Column_Wall:
                ShootColumnWall();
                break;
            case ShootingPattern.Rotating_Cross_Column:
                ShootRotatingCrossColumn();
                break;
        }
    }

    private float GetAngleToTarget()
    {
        if (playerTarget == null) return 0f;
        Vector2 direction = playerTarget.position - firePoint.position;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void ShootSingle(float angle)
    {
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        enemyShooter.InstantiateProjectile(firePoint.position, rotation, projectileDamage, projectileSpeed, projectileLifetime);
    }
    
    // Padrão: Leque (Fan)
    private void ShootSpreadFan()
    {
        float startAngle = GetAngleToTarget() - (spreadAngle / 2f);
        float angleStep = spreadAngle / (burstCount > 1 ? burstCount - 1 : 1);
        
        for (int i = 0; i < burstCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            ShootSingle(angle);
        }
    }

    // Padrão: Coluna com Abertura (Wall)
    private void ShootColumnWall()
    {
        int finalEscapeIndex = randomizeEscapeIndex ? Random.Range(0, totalProjectilesInColumn) : fixedEscapeIndex;
        
        float baseAngle = GetAngleToTarget();
        Quaternion forwardRotation = Quaternion.Euler(0, 0, baseAngle);
        Vector3 perpendicularDirection = forwardRotation * Vector3.up; 

        float totalSpacing = (totalProjectilesInColumn - 1) * columnSpacing;
        float halfSpawnLength = totalSpacing / 2f;

        for (int i = 0; i < totalProjectilesInColumn; i++)
        {
            if (i == finalEscapeIndex) continue; 

            float currentOffset = (i * columnSpacing) - halfSpawnLength;
            Vector3 spawnPosition = firePoint.position + (perpendicularDirection * currentOffset);

            enemyShooter.InstantiateProjectile(spawnPosition, forwardRotation, projectileDamage, projectileSpeed, projectileLifetime);
        }
    }
    
    // Padrão: Cruz Rotatória de Colunas (Escudo Estático)
    private void ShootRotatingCrossColumn()
    {
        Vector3 bossCenter = firePoint.position;
        
        // A rotação do padrão é baseada na rotação ATUAL do objeto pai
        Quaternion crossRotation = shieldRotationParent != null ? shieldRotationParent.rotation : Quaternion.identity;

        if (shieldRotationParent == null)
        {
            Debug.LogError("Shield Rotation Parent não referenciado. Padrão Rotatório falhou.");
            return;
        }

        // As 4 direções da Cruz: 0, 90, 180, 270 graus
        for (int arm = 0; arm < 4; arm++)
        {
            float armAngle = (arm * 90f);
            
            // Rotação do Braço: Rotação fixa (0/90/180/270) + Rotação acumulada do Parent
            Quaternion armRotation = crossRotation * Quaternion.Euler(0, 0, armAngle);
            
            int finalEscapeIndex = Random.Range(0, totalProjectilesInColumn);
            
            Vector3 perpendicularDirection = armRotation * Vector3.up; 
            
            float totalSpacing = (totalProjectilesInColumn - 1) * columnSpacing;
            float halfSpawnLength = totalSpacing / 2f;

            for (int i = 0; i < totalProjectilesInColumn; i++)
            {
                if (i == finalEscapeIndex) continue; 

                float currentOffset = (i * columnSpacing) - halfSpawnLength;
                
                // Posição de Spawn: Centro do Boss + (Offset Perpendicular)
                Vector3 spawnPosition = bossCenter + (perpendicularDirection * currentOffset);

                // Instancia com VELOCIDADE ZERO e usa a versão que RETORNA o GameObject
                GameObject shieldGO = enemyShooter.InstantiateProjectileGo(
                    spawnPosition, 
                    armRotation, 
                    projectileDamage, 
                    0f, // VELOCIDADE ZERO: Projétil não se move para a frente
                    projectileLifetime
                );
                
                // CRUCIAL: TORNAR O PROJÉTIL FILHO DO OBJETO QUE ESTÁ GIRANDO
                if (shieldGO != null)
                {
                     shieldGO.transform.SetParent(shieldRotationParent);
                }
            }
        }
    }

    // Padrão: Cruz Fixa (+ ou X)
    private void ShootCross(float initialOffset)
    {
        for (int i = 0; i < burstCount; i++)
        {
            float angle = initialOffset + (360f / burstCount) * i;
            ShootSingle(angle);
        }
    }
    
    // Padrão: Círculo 360 (Bullet Hell)
    private void ShootCircle360()
    {
        float angleStep = 360f / burstCount;
        float baseAngle = GetAngleToTarget(); 

        for (int i = 0; i < burstCount; i++)
        {
            float angle = baseAngle + (angleStep * i);
            ShootSingle(angle);
        }
    }


    // =================================================================
    // MÉTODOS DE CORROTINA (Espiral Periódica)
    // =================================================================

    private IEnumerator ShootSpiralRoutine()
    {
        float totalRotationRequired = requiredRotations * 360f;
        float currentRotation = 0f;
        
        bool runInfinite = infiniteSpiral; 

        spiralCurrentAngle = GetAngleToTarget(); 

        while (runInfinite || currentRotation < totalRotationRequired)
        {
            // O passo de rotação é definido por spiralRotationStep (CORRIGIDO)
            spiralCurrentAngle += spiralRotationStep;
            
            if (!runInfinite) 
            {
                 currentRotation += spiralRotationStep;
            }

            ShootSingle(spiralCurrentAngle);
            yield return new WaitForSeconds(spiralInterval);
        }

        // FIM DO CICLO: CEDE O CONTROLE DE VOLTA
        currentPatternRoutine = null;
    }
}
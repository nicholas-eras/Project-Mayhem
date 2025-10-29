using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;

public enum ShootingPattern
{
    TowardsTarget,
    Spread_Fan,
    Cross_Plus,
    Cross_X,
    Cross_Swap,        // NOVO: Alterna entre + e X
    Circle_360,
    Spiral_Time,
    Column_Wall,
    Rotating_Cross_Column,
    Random_Selection
}

public class PatternShooter : MonoBehaviour
{
    [Header("1. Seleção e Controle")]
    [Tooltip("O padrão de tiro a ser usado (ou Random_Selection).")]
    [SerializeField] private ShootingPattern patternType = ShootingPattern.TowardsTarget;
    [SerializeField] private List<ShootingPattern> randomPatterns;
    
    [Header("1.1 Controle de Random Selection")]
    [Tooltip("Tempo (segundos) que o padrão aleatório permanece antes de sortear outro.")]
    [SerializeField] private float patternSwitchTime = 5f;
    [Tooltip("Se true, cada padrão sorteado dura patternSwitchTime. Se false, sorteia a cada disparo.")]
    [SerializeField] private bool usePatternSwitchTimer = true;

    [Header("2. Configuração de Burst/Geometria")]
    [SerializeField] private int burstCount = 8;
    [SerializeField] private float spreadAngle = 90f;

    [Header("3. Configuração de Colunas & Cruz")]
    [SerializeField] private int totalProjectilesInColumn = 7;
    [SerializeField] private float columnSpacing = 0.8f;
    [SerializeField] private bool randomizeEscapeIndex = true;
    [Range(0, 6)]
    [SerializeField] private int fixedEscapeIndex = 3; 

    [Header("4. Configuração de Espiral/Ciclo")]
    [SerializeField] private bool infiniteSpiral = false;
    [Range(1, 10)]
    [SerializeField] private int requiredRotations = 1;
    [SerializeField] private float spiralInterval = 0.1f;
    [SerializeField] private float spiralRotationStep = 15f;
    
    [Header("5. Configuração do Escudo Rotatório")]
    [SerializeField] private Transform shieldRotationParent; 
    
    [Header("6. Referências")]
    [SerializeField] private EnemyShooter enemyShooter;
    [SerializeField] private Transform firePoint;

    private Transform playerTarget;
    private float projectileDamage;
    private float projectileSpeed;
    private float projectileLifetime;
    
    public Coroutine currentPatternRoutine { get; private set; }
    private float spiralCurrentAngle = 0f;
    private bool shieldIsActive = false;
    private List<GameObject> activeShieldProjectiles = new List<GameObject>();
    
    // Controle de troca de padrão aleatório
    private ShootingPattern currentRandomPattern;
    private float patternSwitchTimer = 0f;
    private bool needsNewPattern = true;
    
    // Controle de alternância Cross_Swap
    private bool crossSwapIsPlus = true;

    void Start()
    {
        if (enemyShooter == null) { enemyShooter = GetComponent<EnemyShooter>(); }
        playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (enemyShooter == null || firePoint == null)
        {
            Debug.LogError("PatternShooter precisa de EnemyShooter e FirePoint configurados!", this);
            enabled = false;
        }
        
        patternSwitchTimer = patternSwitchTime;
    }

    void Update()
    {
        if (patternType == ShootingPattern.Random_Selection && usePatternSwitchTimer)
        {
            patternSwitchTimer -= Time.deltaTime;
            
            if (patternSwitchTimer <= 0f)
            {
                needsNewPattern = true;
                patternSwitchTimer = patternSwitchTime;
            }
        }
    }

    private void OnDisable()
    {
        ClearActiveShield();
    }

    private void OnDestroy()
    {
        ClearActiveShield();
    }

    private float GetProjectileLifetime(ShootingPattern pattern)
    {
        if (pattern == ShootingPattern.Rotating_Cross_Column)
        {
            return -1f;
        }
        return projectileLifetime;
    }

    public void ShootPattern(float damage, float speed, float lifetime)
    {
        projectileDamage = damage;
        projectileSpeed = speed;
        projectileLifetime = lifetime;

        if (currentPatternRoutine != null && patternType == ShootingPattern.Spiral_Time) return;

        if (currentPatternRoutine != null)
        {
            StopCoroutine(currentPatternRoutine);
            currentPatternRoutine = null;
        }

        ShootingPattern finalPattern = patternType;

        if (finalPattern == ShootingPattern.Random_Selection && randomPatterns.Count > 0)
        {
            if (usePatternSwitchTimer)
            {
                if (needsNewPattern)
                {
                    ShootingPattern oldPattern = currentRandomPattern;
                    currentRandomPattern = randomPatterns[Random.Range(0, randomPatterns.Count)];
                    needsNewPattern = false;
                                        
                    if (oldPattern == ShootingPattern.Rotating_Cross_Column && 
                        currentRandomPattern != ShootingPattern.Rotating_Cross_Column && 
                        shieldIsActive)
                    {
                        ClearActiveShield();
                    }
                }
                
                finalPattern = currentRandomPattern;
            }
            else
            {
                finalPattern = randomPatterns[Random.Range(0, randomPatterns.Count)];
                
                if (finalPattern != ShootingPattern.Rotating_Cross_Column && shieldIsActive)
                {
                    ClearActiveShield();
                }
            }
        }
        
        float finalLifetime = GetProjectileLifetime(finalPattern);
        
        if (finalPattern == ShootingPattern.Spiral_Time)
        {
            currentPatternRoutine = StartCoroutine(ShootSpiralRoutine());
        }
        else
        {
            ShootBurst(finalPattern, finalLifetime);
        }
    }
    
    private void ShootBurst(ShootingPattern pattern, float lifetime)
    {
        projectileLifetime = lifetime;
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
            case ShootingPattern.Cross_Swap:
                ShootCrossSwap();
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

    private void ShootRotatingCrossColumn()
    {
        if (shieldIsActive)
        {
            return;
        }

        if (shieldRotationParent == null)
        {
            Debug.LogError("[PatternShooter] shieldRotationParent não configurado!", this);
            return;
        }

        Vector3 bossCenter = firePoint.position;
        Quaternion crossRotation = shieldRotationParent.rotation;

        for (int arm = 0; arm < 4; arm++)
        {
            float armAngle = arm * 90f;
            Quaternion armRotation = crossRotation * Quaternion.Euler(0, 0, armAngle);
            Vector3 perpendicularDirection = armRotation * Vector3.up;

            for (int i = 1; i < totalProjectilesInColumn; i++)
            {

                float currentOffset = i * columnSpacing;
                Vector3 spawnPosition = bossCenter + (perpendicularDirection * currentOffset);

                GameObject shieldGO = enemyShooter.InstantiateProjectileGo(
                    spawnPosition,
                    armRotation,
                    projectileDamage,
                    0f,
                    projectileLifetime
                );

                if (shieldGO != null)
                {
                    // 1. Pegue o NetworkObject do escudo
                    NetworkObject netObj = shieldGO.GetComponent<NetworkObject>();

                    // 2. Spawne o objeto na rede PRIMEIRO
                    if (netObj != null)
                    {
                        netObj.Spawn();
                    }
                    else
                    {
                        Debug.LogError("O prefab do escudo (shieldGO) não tem um NetworkObject!");
                    }

                    // 3. AGORA é seguro definir o parent
                    shieldGO.transform.SetParent(shieldRotationParent);

                    activeShieldProjectiles.Add(shieldGO);
                }
            }
        }
        
        shieldIsActive = true;
    }

    public void ClearActiveShield()
    {
        if (!shieldIsActive && activeShieldProjectiles.Count == 0) return;
                
        foreach (GameObject proj in activeShieldProjectiles)
        {
            if (proj != null) Destroy(proj);
        }
        
        activeShieldProjectiles.Clear();
        shieldIsActive = false;
    }

    private void ShootCross(float initialOffset)
    {
        for (int i = 0; i < burstCount; i++)
        {
            float angle = initialOffset + (360f / burstCount) * i;
            ShootSingle(angle);
        }
    }
    
    private void ShootCrossSwap()
    {
        // Alterna entre + (0°) e X (45°)
        float offset = crossSwapIsPlus ? 0f : 45f;
        ShootCross(offset);
        
        // Inverte para o próximo disparo
        crossSwapIsPlus = !crossSwapIsPlus;
    }
    
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

    private IEnumerator ShootSpiralRoutine()
    {
        float totalRotationRequired = requiredRotations * 360f;
        float currentRotation = 0f;
        bool runInfinite = infiniteSpiral; 
        spiralCurrentAngle = GetAngleToTarget(); 

        while (runInfinite || currentRotation < totalRotationRequired)
        {
            spiralCurrentAngle += spiralRotationStep;
            
            if (!runInfinite) 
            {
                 currentRotation += spiralRotationStep;
            }

            ShootSingle(spiralCurrentAngle);
            yield return new WaitForSeconds(spiralInterval);
        }

        currentPatternRoutine = null;
    }
}
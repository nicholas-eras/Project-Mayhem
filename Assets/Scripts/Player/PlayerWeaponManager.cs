using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("Configuração")]
    public float placementRadius = 1.2f;
    public Transform weaponHolder;

    private List<GameObject> equippedWeapons = new List<GameObject>(); // <-- Lista crucial

    // --- BÔNUS DE UPGRADE ---
    // (Mantendo estes aqui, caso você volte a usá-los no futuro
    // ou precise deles para outras lógicas)
    [HideInInspector] public float damageMultiplier = 1.0f;
    [HideInInspector] public float fireRateMultiplier = 1.0f;
    [HideInInspector] public float rangeMultiplier = 1.0f;

    [Header("Debug")]
    public List<GameObject> weaponPrefabsForTesting;
    private int testWeaponIndex = 0;

    private void Awake()
    {
        if (weaponHolder == null)
        {
            weaponHolder = transform.Find("WeaponHolder");
            if (weaponHolder == null) Debug.LogError("WeaponHolder não encontrado!", this);
        }
    }

    /// <summary>
    /// Chamado pelos Spawners para dar a arma inicial.
    /// </summary>
    public void InitializeStartingWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning($"[PWM] {gameObject.name} não recebeu arma inicial.");
            return;
        }

        // Limpa armas antigas E a lista antes de adicionar a inicial
        foreach (Transform child in weaponHolder) Destroy(child.gameObject);
        equippedWeapons.Clear();

        // Usa o método AddWeapon para garantir consistência
        AddWeaponInternal(weaponPrefab); 
    }

    /// <summary>
    /// Chamado pelos Upgrades para adicionar uma NOVA arma.
    /// </summary>
    public void AddWeapon(GameObject weaponPrefab)
    {
        // Apenas adiciona, não limpa as existentes
        AddWeaponInternal(weaponPrefab);
    }

    /// <summary>
    /// Método interno que faz o trabalho real de instanciar,
    /// configurar e adicionar a arma à lista.
    /// </summary>
    private void AddWeaponInternal(GameObject weaponPrefab) // <-- NOVO MÉTODO INTERNO
    {
        if (weaponHolder == null || weaponPrefab == null) return;

        // 1. Cria a nova arma
        GameObject newWeapon = Instantiate(weaponPrefab, weaponHolder.position, Quaternion.identity);
        newWeapon.transform.SetParent(weaponHolder); // Define como filho

        // 2. OBTÉM O WeaponController
        WeaponController weaponController = newWeapon.GetComponent<WeaponController>();

        if (weaponController != null)
        {
            // 3. CLONAR O ScriptableObject (se existir)
            // Essencial para upgrades individuais
            if (weaponController.weaponData != null)
            {
                 WeaponData originalData = weaponController.weaponData;
                 WeaponData clonedData = Instantiate(originalData); // Cria cópia
                 weaponController.weaponData = clonedData; // Usa a cópia
            }

            // --- 4. CHAMA O SETUP! --- // <-- CORREÇÃO ESSENCIAL
            // Passa a referência deste manager (o "Chefe") para a arma
            weaponController.Setup(this); 
        }

        // --- 5. ADICIONA À LISTA! --- // <-- CORREÇÃO ESSENCIAL
        equippedWeapons.Add(newWeapon); 

        // --- 6. REARRANJA! --- // <-- CORREÇÃO ESSENCIAL
        RearrangeWeapons(); 
    }


    /// <summary>
    /// Método chamado pela UI para obter os ícones.
    /// </summary>
    public List<Sprite> GetEquippedWeaponIcons()
    {
        List<Sprite> icons = new List<Sprite>();
        foreach (GameObject weaponGO in equippedWeapons)
        {
            if (weaponGO == null) continue; // Segurança

            WeaponController wc = weaponGO.GetComponent<WeaponController>();
            if (wc != null && wc.mainSpriteRenderer != null && wc.mainSpriteRenderer.sprite != null)
            {
                icons.Add(wc.mainSpriteRenderer.sprite);
            }
            // else { Debug.LogWarning(...); } // Opcional: Avisar se faltar sprite
        }
        return icons;
    }
    
    /// <summary>
    /// Reorganiza as armas em um círculo.
    /// </summary>
    private void RearrangeWeapons()
    {
        int weaponCount = equippedWeapons.Count;
        if (weaponCount == 0) return;

        float angleStep = 360f / weaponCount;

        for (int i = 0; i < weaponCount; i++)
        {
            if (equippedWeapons[i] == null) continue; // Segurança

            float angle = i * angleStep;
            Vector3 weaponPosition = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad),
                0
            ) * placementRadius;
            
            equippedWeapons[i].transform.localPosition = weaponPosition;
        }
    }

    // --- DEBUG ---
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddTestWeapon();
        }
    }

    private void AddTestWeapon()
    {
        if (weaponPrefabsForTesting == null || weaponPrefabsForTesting.Count == 0) return;
        AddWeapon(weaponPrefabsForTesting[testWeaponIndex]);
        testWeaponIndex = (testWeaponIndex + 1) % weaponPrefabsForTesting.Count;
    }
    // --- FIM DEBUG ---
    
    // --- MÉTODOS DE UPGRADE ---
    // (Estes agora funcionam porque operam no WeaponData clonado)
    public void IncreaseDamage(float value) // Assumindo que 'value' é o valor a adicionar
    {
        foreach (GameObject weaponGO in equippedWeapons)
        {
            WeaponController weapon = weaponGO?.GetComponent<WeaponController>();
            if (weapon != null && weapon.weaponData != null)
            {
                weapon.weaponData.damage += value;
            }
        }
    }
    public void IncreaseFireRateMultiplier(float value) // Assumindo que 'value' é o valor a adicionar
    {
         foreach (GameObject weaponGO in equippedWeapons)
         {
             WeaponController weapon = weaponGO?.GetComponent<WeaponController>();
             if (weapon != null && weapon.weaponData != null)
             {
                 // CUIDADO: FireRateInterval MAIOR = TIRO MAIS LENTO
                 // Para aumentar a cadência, você precisa DIMINUIR o intervalo
                 // ou AUMENTAR uma variável de "Tiros por Segundo"
                 // A forma mais segura é AUMENTAR a frequência (Tiros por Segundo)
                 weapon.weaponData.fireRateInterval += value; // Se 'value' for positivo, a cadência AUMENTA
             }
         }
    }
    public void IncreaseRangeMultiplier(float value) // Assumindo que 'value' é o valor a adicionar
    {
         foreach (GameObject weaponGO in equippedWeapons)
         {
             WeaponController weapon = weaponGO?.GetComponent<WeaponController>();
             if (weapon != null && weapon.weaponData != null)
             {
                 weapon.weaponData.range += value;
             }
         }
    }
    // --- FIM UPGRADES ---
    
    /// <summary>
    /// Método chamado pela UI para obter a arma principal (para stats).
    /// </summary>
    public GameObject GetEquippedWeapon(int index)
    {
        if (index >= 0 && index < equippedWeapons.Count && equippedWeapons[index] != null)
        {
            return equippedWeapons[index];
        }
        return null;
    }
}
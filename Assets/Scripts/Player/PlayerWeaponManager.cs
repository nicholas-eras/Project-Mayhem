using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("O raio do círculo onde as armas serão posicionadas.")]
    public float placementRadius = 1.2f;
    
    [Tooltip("O objeto filho que servirá como o centro de posicionamento.")]
    public Transform weaponHolder;

    // A lista de armas que o jogador possui atualmente.
    private List<GameObject> equippedWeapons = new List<GameObject>();

    [Header("Debug")]
    [Tooltip("Lista de prefabs de armas para adicionar com a tecla de atalho.")]
    public List<GameObject> weaponPrefabsForTesting;
    private int testWeaponIndex = 0;

    [Header("Configuração")]
    [Tooltip("O prefab da arma que o jogador começa o jogo. (Ex: Pistol)")]
    [SerializeField] private GameObject startingWeaponPrefab; // <-- NOVO CAMPO

    void Start()
    {
        if (startingWeaponPrefab != null)
        {
            AddWeapon(startingWeaponPrefab);
        }
    }

    void Update()
    {
        // DEBUG: Pressione "espaço" para adicionar uma nova arma para teste.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddTestWeapon();
        }
    }

    public void AddWeapon(GameObject weaponPrefab)
    {
        if (weaponHolder == null) return;

        // 1. Cria a nova arma
        GameObject newWeapon = Instantiate(weaponPrefab, weaponHolder.position, Quaternion.identity);
        newWeapon.transform.SetParent(weaponHolder);

        // 2. OBTÉM O WeaponController
        WeaponController weaponController = newWeapon.GetComponent<WeaponController>();

        if (weaponController != null && weaponController.weaponData != null)
        {
            // 3. CRUCIAL: CLONAR O ScriptableObject
            // Cria uma cópia temporária do Asset para que as alterações não sejam permanentes.
            // Assumimos que WeaponData herda de ScriptableObject.
            WeaponData originalData = weaponController.weaponData;
            WeaponData clonedData = Instantiate(originalData);
            weaponController.weaponData = clonedData;
        }

        equippedWeapons.Add(newWeapon);
        RearrangeWeapons();
    }
    
    private void RearrangeWeapons()
    {
        int weaponCount = equippedWeapons.Count;
        if (weaponCount == 0) return;

        float angleStep = 360f / weaponCount;

        for (int i = 0; i < weaponCount; i++)
        {
            float angle = i * angleStep;
            Vector3 weaponPosition = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad),
                0
            ) * placementRadius;
            
            // Aplica a posição local em relação ao "suporte" de armas.
            equippedWeapons[i].transform.localPosition = weaponPosition;
        }
    }

    private void AddTestWeapon()
    {
        if (weaponPrefabsForTesting.Count == 0) return;

        AddWeapon(weaponPrefabsForTesting[testWeaponIndex]);
        testWeaponIndex = (testWeaponIndex + 1) % weaponPrefabsForTesting.Count;
    }
    
    // 1. AUMENTO DE DANO
    public void IncreaseDamage(float percentage)
    {
        // O valor 'percentage' é o multiplicador de aumento (ex: 0.1 para 10%).
        float multiplier = percentage; 

        foreach (GameObject weaponGO in equippedWeapons)
        {
            // Tenta obter o WeaponController.
            WeaponController weapon = weaponGO.GetComponent<WeaponController>();
            
            if (weapon != null && weapon.weaponData != null)
            {
                // Aplica o multiplicador ao campo 'damage' do WeaponData (que deve ser a cópia clonada).
                weapon.weaponData.damage += multiplier;                
            }
        }
    }
    
    // 2. AUMENTO DE CADÊNCIA (Fire Rate)
    public void IncreaseFireRateMultiplier(float percentage)
    {
        // Aumentar o FireRate em 50% significa aumentar o multiplicador em 1.5.
        // O ideal é que o upgrade seja aditivo ou que o valor base da arma seja o ponto de partida.
        
        // COMO CORRIGIR O PROBLEMA DE MULTIPLICAÇÃO EXAGERADA:
        float multiplier = 1f + percentage; // Ex: 1.15 para 15%

        foreach (GameObject weaponGO in equippedWeapons)
        {
            WeaponController weapon = weaponGO.GetComponent<WeaponController>();
            if (weapon != null && weapon.weaponData != null)
            {
                // O código original estava correto para aumentar uma frequência, mas causa o exagero.
                // Para consertar o exagero, vamos aplicar a mudança de forma mais controlada.
                
                // Se você quer que o upgrade seja ADITIVO (e não cumulativo no código):
                // Esta solução exige que você tenha o valor base em algum lugar.
                
                // Solução Padrão: Multiplicar (e aceitar que o aumento é grande se o base for alto)
                // weapon.weaponData.fireRate *= multiplier; 

                // OU, a solução mais limpa: Se você quer que o efeito pareça ADITIVO (ex: +4 Fire Rate):
                weapon.weaponData.fireRate += percentage; // Se percentage for 1 (100%)                
            }
        }
    }

    // 3. AUMENTO DE ALCANCE
    public void IncreaseRangeMultiplier(float percentage)
    {
        foreach (GameObject weaponGO in equippedWeapons)
        {
            WeaponController weapon = weaponGO.GetComponent<WeaponController>();
            if (weapon != null && weapon.weaponData != null)
            {
                weapon.weaponData.range += percentage;
            }
        }
    }
    
    public GameObject GetEquippedWeapon(int index)
    {
        if (index >= 0 && index < equippedWeapons.Count)
        {
            return equippedWeapons[index];
        }
        return null;
    }
}
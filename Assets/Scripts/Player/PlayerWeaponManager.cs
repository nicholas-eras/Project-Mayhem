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

        // Cria a nova arma e a torna filha do "suporte" de armas.
        GameObject newWeapon = Instantiate(weaponPrefab, weaponHolder.position, Quaternion.identity);
        newWeapon.transform.SetParent(weaponHolder);

        equippedWeapons.Add(newWeapon);

        // Reorganiza TODAS as armas em um círculo estático.
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
}
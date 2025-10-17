using System.Collections.Generic;
using UnityEngine;

public class LootDropper : MonoBehaviour
{
    // Uma classe interna para organizar nossa tabela de loot.
    [System.Serializable]
    public class LootDrop
    {
        [Tooltip("O prefab da moeda (pequena, grande, etc.)")]
        public GameObject coinPrefab;

        [Tooltip("Chance de este tipo de moeda dropar (0 = nunca, 1 = sempre)")]
        [Range(0f, 1f)]
        public float dropChance = 1f;

        [Tooltip("Quantidade MÍNIMA de moedas a dropar, se a chance for bem-sucedida")]
        public int minAmount = 1;

        [Tooltip("Quantidade MÁXIMA de moedas a dropar")]
        public int maxAmount = 1;
    }

    [Tooltip("A lista de todos os possíveis drops para este inimigo.")]
    public List<LootDrop> lootTable;

    // Esta função será chamada quando o inimigo morrer.
    public void DropLoot()
    {
        // Passa por cada tipo de drop na nossa tabela.
        foreach (var drop in lootTable)
        {
            // Rola um "dado" para ver se este drop acontece.
            if (Random.value <= drop.dropChance)
            {
                // Se acontecer, calcula quantas moedas criar.
                int amountToDrop = Random.Range(drop.minAmount, drop.maxAmount + 1);

                for (int i = 0; i < amountToDrop; i++)
                {
                    // Cria a moeda na posição do inimigo.
                    Instantiate(drop.coinPrefab, transform.position, Quaternion.identity);
                }
            }
        }
    }
}
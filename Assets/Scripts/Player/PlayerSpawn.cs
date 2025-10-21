using UnityEngine;

// Script vazio, serve apenas para marcar a posição de spawn do Player
public class PlayerSpawn : MonoBehaviour
{
    // Opcional: para diferenciar o spawn padrão do spawn de boss
    public bool isBossSpawnPoint = false;

    // Garante que o Game Object que o contém seja um Spawn Point
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        if (isBossSpawnPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.7f);
        }
    }
}
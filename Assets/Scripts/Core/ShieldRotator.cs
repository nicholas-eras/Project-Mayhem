// NOVO SCRIPT: ShieldRotator.cs
using UnityEngine;

// Anexe este script ao Boss, ou crie um objeto filho no Boss para isto.
public class ShieldRotator : MonoBehaviour
{
    // A rotação angular que o padrão irá acumular (graus por segundo)
    [SerializeField] private float rotationSpeed = 100f;

    void Update()
    {
        // Gira o objeto no eixo Z
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}
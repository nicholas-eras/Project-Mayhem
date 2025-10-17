using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;

    [Tooltip("Volume do som (0 a 1).")]
    [Range(0f, 1f)] // Cria um slider no Inspector de 0 a 1
    public float volume = .5f; // Valor padrão é 1 (volume máximo)

    [Tooltip("Tom/Velocidade do som (1 = normal).")]
    [Range(0.1f, 3f)] // Cria um slider de 0.1 a 3
    public float pitch = 1f; // Valor padrão é 1 (tom normal)
}
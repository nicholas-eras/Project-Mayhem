using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimationData", menuName = "Game/Sprite Animation Data")]
public class AnimationData : ScriptableObject
{
    [Tooltip("A lista de sprites que compõem esta animação.")]
    public Sprite[] frames;

    [Tooltip("Quadros por segundo. Controla a velocidade da animação.")]
    public float framesPerSecond = 10f;

    [Tooltip("Se a animação deve repetir.")]
    public bool loop = true;
}
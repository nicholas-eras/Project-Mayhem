using UnityEngine;
using UnityEngine.Events; // Necessário para usar UnityEvents

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHealth = 10f;
    private float currentHealth;

    // Um evento que podemos usar para notificar outros scripts quando este objeto morre.
    // Isso é ótimo porque o sistema de vida não precisa saber sobre kill streak, score, etc.
    // Ele apenas anuncia: "Eu morri!", e quem estiver interessado que ouça.
    public UnityEvent OnDeath;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        // Adicionar feedback de dano aqui!
        // Ex: Mudar a cor do sprite para branco por um instante
        // Ex: Tocar um som de "acerto"

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Invoca o evento OnDeath. Qualquer função que registramos nele será chamada.
        OnDeath?.Invoke();

        // Adicionar feedback de morte!
        // Ex: Instanciar um Prefab de explosão

        // Destrói o GameObject ao qual este script está anexado.
        Destroy(gameObject);
    }
}
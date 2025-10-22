using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Exemplo de um novo script que você pode criar:
public class PlayerStatusEffects : MonoBehaviour
{
    private PoisonEffect poisonEffect;
    private FireEffect fireEffect;
    // ... outros efeitos ...

    void Awake()
    {
        poisonEffect = GetComponent<PoisonEffect>();
        fireEffect = GetComponent<FireEffect>();
    }

    // O método principal que será chamado pelo HealthSystem
    public void CureAllStatusEffects()
    {
        // Chame o método de "cura" em cada script de efeito
        poisonEffect?.Cure(); 
        fireEffect?.Cure();
        // ...
    }
}
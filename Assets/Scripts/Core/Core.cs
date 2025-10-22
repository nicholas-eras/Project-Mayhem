// Este arquivo define a estrutura do nosso "pacote de dano".
// Não precisa herdar de MonoBehaviour.

// Um enum para definir nossos tipos de dano de forma clara e segura.
public enum DamageType
{
    Standard,      // Dano normal que ativa i-frames.
    Unblockable,   // Dano de alta prioridade que IGNORA i-frames.
    Poison,        // (Exemplo futuro) Dano ao longo do tempo.
    Fire,           // (Exemplo futuro) Outro tipo de dano.
    DamageZone
}

public struct DamageInfo
{
    public float damageAmount;
    public DamageType type;

    // Um "construtor" para facilitar a criação de novos pacotes de dano.
    public DamageInfo(float damage, DamageType damageType = DamageType.Standard)
    {
        damageAmount = damage;
        type = damageType;
    }
}
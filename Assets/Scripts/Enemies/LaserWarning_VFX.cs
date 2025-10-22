using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Este script é vazio, o objeto é puramente visual.
// O BossLaserAttack gerencia sua destruição.

public class LaserWarning_VFX : MonoBehaviour
{
    // Este objeto DEVE ter um SpriteRenderer e um BoxCollider2D (Is Trigger)
    // que cobre a linha de ataque, mas o BoxCollider2D não deve ter scripts de dano anexados.
    
    // Configuração do SpriteRenderer:
    // - Sprite: Linha fina e transparente (vermelha ou cinza)
    // - Sorting Order: Deve ser ligeiramente mais alto que o chão, mas menor que o Boss.
}

using UnityEngine;

// Classe estática para guardar os dados do lobby entre as cenas
public static class LobbyData
{
    // Guarda o estado de cada slot (Empty, Human, Bot)
    public static SlotState[] SlotStates { get; set; } = new SlotState[4];
    // Guarda o ID da skin escolhida para cada slot
    public static int[] SkinIDs { get; set; } = new int[4];

    // Poderíamos adicionar nomes de jogadores, etc., se necessário
}
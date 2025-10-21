using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Importar para escutar eventos de cena
using System; // Para o System.Serializable

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Fontes de Áudio")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Biblioteca de Áudio")]
    public List<Sound> soundList;

    private string currentTrackName = string.Empty; // Rastreia a música atual
    
    void Awake()
    {
        // 1. Lógica do Singleton Persistente
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
        }
        else
        {
            // Destrói duplicatas
            Destroy(gameObject);
        }
    }
    
    // ========================================================
    // FUNÇÕES PÚBLICAS DE CONTROLE
    // ========================================================

    public void PlaySFX(string name)
    {
        Sound sound = FindSound(name);
        if (sound == null)
        {
            Debug.LogWarning("SFX: " + name + " não encontrado!");
            return;
        }

        // Aplica o pitch à fonte de SFX
        sfxSource.pitch = sound.pitch;
        
        // PlayOneShot é ideal para SFX, pois permite sobreposições e usa o volume do Sound
        sfxSource.PlayOneShot(sound.clip, sound.volume); 
    }

    // --------------------------------------------------------
    // FUNÇÃO DE BUSCA
    // --------------------------------------------------------

    private Sound FindSound(string name)
    {
        return soundList.Find(s => s.name == name);
    }
}
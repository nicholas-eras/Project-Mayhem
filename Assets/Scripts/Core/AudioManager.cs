using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Fontes de Áudio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Biblioteca de Áudio")]
    public List<Sound> soundList;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayMusic("Theme"); 
    }

    public void PlayMusic(string name)
    {
        Sound sound = FindSound(name);
        if (sound == null)
        {
            Debug.LogWarning("Música: " + name + " não encontrada!");
            return;
        }
        
        // APLICA OS VALORES DA NOSSA LISTA
        musicSource.clip = sound.clip;
        musicSource.volume = sound.volume; // <<<--- APLICA O VOLUME
        musicSource.pitch = sound.pitch;   // <<<--- APLICA O PITCH
        musicSource.Play();
    }

    public void PlaySFX(string name)
    {
        Sound sound = FindSound(name);
        if (sound == null)
        {
            Debug.LogWarning("SFX: " + name + " não encontrado!");
            return;
        }

        // APLICA O PITCH À FONTE DE SFX
        sfxSource.pitch = sound.pitch;
        
        // PlayOneShot tem uma sobrecarga que aceita um segundo argumento para o volume!
        // Isso é perfeito para SFX, pois não altera o volume base da fonte.
        sfxSource.PlayOneShot(sound.clip, sound.volume); // <<<--- USA O VOLUME AQUI
    }

    private Sound FindSound(string name)
    {
        return soundList.Find(s => s.name == name);
    }
}
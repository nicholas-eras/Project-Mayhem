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
    
    // NOVO: Rastreamento de sons em loop por chave única (nome_idinstancia)
    private Dictionary<string, AudioSource> loopingSources = new Dictionary<string, AudioSource>(); 

    [Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }
    
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
    
    /// <summary>
    /// Toca um SFX em uma AudioSource dedicada que pode ser rastreada e parada.
    /// Usa o 'uniqueKey' para permitir que múltiplos sons com o mesmo nome toquem ao mesmo tempo.
    /// </summary>
    public void PlayLoopingSFX(string uniqueKey)
    {
        // 1. Extrai o nome real do som (removendo o ID da instância)
        string soundName = uniqueKey.Split('_')[0]; 
        Sound sound = FindSound(soundName);

        if (sound == null)
        {
            Debug.LogWarning("Looping SFX: " + uniqueKey + " não encontrado!");
            return;
        }

        // 2. Se a chave única já existe no rastreamento, apenas garante que está tocando.
        if (loopingSources.ContainsKey(uniqueKey))
        {
            if (!loopingSources[uniqueKey].isPlaying)
            {
                loopingSources[uniqueKey].Play();
            }
            return;
        }

        // 3. Cria uma nova AudioSource (para garantir que ele possa tocar em loop e ser parado)
        GameObject soundObject = new GameObject($"Looping_SFX_{uniqueKey}");
        soundObject.transform.SetParent(transform); // Faz o AudioManager ser o pai

        AudioSource loopSource = soundObject.AddComponent<AudioSource>();
        loopSource.clip = sound.clip;
        loopSource.volume = sound.volume;
        loopSource.pitch = sound.pitch;
        loopSource.loop = true; // Configura para tocar em loop

        // 4. Inicia e rastreia o som
        loopSource.Play();
        loopingSources.Add(uniqueKey, loopSource);
    }
    
    /// <summary>
    /// Para um som em loop usando sua chave única (nome_idinstancia) e destrói sua AudioSource.
    /// </summary>
    public void StopSFX(string uniqueKey)
    {
        if (loopingSources.ContainsKey(uniqueKey))
        {
            AudioSource loopSource = loopingSources[uniqueKey];
            loopSource.Stop();
            loopingSources.Remove(uniqueKey);
            
            // Destrói o GameObject temporário que segurava a AudioSource
            Destroy(loopSource.gameObject); 
        }
        // Se a chave não for encontrada, tentamos parar o som normal (se ele não estiver em loop)
        else
        {
             // Caso a chave única seja o nome de um SFX simples (não loop), não faz nada aqui, 
             // pois PlaySFX usa PlayOneShot e não pode ser parado.
        }
    }

    // --------------------------------------------------------
    // FUNÇÃO DE BUSCA
    // --------------------------------------------------------

    private Sound FindSound(string name)
    {
        return soundList.Find(s => s.name == name);
    }
}

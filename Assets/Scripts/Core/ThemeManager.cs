using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Importar para escutar eventos de cena
using System; // Para o System.Serializable

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance;

    [Header("Fontes de Áudio")]
    [SerializeField] private AudioSource musicSource;

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
            
            // 2. Se inscreve para o evento de carregamento de cena
            SceneManager.sceneLoaded += OnSceneLoaded; 
        }
        else
        {
            // Destrói duplicatas
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        // 3. Remove a inscrição quando o objeto é destruído para evitar erros
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // Opcional: Inicia a música da primeira cena carregada
    void Start()
    {
        // Garante que a primeira cena (geralmente MainMenu ou MapSelect) tenha sua música.
        // O OnSceneLoaded cuidará disso, mas Start é uma segurança extra.
        // O nome da trilha deve ser igual ao nome da sua cena inicial.
        PlayMusic(SceneManager.GetActiveScene().name); 
    }

    // 4. Chamado automaticamente quando uma nova cena é carregada
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Usa o nome da cena (Ex: "Cyberpunk", "MapSelectScene") para buscar a trilha
        PlayMusic(scene.name);
    }

    // ========================================================
    // FUNÇÕES PÚBLICAS DE CONTROLE
    // ========================================================

    public void PlayMusic(string name)
    {
        // 1. Evita tocar a mesma música novamente
        if (musicSource.isPlaying && currentTrackName == name)
        {
            return;
        }
        
        // 2. Procura o Sound Asset na biblioteca
        Sound sound = FindSound(name);
        if (sound == null)
        {
            Debug.LogWarning($"Música: '{name}' não encontrada na Sound List. Verifique se o nome da trilha é igual ao nome da cena.");
            musicSource.Stop(); 
            currentTrackName = string.Empty;
            return;
        }
        
        // 3. Para a música anterior
        musicSource.Stop(); 

        // 4. Inicia a nova música
        musicSource.clip = sound.clip;
        musicSource.volume = sound.volume;
        musicSource.pitch = sound.pitch;
        musicSource.loop = true; // Música de fundo sempre loopa
        musicSource.Play();
        
        // 5. Atualiza o rastreador
        currentTrackName = name;
    }

    // --------------------------------------------------------
    // FUNÇÃO DE BUSCA
    // --------------------------------------------------------

    private Sound FindSound(string name)
    {
        return soundList.Find(s => s.name == name);
    }
}
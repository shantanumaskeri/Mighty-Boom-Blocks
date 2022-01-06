using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    public static AudioPlayer Instance;
    
    [SerializeField] private AudioSource[] audioSources;

    private void Start()
    {
        Instance = this;
    }

    public void PlayAudio(int sfxId)
    {
        audioSources[sfxId].Play();
    }
}

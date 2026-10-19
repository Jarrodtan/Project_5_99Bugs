using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioMixer MainMixer;
    public AudioMixerSnapshot menu;
    public AudioMixerSnapshot simu;

    public AudioSource bgm;
    public AudioSource ambience;
    public AudioSource sfx;

    public void Awake()
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

    public void Start()
    {
        if (menu != null)
        {
            menu.TransitionTo(0f);
        }
    }

    public void SwitchToSimu()
    {
        if (simu != null)
        {
            simu.TransitionTo(2.0f);
        }
        if (ambience != null && !ambience.isPlaying)
        {
            ambience.Play();
        }
    }

    public void PlayUIClick(AudioClip click)
    {
        if (sfx != null && click != null)
        {
            sfx.PlayOneShot(click);
        }
    }

    public void Play3DSound(AudioSource source, AudioClip clip)
    {
        if (source != null && clip != null)
        {
            source.pitch = Random.Range(0.9f, 1.1f);
            source.PlayOneShot(clip);
        }
    }
}

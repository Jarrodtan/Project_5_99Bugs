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

    public AudioClip starting;
    public AudioClip game;
       

    public void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject);  return; }
    }

    public void Start()
    {
        if (bgm != null && game != null)
        {
            bgm.clip = game;
            bgm.loop = true;
            bgm.Play();
        }
    }

    public void SwitchToSimu()
    {
        simu.TransitionTo(2.0f);
        if (bgm != null && game != null) {
            bgm.clip = game;
            bgm.Play();
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

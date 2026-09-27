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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SwitchToSimu()
    {
        simu.TransitionTo(2.0f);
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

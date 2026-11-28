using UnityEngine;

/// <summary>
/// Three saved volumes (0-1) from the Sound box: Music (BGM), Voice (mascot VO) and Sounds (animals,
/// river, ambience, buttons). The master volume stays at 1; each source uses its own slider.
/// </summary>
public static class AudioPrefs
{
    const string MusicKey = "EcoBalance.Volume.Music", VoiceKey = "EcoBalance.Volume.Voice", SoundsKey = "EcoBalance.Volume.Sounds";
    const string OldKey = "EcoBalance.Volume";   // the one slider before M3: used as the start value for all three

    static float Get(string key) => PlayerPrefs.GetFloat(key, PlayerPrefs.GetFloat(OldKey, 1f));

    public static float Music { get => Get(MusicKey); set { PlayerPrefs.SetFloat(MusicKey, value); Apply(); } }
    public static float Voice { get => Get(VoiceKey); set { PlayerPrefs.SetFloat(VoiceKey, value); Apply(); } }
    public static float Sounds { get => Get(SoundsKey); set { PlayerPrefs.SetFloat(SoundsKey, value); Apply(); } }

    static AudioManager baseFor;
    static float baseBgm = 1f, baseAmb = 1f, baseSfx = 1f;

    /// <summary>Pushes the volumes to the sources that keep playing (music, ambience, UI clicks, mascot).</summary>
    public static void Apply()
    {
        AudioListener.volume = 1f;
        var am = AudioManager.Instance;
        if (am != null)
        {
            if (baseFor != am)   // remember the audio team's own mix once, then scale it
            {
                baseFor = am;
                baseBgm = am.bgm != null ? am.bgm.volume : 1f;
                baseAmb = am.ambience != null ? am.ambience.volume : 1f;
                baseSfx = am.sfx != null ? am.sfx.volume : 1f;
            }
            if (am.bgm != null) am.bgm.volume = baseBgm * Music;
            if (am.ambience != null) am.ambience.volume = baseAmb * Sounds;
            if (am.sfx != null) am.sfx.volume = baseSfx * Sounds;
        }
        Mascot.ApplyVoiceVolume();
    }
}

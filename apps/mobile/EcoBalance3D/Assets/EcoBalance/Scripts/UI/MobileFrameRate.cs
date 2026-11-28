// [AI-GENERATED] Claude Code (Claude Opus 5.5) - reviewed by Tan Wei Leong
using UnityEngine;

/// <summary>
/// Android and iOS run at 30 FPS unless a frame rate is set, which made the tablet feel laggy next to
/// the PC. Asks for 60 on phones and tablets (the device drops lower by itself if it can't keep up).
/// </summary>
static class MobileFrameRate
{
    const int Target = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        if (!Application.isMobilePlatform) return;
        QualitySettings.vSyncCount = 0;   // vSync on mobile is ignored; targetFrameRate decides
        Application.targetFrameRate = Target;
    }
}

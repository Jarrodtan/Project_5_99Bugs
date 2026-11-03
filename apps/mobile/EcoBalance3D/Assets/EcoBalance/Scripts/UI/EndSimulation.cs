using UnityEngine;

public class EndSimulation : MonoBehaviour
{
    public DataLogging Logging;

    public void Quit()
    {
        if (Logging != null)
        {
            Logging.ExportCSV();
        }
        else
        {
            Debug.LogWarning("[DATA LOG] Export skipped: Logging reference is missing!");
        }

        Debug.Log("[SYSTEM] Exiting simulation...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

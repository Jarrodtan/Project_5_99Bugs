using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void GoToConfiguration()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SwitchToSimu();
        }
        SceneManager.LoadScene("ConfigurationScreen");
    }

    public void StartSimu()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SwitchToSimu();
        }
    }

}

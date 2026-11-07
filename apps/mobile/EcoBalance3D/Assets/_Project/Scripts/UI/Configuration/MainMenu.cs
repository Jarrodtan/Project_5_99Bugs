using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void GoToConfiguration()
    {
        SceneManager.LoadScene("ConfigurationScreen");
    }
}

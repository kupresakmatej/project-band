using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public void GoToBandManagement()
    {
        SceneManager.LoadScene("GameMenu");
    }

    public void GoToMatch()
    {
        SceneManager.LoadScene("Battle");
    }
}
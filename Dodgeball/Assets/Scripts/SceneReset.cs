using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReset : MonoBehaviour
{
    public void ResetScene()
    {
        Debug.ClearDeveloperConsole();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

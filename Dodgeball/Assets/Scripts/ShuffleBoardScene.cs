using UnityEngine;
using UnityEngine.SceneManagement;

public class ShuffleBoardScene : MonoBehaviour
{
    public void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

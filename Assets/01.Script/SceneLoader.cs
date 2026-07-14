using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string targetSceneName;

    public void LoadScene()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}

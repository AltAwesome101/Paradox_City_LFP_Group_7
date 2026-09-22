using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    [SerializeField] SceneReference scene;

    public void Load()
    {
        SceneManager.LoadScene(scene.ScenePath);
    }
}

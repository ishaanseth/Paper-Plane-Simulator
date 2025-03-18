
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlaneController : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void OnFlyButtonPressed()
    {
        SceneManager.LoadScene("level1 1");
    }
}

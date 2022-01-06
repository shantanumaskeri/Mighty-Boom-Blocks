using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroScreen : MonoBehaviour
{
    public GameObject applicationObject;
    
    private void Start()
    {
        DontDestroyOnLoad(applicationObject);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            SceneManager.LoadScene("Game");
    }
}

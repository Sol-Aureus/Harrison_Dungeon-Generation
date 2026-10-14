using UnityEngine;

using UnityEngine.SceneManagement;

public class WinScript : MonoBehaviour
{
    public GameObject winText;

    private void Start()
    {
        winText = GameObject.Find("WinPanel");
        winText.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            winText.SetActive(true);
            Time.timeScale = 0.0f;
        }
    }

}

using UnityEngine;

public class GateScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Key"))
        {
            other.transform.gameObject.tag = "Player";
            Destroy(other.transform.Find("Key").gameObject);
            gameObject.SetActive(false);
        }
    }
}

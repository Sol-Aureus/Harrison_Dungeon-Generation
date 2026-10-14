using UnityEngine;

public class KeyScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            gameObject.transform.SetParent(other.transform);
            gameObject.transform.position = other.transform.position + new Vector3(0.0f, 1.2f, 0.0f);
            other.transform.gameObject.tag = "Key";
        }
    }
}

using UnityEngine;

public class CameraMovementScript : MonoBehaviour
{
    [SerializeField]
    private Transform target; // The target object to follow

    private void Start()
    {
        transform.rotation = Quaternion.identity;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position;
        }
    }
}

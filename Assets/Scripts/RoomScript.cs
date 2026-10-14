using UnityEngine;

public class RoomScript : MonoBehaviour
{
    [System.Serializable]
    public class ColorKeyword
    {
        public string keyword;
        public Color color;
    }

    // Reference to the doors
    public Transform[] doors;
    // The color used for each keyword
    public ColorKeyword[] colorKeywords;
    // A reference to the collider that will be used to check for overlaps
    public Collider colliderCheck;
    public bool isGateRoom;
    public GameObject Key;
    public GameObject[] Gates;
}

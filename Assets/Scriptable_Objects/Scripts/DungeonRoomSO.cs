using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DungeonRoomSO", menuName = "Scriptable Objects/DungeonRoomSO")]
public class DungeonRoomSO : ScriptableObject
{
    [System.Serializable]
    public class DoorProperties
    {
        //The keyword for this door
        public string doorKeyword;
        //Which keywords it can connect to
        public List<string> canConnectToKeywords = new();
    }

    //This is the prefab of the room
    [SerializeField]
    public RoomScript prefab;
    //This is going to be a parallel array to one in the prefab referencing 
    // its doors
    [SerializeField]
    public DoorProperties[] roomDoors;
}

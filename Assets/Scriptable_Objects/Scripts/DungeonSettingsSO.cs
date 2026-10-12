using UnityEngine;

[CreateAssetMenu(fileName = "DungeonSettingsSO", menuName = "Scriptable Objects/DungeonSettingsSO")]
public class DungeonSettingsSO : ScriptableObject
{
    [System.Serializable]
    public class RoomInventoryItem
    {
        public DungeonRoomSO room;
        public int copies;
    }
    // The name for this type of dungeon
    [SerializeField]
    public string dungeonName;
    // The rooms that can be in this dungeon and how many times they can show up
    [SerializeField]
    public RoomInventoryItem[] normalRooms;
    public RoomInventoryItem[] gateRooms;
    public RoomInventoryItem startRoom;
    public RoomInventoryItem endRoom;
    // A reference to the object used to close off open exits
    [SerializeField]
    public Transform cap;
}

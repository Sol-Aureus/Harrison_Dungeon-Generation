using System.Collections.Generic;
using UnityEngine;

public class DungeonManagerScript : MonoBehaviour
{
    private class RoomExits
    {
        public Transform edge;
        public string keyword;
        public List<string> canConnectToKeywords;

        public RoomExits(Transform edge, string keyword, List<string> canConnectToKeywords)
        {
            this.edge = edge;
            this.keyword = keyword;
            this.canConnectToKeywords = canConnectToKeywords ?? new List<string>();
        }
    }

    [SerializeField]
    private DungeonSettingsSO settings;

    [Header("Grid Settings")]
    [Tooltip("The width/length of your uniform room prefabs in world units.")]
    [SerializeField] private float roomSize = 10f;

    private List<DungeonRoomSO> deck = new();
    private List<RoomExits> availableExits = new();

    // Track occupied grid positions (x, y, z)
    private HashSet<Vector3Int> occupiedGridPositions = new();

    void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        occupiedGridPositions.Clear();
        availableExits.Clear();

        int roomCount = 0;
        foreach (DungeonSettingsSO.RoomInventoryItem normal in settings.normalRooms)
        {
            roomCount += normal.copies;
        }
        foreach (DungeonSettingsSO.RoomInventoryItem gate in settings.gateRooms)
        {
            roomCount += gate.copies;
        }

        DungeonRoomSO newRoom;
        RoomScript currentRoom;
        bool gateRoom = false;

        RestockDeck();
        ShuffleDeck();

        // Spawn Start Room
        newRoom = settings.startRoom.room;
        currentRoom = Instantiate(newRoom.prefab, Vector3.zero, Quaternion.identity);

        // Mark start room position as occupied
        occupiedGridPositions.Add(WorldToGridPos(currentRoom.transform.position));

        for (int n = 0; n < currentRoom.doors.Length; n++)
        {
            string kw = (n < newRoom.roomDoors.Length) ? newRoom.roomDoors[n].doorKeyword : currentRoom.colorKeywords[n].keyword;
            List<string> connects = (n < newRoom.roomDoors.Length) ? newRoom.roomDoors[n].canConnectToKeywords : new List<string>();

            availableExits.Add(new RoomExits(currentRoom.doors[n], kw, connects));
        }

        int placedRooms = 1;
        int attempts = 0;
        int maxAttempts = roomCount * 5;

        while (placedRooms < roomCount && attempts < maxAttempts)
        {
            attempts++;

            if (!Draw(out newRoom))
            {
                ShuffleDeck();
                if (!Draw(out newRoom)) break;
            }

            if (SpawnRoom(newRoom, ref currentRoom, ref gateRoom))
            {
                placedRooms++;
            }
            else
            {
                deck.Add(newRoom);
                ShuffleDeck();
            }
        }

        // Spawn End Room
        newRoom = settings.endRoom.room;
        SpawnRoom(newRoom, ref currentRoom, ref gateRoom);

        // Cap off all remaining unused exits
        CloseOffExits();
    }

    bool SpawnRoom(DungeonRoomSO newRoom, ref RoomScript currentRoom, ref bool gateRoom)
    {
        RoomExits currentRoomExit;

        currentRoom = Instantiate(newRoom.prefab, Vector3.zero, Quaternion.identity);

        List<RoomExits> newRoomExits = new();
        for (int n = 0; n < currentRoom.doors.Length; n++)
        {
            string kw = (n < newRoom.roomDoors.Length) ? newRoom.roomDoors[n].doorKeyword : currentRoom.colorKeywords[n].keyword;
            List<string> connects = (n < newRoom.roomDoors.Length) ? newRoom.roomDoors[n].canConnectToKeywords : new List<string>();

            newRoomExits.Add(new RoomExits(currentRoom.doors[n], kw, connects));
        }

        // Shuffle incoming room's exits
        RoomExits temp;
        for (int i = 0; i < newRoomExits.Count; i++)
        {
            int rand = Random.Range(0, newRoomExits.Count);
            temp = newRoomExits[rand];
            newRoomExits[rand] = newRoomExits[i];
            newRoomExits[i] = temp;
        }

        for (int exitIndex = 0; exitIndex < newRoomExits.Count; exitIndex++)
        {
            var exit = newRoomExits[exitIndex];

            List<RoomExits> matchingExits = availableExits.FindAll(x => exit.canConnectToKeywords.Contains(x.keyword));

            if (matchingExits.Count > 0)
            {
                currentRoomExit = matchingExits[Random.Range(0, matchingExits.Count)];
                Transform selectedExit = exit.edge;

                Quaternion targetExitRotation = Quaternion.LookRotation(-currentRoomExit.edge.forward, currentRoomExit.edge.up);
                currentRoom.transform.rotation = targetExitRotation * Quaternion.Inverse(selectedExit.localRotation);

                Vector3 positionOffset = currentRoomExit.edge.position - selectedExit.position;
                currentRoom.transform.position += positionOffset;

                // --- Position-Based Collision Check ---
                Vector3Int targetGridPos = WorldToGridPos(currentRoom.transform.position);

                if (occupiedGridPositions.Contains(targetGridPos))
                {
                    // Cell already occupied by another room
                    continue;
                }

                // Connection verified: mark cell occupied
                occupiedGridPositions.Add(targetGridPos);

                Debug.Log("Connecting " + exit.keyword + " to " + currentRoomExit.keyword);

                newRoomExits.RemoveAt(exitIndex);
                availableExits.Remove(currentRoomExit);

                if (currentRoom.isGateRoom)
                {
                    currentRoom.Key.SetActive(!gateRoom);
                    currentRoom.Gates.SetActive(gateRoom);
                    gateRoom = !gateRoom;

                    if (!gateRoom)
                    {
                        // Seal off older branches to focus linear generation from the gate
                        CloseOffExits();
                    }
                }

                availableExits.AddRange(newRoomExits);

                for (int j = 0; j < availableExits.Count; j++)
                {
                    int rand = Random.Range(0, availableExits.Count);
                    temp = availableExits[j];
                    availableExits[j] = availableExits[rand];
                    availableExits[rand] = temp;
                }

                return true;
            }
        }

        Debug.Log("Couldn't find an available exit for the spawned room.");
        Destroy(currentRoom.gameObject);
        return false;
    }

    private Vector3Int WorldToGridPos(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(worldPos.x / roomSize),
            Mathf.RoundToInt(worldPos.y / roomSize),
            Mathf.RoundToInt(worldPos.z / roomSize)
        );
    }

    public void ShuffleDeck()
    {
        DungeonRoomSO temp;
        for (int i = 0; i < deck.Count; i++)
        {
            int rand = Random.Range(0, deck.Count);
            temp = deck[rand];
            deck[rand] = deck[i];
            deck[i] = temp;
        }
    }

    public void RestockDeck()
    {
        deck.Clear();

        foreach (DungeonSettingsSO.RoomInventoryItem item in settings.normalRooms)
        {
            for (int i = 0; i < item.copies; i++)
            {
                deck.Add(item.room);
            }
        }

        foreach (DungeonSettingsSO.RoomInventoryItem item in settings.gateRooms)
        {
            for (int i = 0; i < item.copies; i++)
            {
                deck.Add(item.room);
            }
        }
    }

    private void CloseOffExits()
    {
        foreach (var exit in availableExits)
        {
            Transform cap = Instantiate(settings.cap);
            cap.position = exit.edge.position;
            cap.forward = -exit.edge.forward;
        }
        availableExits.Clear();
    }

    public bool Draw(out DungeonRoomSO room)
    {
        room = null;
        if (deck.Count > 0)
        {
            room = deck[0];
            deck.RemoveAt(0);
            return true;
        }
        return false;
    }
}
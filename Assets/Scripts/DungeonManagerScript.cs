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
    private HashSet<Vector3Int> occupiedGridPositions = new();

    // Track instantiated objects so we can clean up cleanly if a retry is required
    private List<GameObject> spawnedObjects = new();

    void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        int maxRetries = 100;
        for (int retry = 0; retry < maxRetries; retry++)
        {
            if (TryGenerateDungeon())
            {
                Debug.Log($"Dungeon successfully generated on attempt {retry + 1}.");
                return;
            }

            // Clean up failed layout and retry with a new seed/shuffle
            ClearDungeon();
        }

        Debug.LogError("Failed to generate dungeon within max retries. Check room door keywords or grid dimensions!");
    }

    private bool TryGenerateDungeon()
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
        bool gateRoom = false; // false = next gate room is Key, true = next gate room is Gate

        RestockDeck();
        ShuffleDeck();

        // Spawn Start Room
        newRoom = settings.startRoom.room;
        currentRoom = Instantiate(newRoom.prefab, Vector3.zero, Quaternion.identity);
        spawnedObjects.Add(currentRoom.gameObject);

        occupiedGridPositions.Add(WorldToGridPos(currentRoom.transform.position));

        for (int n = 0; n < currentRoom.doors.Length; n++)
        {
            string kw = (n < newRoom.roomDoors.Length) ? newRoom.roomDoors[n].doorKeyword : currentRoom.colorKeywords[n].keyword;
            List<string> connects = (n < newRoom.roomDoors.Length) ? newRoom.roomDoors[n].canConnectToKeywords : new List<string>();

            availableExits.Add(new RoomExits(currentRoom.doors[n], kw, connects));
        }

        int placedRooms = 1;
        int attempts = 0;
        int maxAttempts = roomCount * 10;

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

        // Validation 1: All rooms placed?
        // Validation 2: Are all Keys paired with Gates? (gateRoom must be false)
        if (placedRooms < roomCount || gateRoom)
        {
            return false;
        }

        // Spawn End Room
        newRoom = settings.endRoom.room;
        if (!SpawnRoom(newRoom, ref currentRoom, ref gateRoom))
        {
            return false;
        }

        // Cap remaining exits
        CloseOffExits();
        return true;
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
            RoomExits exit = newRoomExits[exitIndex];

            List<RoomExits> matchingExits = availableExits.FindAll(x => exit.canConnectToKeywords.Contains(x.keyword));

            if (matchingExits.Count > 0)
            {
                currentRoomExit = matchingExits[Random.Range(0, matchingExits.Count)];
                Transform selectedExit = exit.edge;

                Quaternion targetExitRotation = Quaternion.LookRotation(-currentRoomExit.edge.forward, currentRoomExit.edge.up);
                currentRoom.transform.rotation = targetExitRotation * Quaternion.Inverse(selectedExit.localRotation);

                Vector3 positionOffset = currentRoomExit.edge.position - selectedExit.position;
                currentRoom.transform.position += positionOffset;

                // Position Collision Check
                Vector3Int targetGridPos = WorldToGridPos(currentRoom.transform.position);

                if (occupiedGridPositions.Contains(targetGridPos))
                {
                    continue;
                }

                occupiedGridPositions.Add(targetGridPos);

                newRoomExits.RemoveAt(exitIndex);
                availableExits.Remove(currentRoomExit);

                if (currentRoom.isGateRoom)
                {
                    if (gateRoom)
                    {
                        Debug.Log("Locking gate room door: " + exit.keyword);
                        switch (exit.keyword)
                        {
                            case "RED":
                                currentRoom.Gates[0].transform.gameObject.SetActive(true);
                                break;
                            case "GREEN":
                                currentRoom.Gates[1].transform.gameObject.SetActive(true);
                                break;
                            case "BLUE":
                                currentRoom.Gates[2].transform.gameObject.SetActive(true);
                                break;
                            case "YELLOW":
                                currentRoom.Gates[3].transform.gameObject.SetActive(true);
                                break;
                        }
                        CloseOffExits();
                    }
                    else
                    {
                        currentRoom.Key.SetActive(true);
                    }

                    gateRoom = !gateRoom;
                }

                availableExits.AddRange(newRoomExits);

                for (int j = 0; j < availableExits.Count; j++)
                {
                    int rand = Random.Range(0, availableExits.Count);
                    temp = availableExits[j];
                    availableExits[j] = availableExits[rand];
                    availableExits[rand] = temp;
                }

                spawnedObjects.Add(currentRoom.gameObject);
                return true;
            }
        }

        Destroy(currentRoom.gameObject);
        return false;
    }

    private void ClearDungeon()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        spawnedObjects.Clear();
        occupiedGridPositions.Clear();
        availableExits.Clear();
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
            spawnedObjects.Add(cap.gameObject);
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
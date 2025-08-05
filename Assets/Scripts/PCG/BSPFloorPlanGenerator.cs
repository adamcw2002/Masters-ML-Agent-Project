using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BSPGridFloorPlanGenerator : MonoBehaviour
{
    [Header("Generation Seed")]
    private int seed;
    [SerializeField] private int startSeed;
    [SerializeField] private bool randomizeSeedOnNewRecipe;

    [Header("Floor Plan Settings")]
    [SerializeField] private int gridWidth = 50;    // Width in cells
    [SerializeField] private int gridLength = 50;
    [SerializeField] private int roomCount = 3;
    [SerializeField] private Material floorMaterial;

    // Floor height
    [SerializeField] private float floorHeight = 0f;
    private const float floorYScale = 0.2f;

    // Gap between rooms
    [SerializeField] private int gapSize = 1;

    // Minimum room size in cells
    [SerializeField] private int minRoomSize = 5;

    [Header("Doorway Settings")]
    [SerializeField] private bool generateDoors = true;
    [SerializeField] private int doorWidth = 3;
    [SerializeField] private bool placeDoorsRandomly = true;
    [SerializeField] private Material doorMaterial;
    private HashSet<Vector2Int> doorPositions = new HashSet<Vector2Int>();

    [Header("Bridge Settings")]
    [SerializeField] private bool generateBridges = true;
    [SerializeField] private int minimumBridgeWidth = 2;
    [SerializeField] private int bridgeWidth = 3;
    [SerializeField] private bool connectAllRooms = false;
    [SerializeField] private Material bridgeMaterial;
    private const int borderSize = 1;
    private HashSet<Vector2Int> bridgePositions = new HashSet<Vector2Int>();

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    private GameObject player;

    // Grid to track which cells are floors
    private bool[,] floorGrid;

    // Queue to track which rooms need to be processed
    private Queue<Room> roomQueue = new Queue<Room>();

    // Final list of rooms after splitting
    private List<Room> finalRooms = new List<Room>();

    public List<Room> GeneratedRooms => finalRooms;
    public HashSet<Vector2Int> DoorPositions => doorPositions;
    public HashSet<Vector2Int> BridgePositions => bridgePositions;


    public static event Action OnFloorGenerated;

    private void Start()
    {
        seed = startSeed;

        GenerateFloorPlan(false);

        PlayerAgent.OnEpisodeEnd += PlayerAgent_OnEpisodeEnd;
    }

    private void PlayerAgent_OnEpisodeEnd(object sender, EventArgs e)
    {
        GenerateFloorPlan(randomizeSeedOnNewRecipe);
    }

    public void GenerateFloorPlan(bool randomizeSeed)
    {
        if (randomizeSeed)
        {
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        UnityEngine.Random.InitState(seed);

        // Clear any existing floor plan
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        roomQueue.Clear();
        finalRooms.Clear();
        doorPositions.Clear();
        bridgePositions.Clear();

        // Initialize grid
        floorGrid = new bool[gridWidth, gridLength];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                floorGrid[x, z] = false;
            }
        }

        // Create the root container
        Room rootRoom = new Room(
            new Vector2Int(0, 0),
            new Vector2Int(gridWidth, gridLength)
        );

        // Add root to queue
        roomQueue.Enqueue(rootRoom);

        // Apply BSP with exact split count
        SplitRoomsExactly(roomCount - 1);

        // Mark cells as floor in our grid
        foreach (Room room in finalRooms)
        {
            for (int x = room.position.x; x < room.position.x + room.size.x; x++)
            {
                for (int z = room.position.y; z < room.position.y + room.size.y; z++)
                {
                    if (x >= 0 && x < gridWidth && z >= 0 && z < gridLength)
                    {
                        floorGrid[x, z] = true;
                    }
                }
            }
        }

        // Generate doors or bridges between rooms
        if (generateDoors && gapSize <= 0 && finalRooms.Count > 1)
        {
            GenerateDoorsBetweenRooms();
        }
        else if (generateBridges && finalRooms.Count > 1)
        {
            GenerateBridges();
        }

        // Generate the actual floor cells
        CreateFloorCells();

        SpawnPlayer();

        OnFloorGenerated?.Invoke();
    }

    private void SpawnPlayer()
    {
        if (finalRooms.Count == 0 || playerPrefab == null) return;

        Room randomRoom = finalRooms[UnityEngine.Random.Range(0, finalRooms.Count)];

        Vector3 spawnPosition = new Vector3(randomRoom.center.x, floorHeight, randomRoom.center.y);

        if (player != null)
        {
            CharacterController characterController = player.GetComponent<CharacterController>();

            characterController.enabled = false;
            player.transform.position = spawnPosition;
            characterController.enabled = true;
        }
        else
        {
            player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        }
    }

    private void SplitRoomsExactly(int exactSplitCount)
    {
        int splitsDone = 0;

        if (gapSize < -1)
        {
            Debug.LogWarning("Gap Size out of range, resetting");
            gapSize = Mathf.Max(0, gapSize);
        }

        while (splitsDone < exactSplitCount && roomQueue.Count > 0)
        {
            Room roomToSplit = null;

            // Find the largest room to split
            List<Room> currentRooms = new List<Room>();
            while (roomQueue.Count > 0)
            {
                currentRooms.Add(roomQueue.Dequeue());
            }

            // Sort rooms by area (largest first)
            currentRooms.Sort((a, b) => (b.size.x * b.size.y).CompareTo(a.size.x * a.size.y));

            // Find first splittable room
            foreach (Room room in currentRooms)
            {
                if (room.IsSplittable(minRoomSize, gapSize))
                {
                    roomToSplit = room;
                    currentRooms.Remove(room);
                    break;
                }
            }

            // Re-queue rooms we didn't split
            foreach (Room room in currentRooms)
            {
                if (room != roomToSplit)
                {
                    if (room.IsSplittable(minRoomSize, gapSize))
                    {
                        roomQueue.Enqueue(room);  // Only re-queue splittable rooms
                    }
                    else
                    {
                        finalRooms.Add(room);  // Add non-splittable rooms to final list
                    }
                }
            }

            // If no room can be split further, break
            if (roomToSplit == null)
            {
                // Add any remaining rooms to final list
                while (roomQueue.Count > 0)
                {
                    finalRooms.Add(roomQueue.Dequeue());
                }
                break;
            }

            // Perform the split
            bool splitHorizontal = roomToSplit.size.x > roomToSplit.size.y;
            if (roomToSplit.size.x < (minRoomSize * 2 + gapSize))
            {
                splitHorizontal = false;
            }
            else if (roomToSplit.size.y < (minRoomSize * 2 + gapSize))
            {
                splitHorizontal = true;
            }

            // Calculate split position with some randomness
            int availableSpace;
            int minSplitPos;
            int maxSplitPos;

            if (splitHorizontal)
            {
                availableSpace = roomToSplit.size.x - (minRoomSize * 2 + gapSize);
                minSplitPos = roomToSplit.position.x + minRoomSize;
                maxSplitPos = roomToSplit.position.x + roomToSplit.size.x - minRoomSize - gapSize;
            }
            else
            {
                availableSpace = roomToSplit.size.y - (minRoomSize * 2 + gapSize);
                minSplitPos = roomToSplit.position.y + minRoomSize;
                maxSplitPos = roomToSplit.position.y + roomToSplit.size.y - minRoomSize - gapSize;
            }

            // If there's not enough space to make a valid split with gap
            if (availableSpace <= 0)
            {
                finalRooms.Add(roomToSplit); // Can't split, add to final rooms
                continue;
            }

            int splitPos = UnityEngine.Random.Range(minSplitPos, maxSplitPos + 1);

            Room leftChild, rightChild;

            if (splitHorizontal)
            {
                // Split horizontally (along X axis)
                int leftWidth = splitPos - roomToSplit.position.x;
                int rightWidth = roomToSplit.size.x - leftWidth - gapSize;

                // Left child
                leftChild = new Room(
                    roomToSplit.position,
                    new Vector2Int(leftWidth, roomToSplit.size.y)
                );

                // Right child
                rightChild = new Room(
                    new Vector2Int(splitPos + gapSize, roomToSplit.position.y),
                    new Vector2Int(rightWidth, roomToSplit.size.y)
                );
            }
            else
            {
                // Split vertically (along Z axis)
                int bottomLength = splitPos - roomToSplit.position.y;
                int topLength = roomToSplit.size.y - bottomLength - gapSize;

                // Bottom child
                leftChild = new Room(
                    roomToSplit.position,
                    new Vector2Int(roomToSplit.size.x, bottomLength)
                );

                // Top child
                rightChild = new Room(
                    new Vector2Int(roomToSplit.position.x, splitPos + gapSize),
                    new Vector2Int(roomToSplit.size.x, topLength)
                );
            }

            roomQueue.Enqueue(leftChild);
            roomQueue.Enqueue(rightChild);

            roomToSplit.isSplit = true;
            splitsDone++;
        }

        // Add any remaining rooms to final list
        while (roomQueue.Count > 0)
        {
            finalRooms.Add(roomQueue.Dequeue());
        }
    }

    private void GenerateDoorsBetweenRooms()
    {
        List<RoomConnection> connections = new List<RoomConnection>();

        // Find all possible connections between rooms
        for (int i = 0; i < finalRooms.Count; i++)
        {
            for (int j = i + 1; j < finalRooms.Count; j++)
            {
                Room roomA = finalRooms[i];
                Room roomB = finalRooms[j];

                // Check if rooms are adjacent (sharing a wall)
                if (AreRoomsAdjacent(roomA, roomB))
                {
                    connections.Add(new RoomConnection(roomA, roomB));
                }
            }
        }

        if (!connectAllRooms && connections.Count > finalRooms.Count - 1)
        {
            // Generate a minimum spanning tree to ensure all rooms are connected
            connections = GenerateMinimumSpanningTree(connections);
        }

        foreach (RoomConnection connection in connections)
        {
            CreateDoorBetweenRooms(connection.roomA, connection.roomB);
        }
    }

    private bool AreRoomsAdjacent(Room roomA, Room roomB)
    {
        // Get room rectangles
        Rect rectA = new Rect(roomA.position.x, roomA.position.y, roomA.size.x, roomA.size.y);
        Rect rectB = new Rect(roomB.position.x, roomB.position.y, roomB.size.x, roomB.size.y);

        // Check if rooms share a horizontal wall
        bool shareHorizontalWall =
            (rectA.xMin <= rectB.xMax && rectA.xMax >= rectB.xMin) && // X overlap
            ((Mathf.Approximately(rectA.yMax + gapSize, rectB.yMin)) || (Mathf.Approximately(rectA.yMin, rectB.yMax + gapSize))); // Y touching

        // Check if rooms share a vertical wall
        bool shareVerticalWall =
            (rectA.yMin <= rectB.yMax && rectA.yMax >= rectB.yMin) && // Y overlap
            ((Mathf.Approximately(rectA.xMax + gapSize, rectB.xMin)) || (Mathf.Approximately(rectA.xMin, rectB.xMax + gapSize))); // X touching

        return shareHorizontalWall || shareVerticalWall;
    }

    private void CreateDoorBetweenRooms(Room roomA, Room roomB)
    {
        // Get room rectangles
        Rect rectA = new Rect(roomA.position.x, roomA.position.y, roomA.size.x, roomA.size.y);
        Rect rectB = new Rect(roomB.position.x, roomB.position.y, roomB.size.x, roomB.size.y);

        // Shared horizontal wall (rooms are stacked vertically)
        if ((rectA.xMin <= rectB.xMax && rectA.xMax >= rectB.xMin) &&
            (Mathf.Approximately(rectA.yMax + gapSize, rectB.yMin) || Mathf.Approximately(rectA.yMin, rectB.yMax + gapSize)))
        {
            // Find the overlap in x coordinate
            int xStart = Mathf.Max((int)rectA.xMin, (int)rectB.xMin);
            int xEnd = Mathf.Min((int)rectA.xMax, (int)rectB.xMax);

            // Calculate the potential door position
            int doorLength = Mathf.Min(doorWidth, xEnd - xStart - 2);
            int doorStart;

            if (placeDoorsRandomly)
            {
                // Random position along the shared wall
                doorStart = UnityEngine.Random.Range(xStart + 1, xEnd - (doorLength + 1));
            }
            else
            {
                // Center position
                doorStart = xStart + (xEnd - xStart - doorLength) / 2;
            }

            int wallYStart = Mathf.Min((int)rectA.yMax, (int)rectB.yMax);
            int wallYEnd = Mathf.Max((int)rectA.yMin, (int)rectB.yMin);

            // Mark the door cells in our floor grid
            for (int x = doorStart; x < doorStart + doorLength; x++)
            {
                for (int y = wallYStart - 1; y < wallYEnd + 1; y++)
                {
                    // Mark as door cell
                    if (x >= 0 && x < gridWidth)
                    {
                        CreateDoorCell(x, y);
                    }
                }
            }
        }
        // Shared vertical wall (rooms are side by side)
        else if ((rectA.yMin <= rectB.yMax && rectA.yMax >= rectB.yMin) &&
                 (Mathf.Approximately(rectA.xMax + gapSize, rectB.xMin) || Mathf.Approximately(rectA.xMin, rectB.xMax + gapSize)))
        {
            // Find the overlap in y coordinate
            int yStart = Mathf.Max((int)rectA.yMin, (int)rectB.yMin);
            int yEnd = Mathf.Min((int)rectA.yMax, (int)rectB.yMax);

            // Calculate the potential door position
            int doorLength = Mathf.Min(doorWidth, yEnd - yStart - 2);
            int doorStart;

            if (placeDoorsRandomly)
            {
                // Random position along the shared wall
                doorStart = UnityEngine.Random.Range(yStart + 1, yEnd - (doorLength + 1));
            }
            else
            {
                // Center position
                doorStart = yStart + (yEnd - yStart - doorLength) / 2;
            }

            int wallXStart = Mathf.Min((int)rectA.xMax, (int)rectB.xMax);
            int wallXEnd = Mathf.Max((int)rectA.xMin, (int)rectB.xMin);

            // Mark the door cells in our floor grid
            for (int y = doorStart; y < doorStart + doorLength; y++)
            {
                for (int x = wallXStart - 1; x < wallXEnd + 1; x++)
                {
                    // Mark as door cell
                    if (y >= 0 && y < gridLength)
                    {
                        CreateDoorCell(x, y);
                    }
                }
            }
        }
    }

    private void GenerateBridges()
    {
        if (minimumBridgeWidth > bridgeWidth)
        {
            minimumBridgeWidth = bridgeWidth;
            Debug.Log("Adjusted minimum bridge width to: " +  minimumBridgeWidth);
        }

        List<RoomConnection> connections = new List<RoomConnection>();

        for (int i = 0; i < finalRooms.Count; i++)
        {
            for (int j = i + 1; j < finalRooms.Count; j++)
            {
                Room roomA = finalRooms[i];
                Room roomB = finalRooms[j];

                connections.Add(new RoomConnection(roomA, roomB));
            }
        }

        if (!connectAllRooms)
        {
            // Generate a minimum spanning tree to ensure all rooms are connected
            connections = GenerateMinimumSpanningTree(connections);
        }

        foreach (RoomConnection connection in connections)
        {
            CreateBridgeBetweenRooms(connection.roomA, connection.roomB);
        }
    }

    private List<RoomConnection> GenerateMinimumSpanningTree(List<RoomConnection> allConnections)
    {
        // Sort connections by distance (ascending)
        allConnections.Sort((a, b) => a.distance.CompareTo(b.distance));

        // Sets for tracking connected components
        Dictionary<Room, HashSet<Room>> connectedSets = new Dictionary<Room, HashSet<Room>>();
        foreach (Room room in finalRooms)
        {
            HashSet<Room> newSet = new HashSet<Room> { room };
            connectedSets[room] = newSet;
        }

        // Result list for MST connections
        List<RoomConnection> mstConnections = new List<RoomConnection>();

        // Kruskal's algorithm
        foreach (RoomConnection connection in allConnections)
        {
            Room roomA = connection.roomA;
            Room roomB = connection.roomB;

            // If the rooms are already in the same connected component, skip
            if (connectedSets[roomA] == connectedSets[roomB])
                continue;

            // Add this connection to MST
            mstConnections.Add(connection);

            // Merge the connected components
            HashSet<Room> setA = connectedSets[roomA];
            HashSet<Room> setB = connectedSets[roomB];

            // Merge smaller set into larger set for efficiency
            if (setA.Count < setB.Count)
            {
                foreach (Room room in setA)
                {
                    setB.Add(room);
                    connectedSets[room] = setB;
                }
            }
            else
            {
                foreach (Room room in setB)
                {
                    setA.Add(room);
                    connectedSets[room] = setA;
                }
            }

            // If all rooms are connected, we're done
            if (mstConnections.Count == finalRooms.Count - 1)
                break;
        }

        return mstConnections;
    }

    private void CreateBridgeBetweenRooms(Room roomA, Room roomB)
    {
        // Get room rectangles
        Rect rectA = new Rect(roomA.position.x, roomA.position.y, roomA.size.x, roomA.size.y);
        Rect rectB = new Rect(roomB.position.x, roomB.position.y, roomB.size.x, roomB.size.y);

        // Get room centres and check distance
        Vector2 centerA = roomA.center;
        Vector2 centerB = roomB.center;
        Vector2 distance = centerB - centerA;

        // Determine if rooms are adjacent or diagonal
        bool shareX = (rectA.xMin <= rectB.xMax && rectA.xMax >= rectB.xMin);
        bool shareY = (rectA.yMin <= rectB.yMax && rectA.yMax >= rectB.yMin);

        if (shareX || shareY)
        {
            // Rooms share an axis, can connect directly
            if (shareX)
            {
                float requiredY = (roomA.size.y / 2f) + (roomB.size.y / 2f) + gapSize;

                //If rooms are too far apart, usually meaning there is a room between them
                if (Mathf.Abs(distance.y) > requiredY) return;

                int lowerBound = Mathf.Max((int)rectA.xMin + borderSize, (int)rectB.xMin + borderSize);
                int upperBound = Mathf.Min((int)rectA.xMax - (bridgeWidth + borderSize), (int)rectB.xMax - (bridgeWidth + borderSize));
                int currentBridgeWidth = bridgeWidth;

                // Connect along Y axis
                int bridgeX = Mathf.Clamp(
                    Mathf.RoundToInt((roomA.center.x + roomB.center.x) / 2),
                    lowerBound,
                    upperBound
                );

                // Check if enough space for bridge, adjust bridge size if not
                if (lowerBound > upperBound)
                {
                    bridgeX = lowerBound;
                    currentBridgeWidth = Mathf.Min((int)rectA.xMax - borderSize, (int)rectB.xMax - borderSize) - lowerBound;

                    // Return if bridge length is less than the minimum
                    if (currentBridgeWidth < minimumBridgeWidth) return;
                }

                if (bridgeX < 0) bridgeX = Mathf.Max((int)rectA.xMin + bridgeWidth, (int)rectB.xMin + bridgeWidth);

                int startY, endY;
                if (rectA.yMax < rectB.yMin) // A is below B
                {
                    startY = (int)rectA.yMax - 1;
                    endY = (int)rectB.yMin;
                }
                else // B is below A
                {
                    startY = (int)rectB.yMax - 1;
                    endY = (int)rectA.yMin;
                }

                // Create vertical bridge
                for (int y = startY; y <= endY; y++)
                {
                    for (int x = bridgeX; x < bridgeX + currentBridgeWidth; x++)
                    {
                        if (x >= 0 && x < gridWidth && y >= 0 && y < gridLength)
                        {
                            floorGrid[x, y] = true;

                            if (y == startY || y == endY) doorPositions.Add(new Vector2Int(x, y));
                            else bridgePositions.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
            else // shareY
            {
                float requiredX = (roomA.size.x / 2f) + (roomB.size.x / 2f) + gapSize;

                //If rooms are too far apart, usually meaning there is a room between them
                if (Mathf.Abs(distance.x) > requiredX) return;

                int lowerBound = Mathf.Max((int)rectA.yMin + borderSize, (int)rectB.yMin) + borderSize;
                int upperBound = Mathf.Min((int)rectA.yMax - (bridgeWidth + borderSize), (int)rectB.yMax - (bridgeWidth + borderSize));
                int currentBridgeWidth = bridgeWidth;

                int bridgeY = Mathf.Clamp(
                    Mathf.RoundToInt((roomA.center.y + roomB.center.y) / 2),
                    lowerBound,
                    upperBound
                );

                // Check if enough space for bridge, adjust bridge size if not
                if (lowerBound > upperBound)
                {
                    bridgeY = lowerBound;
                    currentBridgeWidth = Mathf.Min((int)rectA.yMax - borderSize, (int)rectB.yMax - borderSize) - lowerBound;

                    // Return if bridge length is less than the minimum
                    if (currentBridgeWidth < minimumBridgeWidth) return;
                }

                if (bridgeY < 0) bridgeY = Mathf.Max((int)rectA.yMin, (int)rectB.yMin);

                int startX, endX;
                if (rectA.xMax < rectB.xMin) // A is left of B
                {
                    startX = (int)rectA.xMax - 1;
                    endX = (int)rectB.xMin;
                }
                else // B is left of A
                {
                    startX = (int)rectB.xMax - 1;
                    endX = (int)rectA.xMin;
                }

                // Create horizontal bridge
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = bridgeY; y < bridgeY + currentBridgeWidth; y++)
                    {
                        if (x >= 0 && x < gridWidth && y >= 0 && y < gridLength)
                        {
                            floorGrid[x, y] = true;

                            if (x == startX || x == endX) doorPositions.Add(new Vector2Int(x, y));
                            else bridgePositions.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
        }
    }

    private void CreateDoorCell(int x, int z)
    {
        // Mark this position as a door
        doorPositions.Add(new Vector2Int(x, z));

        // Ensure it's marked as a floor in our grid
        if (x >= 0 && x < gridWidth && z >= 0 && z < gridLength)
        {
            floorGrid[x, z] = true;
        }
    }

    private void CreateFloorCells()
    {
        GameObject cellsParent = new GameObject("Floor Cells");
        cellsParent.transform.SetParent(transform);

        // Create a floor cell for each marked grid position
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                if (floorGrid[x, z])
                {
                    bool isBridge = bridgePositions.Contains(new Vector2Int(x, z));
                    CreateFloorCell(x, z, cellsParent.transform, isBridge);
                }
            }
        }
    }

    private void CreateFloorCell(int x, int z, Transform parent, bool isBridge)
    {
        bool isDoor = doorPositions.Contains(new Vector2Int(x, z));

        GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cell.name = isDoor ? $"Door_{x}_{z}" : (isBridge ? $"Bridge_{x}_{z}" : $"Cell_{x}_{z}");
        cell.transform.SetParent(parent);

        // Position the cell (center of each 1x1 grid cell)
        cell.transform.position = new Vector3(
            x + 0.5f,  // Center of the cell
            floorHeight - (floorYScale * 2),
            z + 0.5f   // Center of the cell
        );

        // 1x1 cells with some height
        cell.transform.localScale = new Vector3(1.0f, floorYScale, 1.0f);

        // Apply material based on cell type
        if (isDoor && doorMaterial != null)
        {
            cell.GetComponent<Renderer>().material = doorMaterial;
        }
        else if (isBridge && bridgeMaterial != null)
        {
            cell.GetComponent<Renderer>().material = bridgeMaterial;
        }
        else if (floorMaterial != null)
        {
            cell.GetComponent<Renderer>().material = floorMaterial;
        }
    }

    // Allow for runtime parameter changes
    public void SetParameters(int newWidth, int newLength, int newRoomCount, int newGapSize, bool newGenerateBridges, int newBridgeWidth)
    {
        gridWidth = newWidth;
        gridLength = newLength;
        roomCount = newRoomCount;
        gapSize = newGapSize;
        generateBridges = newGenerateBridges;
        bridgeWidth = newBridgeWidth;
    }

    public float GetFloorHeight() => floorHeight;
    public float GetFloorYScale() => floorYScale;
    public int GetGridWidth() => gridWidth;
    public int GetGridLength() => gridLength;
}
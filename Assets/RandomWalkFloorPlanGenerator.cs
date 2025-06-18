using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RandomWalkFloorPlanGenerator : MonoBehaviour
{
    [Header("Default Workspace")]
    [SerializeField] private GameObject emptyWorkspacePrefab;

    [Header("Random Walk Settings")]
    [SerializeField] private int minWalkLength = 3;
    [SerializeField] private int maxWalkLength = 8;
    [SerializeField] private int minStartingPoints = 1;
    [SerializeField] private int maxStartingPoints = 2;
    [SerializeField] private float branchingChance = 0.3f;
    [SerializeField] private int maxBranches = 2;

    private List<GameObject> spawnedCenterWorkspaces = new List<GameObject>();
    private HashSet<Vector2Int> centerWorkspacePositions = new HashSet<Vector2Int>();
    private BSPGridFloorPlanGenerator floorPlanGenerator;
    private System.Random rng = new System.Random();

    private float floorHeight = 0;
    private float floorYScale = 0;
    private Transform centerWorkspacesParent = null;

    private void OnEnable()
    {
        BSPGridFloorPlanGenerator.OnFloorGenerated += GenerateCenterWorkspaces;
    }

    private void OnDisable()
    {
        BSPGridFloorPlanGenerator.OnFloorGenerated -= GenerateCenterWorkspaces;
    }

    private void Awake()
    {
        floorPlanGenerator = GetComponent<BSPGridFloorPlanGenerator>();
        AssignFloorPlanValues();
    }

    private void AssignFloorPlanValues()
    {
        if (floorPlanGenerator)
        {
            floorHeight = floorPlanGenerator.GetFloorHeight();
            floorYScale = floorPlanGenerator.GetFloorYScale();
        }
    }

    private void GenerateCenterWorkspaces()
    {
        // Clean up previous center workspaces
        CleanupPreviousCenterWorkspaces();

        // Assign correct values from floor plan generator
        AssignFloorPlanValues();

        // Generate center workspaces for each room
        foreach (Room room in floorPlanGenerator.GeneratedRooms)
        {
            GenerateWorkspacesForRoom(room);
        }
    }

    private void GenerateWorkspacesForRoom(Room room)
    {
        // Calculate the valid area (leaving 1 cell gap around edges)
        int validMinX = room.position.x + 2;
        int validMaxX = room.position.x + room.size.x - 3;
        int validMinZ = room.position.y + 2;
        int validMaxZ = room.position.y + room.size.y - 3;

        // Check if room is large enough for center workspaces
        if (validMaxX <= validMinX || validMaxZ <= validMinZ)
        {
            return; // Room too small for center workspaces
        }

        // Determine number of starting points for this room
        int startingPoints = rng.Next(minStartingPoints, maxStartingPoints + 1);

        // Generate random walk patterns
        for (int i = 0; i < startingPoints; i++)
        {
            Vector2Int startPos = GetRandomValidPosition(validMinX, validMaxX, validMinZ, validMaxZ);

            // Make sure starting position isn't already occupied
            if (!centerWorkspacePositions.Contains(startPos))
            {
                GenerateRandomWalk(startPos, validMinX, validMaxX, validMinZ, validMaxZ);
            }
        }
    }

    private void GenerateRandomWalk(Vector2Int startPos, int validMinX, int validMaxX, int validMinZ, int validMaxZ)
    {
        HashSet<Vector2Int> walkPositions = new HashSet<Vector2Int>();
        Queue<WalkNode> walkQueue = new Queue<WalkNode>();

        // Start the walk
        walkQueue.Enqueue(new WalkNode(startPos, 0, 0)); // position, steps taken, branch level

        while (walkQueue.Count > 0)
        {
            WalkNode currentNode = walkQueue.Dequeue();
            Vector2Int currentPos = currentNode.position;

            // Skip if position is already occupied or out of bounds
            if (centerWorkspacePositions.Contains(currentPos) ||
                !IsValidPosition(currentPos, validMinX, validMaxX, validMinZ, validMaxZ))
            {
                continue;
            }

            // Add position to walk
            walkPositions.Add(currentPos);
            centerWorkspacePositions.Add(currentPos);

            // Determine walk length for this branch
            int walkLength = rng.Next(minWalkLength, maxWalkLength + 1);

            // Continue walk if we haven't reached the target length
            if (currentNode.stepsTaken < walkLength)
            {
                List<Vector2Int> directions = GetRandomDirections();
                Vector2Int nextPos = currentPos + directions[0];

                // Add next step in walk
                walkQueue.Enqueue(new WalkNode(nextPos, currentNode.stepsTaken + 1, currentNode.branchLevel));

                // Chance to create branches
                if (currentNode.branchLevel < maxBranches &&
                    rng.NextDouble() < branchingChance &&
                    directions.Count > 1)
                {
                    Vector2Int branchPos = currentPos + directions[1];
                    walkQueue.Enqueue(new WalkNode(branchPos, currentNode.stepsTaken + 1, currentNode.branchLevel + 1));
                }
            }
        }

        // Create workspace objects for all positions in this walk
        foreach (Vector2Int pos in walkPositions)
        {
            CreateCenterWorkspaceCell(pos);
        }
    }

    private Vector2Int GetRandomValidPosition(int validMinX, int validMaxX, int validMinZ, int validMaxZ)
    {
        int x = rng.Next(validMinX, validMaxX + 1);
        int z = rng.Next(validMinZ, validMaxZ + 1);
        return new Vector2Int(x, z);
    }

    private bool IsValidPosition(Vector2Int pos, int validMinX, int validMaxX, int validMinZ, int validMaxZ)
    {
        return pos.x >= validMinX && pos.x <= validMaxX &&
               pos.y >= validMinZ && pos.y <= validMaxZ;
    }

    private List<Vector2Int> GetRandomDirections()
    {
        List<Vector2Int> directions = new List<Vector2Int>
        {
            Vector2Int.up,      // North
            Vector2Int.right,   // East  
            Vector2Int.down,    // South
            Vector2Int.left     // West
        };

        // Shuffle directions for randomness
        for (int i = 0; i < directions.Count; i++)
        {
            int randomIndex = rng.Next(i, directions.Count);
            Vector2Int temp = directions[i];
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        return directions;
    }

    private void CreateCenterWorkspaceCell(Vector2Int pos)
    {
        GameObject cell = Instantiate(emptyWorkspacePrefab);
        cell.name = $"CenterWorkspace_{pos.x}_{pos.y}";

        if (centerWorkspacesParent == null)
        {
            centerWorkspacesParent = new GameObject("CenterWorkspaces").transform;
            centerWorkspacesParent.transform.SetParent(transform);
        }

        cell.transform.SetParent(centerWorkspacesParent);

        cell.transform.position = new Vector3(
            pos.x + 0.5f,
            floorHeight + floorYScale,
            pos.y + 0.5f
        );

        spawnedCenterWorkspaces.Add(cell);
    }

    private void CleanupPreviousCenterWorkspaces()
    {
        foreach (GameObject workspace in spawnedCenterWorkspaces)
        {
            if (workspace != null)
                Destroy(workspace);
        }

        spawnedCenterWorkspaces.Clear();
        centerWorkspacePositions.Clear();

        if (centerWorkspacesParent != null)
        {
            Destroy(centerWorkspacesParent.gameObject);
            centerWorkspacesParent = null;
        }
    }

    // Helper class to track walk state
    private class WalkNode
    {
        public Vector2Int position;
        public int stepsTaken;
        public int branchLevel;

        public WalkNode(Vector2Int pos, int steps, int branches)
        {
            position = pos;
            stepsTaken = steps;
            branchLevel = branches;
        }
    }

    // Public method to get center workspace positions (useful for other systems)
    public HashSet<Vector2Int> GetCenterWorkspacePositions()
    {
        return new HashSet<Vector2Int>(centerWorkspacePositions);
    }

    // Public method to check if a position has a center workspace
    public bool HasCenterWorkspaceAt(Vector2Int position)
    {
        return centerWorkspacePositions.Contains(position);
    }
}
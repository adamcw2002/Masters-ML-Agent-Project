using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkspaceGenerator : MonoBehaviour
{
    [SerializeField] private Material borderMaterial;

    [Header ("Appliances")]
    [SerializeField] private GameObject choppingBoardPrefab;
    [SerializeField] private GameObject stovePrefab;
    [SerializeField] private GameObject mixingStationPrefab;

    [Header("Other")]
    [SerializeField] private GameObject plateStationPrefab;
    [SerializeField] private GameObject ingredientSpawnerPrefab;
    [SerializeField] private GameObject trashBinPrefab;
    [SerializeField] private GameObject servingCounterPrefab;

    [SerializeField] private int minChoppingBoards = 2;

    private Dictionary<string, int> requiredApplianceCounts = new Dictionary<string, int>();
    private Dictionary<string, int> requiredIngredientCounts = new Dictionary<string, int>();
    private List<GameObject> spawnedWorkspaces = new List<GameObject>();

    private BSPGridFloorPlanGenerator FloorPlanGenerator;

    private void OnEnable()
    {
        BSPGridFloorPlanGenerator.OnFloorGenerated += CreateWorkspaces;
    }
    private void OnDisable()
    {
        BSPGridFloorPlanGenerator.OnFloorGenerated += CreateWorkspaces;
    }

    private void Awake()
    {
        FloorPlanGenerator = GetComponent<BSPGridFloorPlanGenerator>();
    }

    private void CreateWorkspaces()
    {
        foreach (GameObject workspace in spawnedWorkspaces)
        {
            if (workspace != null)
                Destroy(workspace);
        }
        spawnedWorkspaces.Clear();

        AnalyzeRecipes();

        List<Vector3> borderPositions = GetAllBorderPositions();

        ShufflePositions(borderPositions);

        /*
        foreach (Room room in FloorPlanGenerator.GeneratedRooms)
        {
            CreateBorderCells(room);
        }
        */
    }

    private void CreateBorderCells(Room room)
    {
        float floorHeight = FloorPlanGenerator.GetFloorHeight();
        float floorYScale = FloorPlanGenerator.GetFloorYScale();
        HashSet<Vector2Int> doorPositions = FloorPlanGenerator.DoorPositions;

        GameObject bordersParent = new GameObject("Border Cells");
        bordersParent.transform.SetParent(transform);

        // Create border cells around this room
        for (int x = room.position.x; x < room.position.x + room.size.x; x++)
        {
            for (int z = room.position.y; z < room.position.y + room.size.y; z++)
            {
                if (doorPositions.Contains(new Vector2Int(x, z))) continue;

                // Only create borders for cells on the edge of the room
                bool isEdgeCell = x == room.position.x ||
                                 x == room.position.x + room.size.x - 1 ||
                                 z == room.position.y ||
                                 z == room.position.y + room.size.y - 1;

                if (isEdgeCell || FloorPlanGenerator.DoorPositions.Contains(new Vector2Int(x, z)))
                {
                    CreateBorderCell(x, z, floorHeight, floorYScale, bordersParent.transform);
                }
            }
        }
    }

    private void CreateBorderCell(int x, int z, float floorHeight, float floorYScale, Transform parent)
    {
        GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cell.name = $"Border_{x}_{z}";
        cell.transform.SetParent(parent);

        cell.transform.position = new Vector3(
            x + 0.5f,  // Center of the cell
            floorHeight + floorYScale,  // 1 unit above the floor
            z + 0.5f   // Center of the cell
        );

        // 1x1x1 cubes
        cell.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

        // Apply material
        if (borderMaterial != null)
        {
            cell.GetComponent<Renderer>().material = borderMaterial;
        }
    }

    private void AnalyzeRecipes()
    {
        requiredApplianceCounts.Clear();
        requiredIngredientCounts.Clear();

        // Initialize with minimum requirements
        requiredApplianceCounts["ChoppingBoard"] = minChoppingBoards;
        requiredApplianceCounts["Stove"] = 0;

        RecipeManager recipeManager = FindObjectOfType<RecipeManager>();

        if (recipeManager == null)
        {
            Debug.LogWarning("Cannot find Recipe Manager");
            return;
        }

        // Analyze each recipe
        foreach (Recipe recipe in recipeManager.activeRecipes)
        {
            // Count required ingredients
            foreach (string ingredient in recipe.requiredIngredients)
            {
                if (!requiredIngredientCounts.ContainsKey(ingredient))
                    requiredIngredientCounts[ingredient] = 0;

                requiredIngredientCounts[ingredient]++;
            }

            // Count required appliances based on cooking steps
            foreach (CookingStep step in recipe.cookingSteps)
            {
                if (!requiredApplianceCounts.ContainsKey(step.applianceRequired))
                    requiredApplianceCounts[step.applianceRequired] = 0;

                requiredApplianceCounts[step.applianceRequired]++;
            }
        }

        // Cap appliance counts based on game balance
        foreach (string appliance in requiredApplianceCounts.Keys)
        {
            // Default cap of 3 per appliance type, can be adjusted
            requiredApplianceCounts[appliance] = Mathf.Min(requiredApplianceCounts[appliance], 3);
        }

        // Ensure we always have at least 1 of each basic appliance type
        EnsureMinimumAppliances();

        // Debug log the analysis
        Debug.Log("Recipe Analysis Results:");
        foreach (var kvp in requiredApplianceCounts)
        {
            Debug.Log($"Required {kvp.Key}: {kvp.Value}");
        }
    }

    private void EnsureMinimumAppliances()
    {
        // Always include these basic appliances
        if (!requiredApplianceCounts.ContainsKey("ChoppingBoard"))
            requiredApplianceCounts["ChoppingBoard"] = minChoppingBoards;

        if (!requiredApplianceCounts.ContainsKey("Stove"))
            requiredApplianceCounts["Stove"] = 0;

        requiredApplianceCounts["IngredientSpawner"] = Mathf.Max(0, requiredIngredientCounts.Count);

        requiredApplianceCounts["TrashBin"] = 0;

        requiredApplianceCounts["ServingCounter"] = 0;

        requiredApplianceCounts["PlateStation"] = 0;
    }

    private List<Vector3> GetAllBorderPositions()
    {
        float floorHeight = FloorPlanGenerator.GetFloorHeight();
        float floorYScale = FloorPlanGenerator.GetFloorYScale();
        HashSet<Vector2Int> doorPositions = FloorPlanGenerator.DoorPositions;
        List<Vector3> borderPositions = new List<Vector3>();

        foreach (Room room in FloorPlanGenerator.GeneratedRooms)
        {
            // Create border cells around this room
            for (int x = room.position.x; x < room.position.x + room.size.x; x++)
            {
                for (int z = room.position.y; z < room.position.y + room.size.y; z++)
                {
                    if (doorPositions.Contains(new Vector2Int(x, z))) continue;

                    // Only use cells on the edge of the room
                    bool isEdgeCell = x == room.position.x ||
                                     x == room.position.x + room.size.x - 1 ||
                                     z == room.position.y ||
                                     z == room.position.y + room.size.y - 1;

                    if (isEdgeCell)
                    {
                        Vector3 position = new Vector3(
                            x + 0.5f,  // Center of the cell
                            floorHeight + floorYScale,  // 1 unit above the floor
                            z + 0.5f   // Center of the cell
                        );

                        borderPositions.Add(position);
                    }
                }
            }
        }

        return borderPositions;
    }

    private void ShufflePositions(List<Vector3> positions)
    {
        System.Random rng = new System.Random();
        int n = positions.Count;

        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Vector3 value = positions[k];
            positions[k] = positions[n];
            positions[n] = value;
        }
    }
}

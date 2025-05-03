using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class WorkspaceGenerator : MonoBehaviour
{
    [SerializeField] private Material borderMaterial;

    [Header("Default Workspace")]
    [SerializeField] private GameObject emptyWorkspacePrefab;

    [Header("Appliances")]
    [SerializeField] private GameObject choppingBoardPrefab;
    [SerializeField] private GameObject stovePrefab;

    [Header("Other")]
    [SerializeField] private GameObject plateStationPrefab;
    [SerializeField] private GameObject ingredientSpawnerPrefab;
    [SerializeField] private GameObject trashBinPrefab;
    [SerializeField] private GameObject servingCounterPrefab;

    private Dictionary<GameObject, int> requiredApplianceCounts = new Dictionary<GameObject, int>();
    private Dictionary<IngredientData, int> requiredIngredientCounts = new Dictionary<IngredientData, int>();
    private List<GameObject> spawnedWorkspaces = new List<GameObject>();

    private BSPGridFloorPlanGenerator FloorPlanGenerator;
    private System.Random rng = new System.Random();

    private void OnEnable()
    {
        BSPGridFloorPlanGenerator.OnFloorGenerated += CreateWorkspaces;
    }

    private void OnDisable()
    {
        BSPGridFloorPlanGenerator.OnFloorGenerated -= CreateWorkspaces;
    }

    private void Awake()
    {
        FloorPlanGenerator = GetComponent<BSPGridFloorPlanGenerator>();
    }

    private void CreateWorkspaces()
    {
        // Clean up previous workspaces
        CleanupPreviousWorkspaces();

        // Analyze recipes to determine required appliances and ingredients
        AnalyzeRecipes();

        // Get border and corner positions
        (List<Vector2Int> borderPositions, HashSet<Vector2Int> cornerPositions) = GetBorderAndCornerPositions();

        // Shuffle the non-corner border positions
        List<Vector2Int> nonCornerBorderPositions = borderPositions
            .Where(pos => !cornerPositions.Contains(pos))
            .ToList();
        ShufflePositions(nonCornerBorderPositions);

        // Create corner workspaces first (always empty)
        float floorHeight = FloorPlanGenerator.GetFloorHeight();
        float floorYScale = FloorPlanGenerator.GetFloorYScale();
        GameObject workspacesParent = new GameObject("Workspaces");
        workspacesParent.transform.SetParent(transform);

        // Create empty workspaces at corners
        foreach (Vector2Int cornerPos in cornerPositions)
        {
            CreateWorkspaceCell(cornerPos, emptyWorkspacePrefab, null, floorHeight, floorYScale, workspacesParent.transform);
        }

        // Create remaining workspaces along the non-corner borders
        var workspacesToSpawn = GetRequiredWorkspacePrefabs();
        CreateWorkspacesAtPositions(nonCornerBorderPositions, workspacesToSpawn, floorHeight, floorYScale, workspacesParent.transform);
    }

    private void CleanupPreviousWorkspaces()
    {
        foreach (GameObject workspace in spawnedWorkspaces)
        {
            if (workspace != null)
                Destroy(workspace);
        }
        spawnedWorkspaces.Clear();
    }

    private void CreateWorkspacesAtPositions(List<Vector2Int> positions, Queue<(GameObject prefab, IngredientData ingredient)> workspacesToSpawn,
                                            float floorHeight, float floorYScale, Transform parent)
    {
        foreach (Vector2Int position in positions)
        {
            GameObject prefab = emptyWorkspacePrefab;
            IngredientData ingredient = null;

            if (workspacesToSpawn.Count > 0)
            {
                (prefab, ingredient) = workspacesToSpawn.Dequeue();
            }

            CreateWorkspaceCell(position, prefab, ingredient, floorHeight, floorYScale, parent);
        }
    }

    private void CreateWorkspaceCell(Vector2Int pos, GameObject prefab, IngredientData ingredient, float floorHeight, float floorYScale, Transform parent)
    {
        GameObject cell = Instantiate(prefab);
        cell.name = $"Workspace_{pos.x}_{pos.y}";
        cell.transform.SetParent(parent);

        cell.transform.position = new Vector3(
            pos.x + 0.5f,
            floorHeight + floorYScale,
            pos.y + 0.5f
        );

        spawnedWorkspaces.Add(cell);

        if (borderMaterial != null && cell.TryGetComponent(out Renderer rend))
        {
            rend.material = borderMaterial;
        }

        // Initialize ingredient spawner
        if (ingredient != null && cell.TryGetComponent(out IngredientSpawner spawner))
        {
            spawner.SetIngredientData(ingredient);
        }
    }

    private void AnalyzeRecipes()
    {
        requiredApplianceCounts.Clear();
        requiredIngredientCounts.Clear();

        RecipeManager recipeManager = FindObjectOfType<RecipeManager>();

        if (recipeManager == null)
        {
            Debug.LogWarning("Cannot find Recipe Manager");
            return;
        }

        // Analyze each recipe
        foreach (RecipeData recipe in recipeManager.activeRecipes)
        {
            // Count required ingredients
            foreach (RequiredRecipeIngredient ingredient in recipe.requiredIngredients)
            {
                if (!requiredIngredientCounts.ContainsKey(ingredient.ingredient))
                    requiredIngredientCounts[ingredient.ingredient] = 0;

                requiredIngredientCounts[ingredient.ingredient]++;

                CheckOrAddAppliance(ingredient.requiredState);
            }
        }

        // Ensure we always have at least 1 of each basic appliance type
        EnsureMinimumAppliances();
    }

    private void CheckOrAddAppliance(IngredientState ingredientState)
    {
        GameObject appliancePrefab = null;

        switch (ingredientState)
        {
            case IngredientState.Chopped:
                appliancePrefab = choppingBoardPrefab;
                break;
            case IngredientState.Cooked:
                appliancePrefab = stovePrefab;
                break;
        }

        if (appliancePrefab != null)
        {
            requiredApplianceCounts.TryGetValue(appliancePrefab, out int count);
            requiredApplianceCounts[appliancePrefab] = count + 1;
        }
    }

    private void EnsureMinimumAppliances()
    {
        if (trashBinPrefab) requiredApplianceCounts[trashBinPrefab] = 1;
        if (servingCounterPrefab) requiredApplianceCounts[servingCounterPrefab] = 1;
        if (plateStationPrefab) requiredApplianceCounts[plateStationPrefab] = 1;
    }

    private Queue<(GameObject prefab, IngredientData ingredient)> GetRequiredWorkspacePrefabs()
    {
        Queue<(GameObject prefab, IngredientData ingredient)> workspaceQueue = new();

        // Appliances (no ingredient data needed)
        foreach (var kvp in requiredApplianceCounts)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                workspaceQueue.Enqueue((kvp.Key, null));
            }
        }

        // Ingredient spawners (include the ingredient data)
        foreach (var kvp in requiredIngredientCounts)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                workspaceQueue.Enqueue((ingredientSpawnerPrefab, kvp.Key));
            }
        }

        return workspaceQueue;
    }

    private (List<Vector2Int>, HashSet<Vector2Int>) GetBorderAndCornerPositions()
    {
        HashSet<Vector2Int> doorPositions = FloorPlanGenerator.DoorPositions;
        List<Vector2Int> borderPositions = new List<Vector2Int>();
        HashSet<Vector2Int> cornerPositions = new HashSet<Vector2Int>();

        foreach (Room room in FloorPlanGenerator.GeneratedRooms)
        {
            int minX = room.position.x;
            int maxX = room.position.x + room.size.x - 1;
            int minZ = room.position.y;
            int maxZ = room.position.y + room.size.y - 1;

            // Add corners first
            Vector2Int[] corners = new[]
            {
                new Vector2Int(minX, minZ), // Bottom-left
                new Vector2Int(maxX, minZ), // Bottom-right
                new Vector2Int(minX, maxZ), // Top-left
                new Vector2Int(maxX, maxZ)  // Top-right
            };

            // Add corners that are not door positions
            foreach (Vector2Int corner in corners)
            {
                if (!doorPositions.Contains(corner))
                {
                    cornerPositions.Add(corner);
                }
            }

            // Add non-corner border cells
            // Left and right edges (excluding corners)
            for (int z = minZ + 1; z < maxZ; z++)
            {
                AddBorderPosition(new Vector2Int(minX, z), doorPositions, borderPositions);
                AddBorderPosition(new Vector2Int(maxX, z), doorPositions, borderPositions);
            }

            // Top and bottom edges (excluding corners)
            for (int x = minX + 1; x < maxX; x++)
            {
                AddBorderPosition(new Vector2Int(x, minZ), doorPositions, borderPositions);
                AddBorderPosition(new Vector2Int(x, maxZ), doorPositions, borderPositions);
            }
        }

        // Add corner positions to border positions as well
        borderPositions.AddRange(cornerPositions);

        return (borderPositions, cornerPositions);
    }

    private void AddBorderPosition(Vector2Int position, HashSet<Vector2Int> doorPositions, List<Vector2Int> borderPositions)
    {
        if (!doorPositions.Contains(position))
        {
            borderPositions.Add(position);
        }
    }

    private void ShufflePositions(List<Vector2Int> positions)
    {
        int n = positions.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Vector2Int temp = positions[k];
            positions[k] = positions[n];
            positions[n] = temp;
        }
    }
}

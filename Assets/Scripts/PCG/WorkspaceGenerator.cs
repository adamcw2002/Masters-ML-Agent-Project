using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WorkspaceGenerator : MonoSingleton<WorkspaceGenerator>
{
    public static event EventHandler OnWorkspacesGenerated;

    [SerializeField] private Material borderMaterial;

    [Header("Default Workspace")]
    [SerializeField] private GameObject emptyWorkspacePrefab;

    [Header("Appliances")]
    [SerializeField] private GameObject choppingBoardPrefab;
    [SerializeField] private GameObject stovePrefab;
    [SerializeField] private GameObject hobPrefab;
    [SerializeField] private GameObject ovenPrefab;

    [Header("Other")]
    [SerializeField] private GameObject plateStationPrefab;
    [SerializeField] private GameObject ingredientSpawnerPrefab;
    [SerializeField] private GameObject trashBinPrefab;
    [SerializeField] private GameObject deliveryStationPrefab;

    [Header("Placement Rules")]
    [SerializeField] private int applianceSpacing = 1;
    [SerializeField] private int maxiumumNumberOfEachAppliance = 3;

    private Dictionary<GameObject, int> requiredApplianceCounts = new Dictionary<GameObject, int>();
    private Dictionary<IngredientData, int> requiredIngredientCounts = new Dictionary<IngredientData, int>();
    private List<GameObject> spawnedWorkspaces = new List<GameObject>();

    // Grid system for workspace lookup
    private Dictionary<Vector2Int, GameObject> workspaceGrid = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int gridMinBounds;
    private Vector2Int gridMaxBounds;

    private BSPGridFloorPlanGenerator FloorPlanGenerator;

    private GameObject deliveryStation;

    private float FloorHeight = 0;
    private float FloorYScale = 0;
    private Transform workspacesParent = null;

    private HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> bufferPositions = new HashSet<Vector2Int>();



    #region Public Grid Access Methods

    public GameObject GetWorkspaceAt(Vector2Int gridPosition)
    {
        workspaceGrid.TryGetValue(gridPosition, out GameObject workspace);
        return workspace;
    }

    public GameObject GetWorkspaceAtWorldPosition(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        return GetWorkspaceAt(gridPos);
    }

    public Dictionary<Vector2Int, GameObject> GetWorkspacesInRange(Vector2Int minPosition, Vector2Int maxPosition)
    {
        Dictionary<Vector2Int, GameObject> workspacesInRange = new Dictionary<Vector2Int, GameObject>();

        for (int x = minPosition.x; x <= maxPosition.x; x++)
        {
            for (int y = minPosition.y; y <= maxPosition.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                GameObject workspace = GetWorkspaceAt(pos);
                if (workspace != null)
                {
                    workspacesInRange[pos] = workspace;
                }
            }
        }

        return workspacesInRange;
    }

    public Dictionary<Vector2Int, GameObject> GetWorkspacesInRadius(Vector2Int centerPosition, int radius)
    {
        Dictionary<Vector2Int, GameObject> workspacesInRadius = new Dictionary<Vector2Int, GameObject>();

        for (int x = centerPosition.x - radius; x <= centerPosition.x + radius; x++)
        {
            for (int y = centerPosition.y - radius; y <= centerPosition.y + radius; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                // Check if position is within circular radius
                float distance = Vector2Int.Distance(centerPosition, pos);
                if (distance <= radius)
                {
                    GameObject workspace = GetWorkspaceAt(pos);
                    if (workspace != null)
                    {
                        workspacesInRadius[pos] = workspace;
                    }
                }
            }
        }

        return workspacesInRadius;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x - 0.5f),
            Mathf.RoundToInt(worldPosition.z - 0.5f)
        );
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(
            gridPosition.x + 0.5f,
            FloorHeight + FloorYScale,
            gridPosition.y + 0.5f
        );
    }


    public bool HasWorkspaceAt(Vector2Int gridPosition)
    {
        return workspaceGrid.ContainsKey(gridPosition);
    }

    public Dictionary<Vector2Int, GameObject> GetAllWorkspaces()
    {
        return new Dictionary<Vector2Int, GameObject>(workspaceGrid);
    }

    public GameObject GetDeliveryStation()
    {
        return deliveryStation;
    }

    #endregion

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
        AssignFloorPlanValues();
    }

    private void AssignFloorPlanValues()
    {
        if (FloorPlanGenerator)
        {
            FloorHeight = FloorPlanGenerator.GetFloorHeight();
            FloorYScale = FloorPlanGenerator.GetFloorYScale();
        }
    }

    private void CreateWorkspaces()
    {
        CleanupPreviousWorkspaces();

        // Assign correct values fromm floor plan generator
        AssignFloorPlanValues();

        // Analyze recipes to determine required appliances and ingredients
        AnalyzeRecipes();

        // Get border and corner positions
        (List<Vector2Int> borderPositions, HashSet<Vector2Int> cornerPositions) = GetBorderAndCornerPositions();

        // Shuffle the non-corner border positions
        List<Vector2Int> nonCornerBorderPositions = borderPositions
            .Where(pos => !cornerPositions.Contains(pos))
            .ToList();
        ShufflePositions(nonCornerBorderPositions);

        // Create empty workspaces at corners
        foreach (Vector2Int cornerPos in cornerPositions)
        {
            CreateWorkspaceCell(cornerPos, emptyWorkspacePrefab, null, true);
        }

        // Apply the new placement rules
        ApplyPlacementRules(nonCornerBorderPositions);

        // Update grid bounds after all workspaces are created
        UpdateGridBounds();

        OnWorkspacesGenerated?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateGridBounds()
    {
        if (workspaceGrid.Count == 0)
        {
            gridMinBounds = Vector2Int.zero;
            gridMaxBounds = Vector2Int.zero;
            return;
        }

        var positions = workspaceGrid.Keys.ToList();
        gridMinBounds = new Vector2Int(
            positions.Min(p => p.x),
            positions.Min(p => p.y)
        );
        gridMaxBounds = new Vector2Int(
            positions.Max(p => p.x),
            positions.Max(p => p.y)
        );
    }

    private void ApplyPlacementRules(List<Vector2Int> availablePositions)
    {
        List<Vector2Int> remainingPositions = new List<Vector2Int>(availablePositions);

        usedPositions.Clear();
        bufferPositions.Clear();

        // 1. Place delivery station first
        if (deliveryStationPrefab != null && remainingPositions.Count > 0)
        {
            // Find a good spot for delivery station
            Vector2Int deliveryPos = FindBestPosition(remainingPositions);
            remainingPositions.Remove(deliveryPos);

            deliveryStation = CreateWorkspaceCell(deliveryPos, deliveryStationPrefab, null);
            MarkPositionUsed(deliveryPos, false); // Don't add buffer - plates need to be adjacent

            // Place plates directly next to delivery station (without buffer)
            if (plateStationPrefab != null)
            {
                // For plate station, we want adjacent but don't respect buffer
                List<Vector2Int> adjacentPositions = GetAvailableAdjacentPositions(deliveryPos, remainingPositions, true);
                if (adjacentPositions.Count > 0)
                {
                    Vector2Int platePos = adjacentPositions[0];
                    remainingPositions.Remove(platePos);

                    CreateWorkspaceCell(platePos, plateStationPrefab, null);
                    MarkPositionUsed(platePos); // Add buffer for plates
                }
                else if (remainingPositions.Count > 0)
                {
                    // Fallback: place plate at next available position if no adjacent spots
                    Vector2Int platePos = FindBestPosition(remainingPositions);
                    remainingPositions.Remove(platePos);

                    CreateWorkspaceCell(platePos, plateStationPrefab, null);
                    MarkPositionUsed(platePos);
                }
            }

            // Now add buffer to delivery station after plate placement
            MarkPositionUsed(deliveryPos);
        }

        // Get updated list of available positions that respect buffer zones
        remainingPositions = GetAvailablePositions(remainingPositions);

        // 2. Place appliances by type, with each type in its own area and proper spacing
        // Group the appliances by type
        Dictionary<GameObject, int> appliancesByType = new Dictionary<GameObject, int>();
        foreach (var kvp in requiredApplianceCounts)
        {
            // Skip trash bins (handled separately) and already placed delivery/plate stations
            if (kvp.Key == trashBinPrefab || kvp.Key == deliveryStationPrefab || kvp.Key == plateStationPrefab)
                continue;

            appliancesByType[kvp.Key] = kvp.Value;
        }

        // Place each appliance type in its own area
        foreach (var applianceType in appliancesByType)
        {
            GameObject prefab = applianceType.Key;
            int count = applianceType.Value;

            // Get updated available positions
            remainingPositions = GetAvailablePositions(remainingPositions);

            if (count > 0 && remainingPositions.Count > 0)
            {
                // Start position for this appliance type group
                Vector2Int startPos = FindBestPosition(remainingPositions);
                remainingPositions.Remove(startPos);

                CreateWorkspaceCell(startPos, prefab, null);
                MarkPositionUsed(startPos, false); // Don't add full buffer yet for appliance group

                // Place remaining appliances of this type with spacing
                Vector2Int lastPos = startPos;
                int placed = 1;

                while (placed < count && remainingPositions.Count > 0)
                {
                    // Find positions with the correct spacing from the last appliance
                    List<Vector2Int> spacedPositions = GetPositionsWithSpacing(lastPos, remainingPositions, applianceSpacing);

                    if (spacedPositions.Count > 0)
                    {
                        Vector2Int nextPos = spacedPositions[0];
                        remainingPositions.Remove(nextPos);

                        CreateWorkspaceCell(nextPos, prefab, null);
                        MarkPositionUsed(nextPos, false); // No buffer for appliances of same type

                        lastPos = nextPos;
                        placed++;
                    }
                    else
                    {
                        // If no properly spaced positions are available, find a new starting point
                        remainingPositions = GetAvailablePositions(remainingPositions);
                        if (remainingPositions.Count > 0)
                        {
                            Vector2Int newStartPos = FindBestPosition(remainingPositions);
                            remainingPositions.Remove(newStartPos);

                            CreateWorkspaceCell(newStartPos, prefab, null);
                            MarkPositionUsed(newStartPos, false);

                            lastPos = newStartPos;
                            placed++;
                        }
                        else
                        {
                            break; // No more positions available
                        }
                    }
                }

                // After placing all appliances of this type, add buffer to the whole group
                // This ensures different appliance types have proper spacing between groups
                usedPositions.Clear(); // Clear existing buffer tracking
                foreach (GameObject workspace in spawnedWorkspaces)
                {
                    if (workspace != null)
                    {
                        Vector2Int position = new Vector2Int(
                            Mathf.RoundToInt(workspace.transform.position.x - 0.5f),
                            Mathf.RoundToInt(workspace.transform.position.z - 0.5f)
                        );
                        MarkPositionUsed(position);
                    }
                }
            }
        }

        // Update remaining positions to respect buffers
        remainingPositions = GetAvailablePositions(availablePositions);

        // 3. Place ingredients together
        List<IngredientData> ingredients = new List<IngredientData>();
        foreach (var kvp in requiredIngredientCounts)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                ingredients.Add(kvp.Key);
            }
        }

        if (ingredients.Count > 0 && remainingPositions.Count > 0)
        {
            // Start with the first available position
            Vector2Int currentPos = FindBestPosition(remainingPositions);
            remainingPositions.Remove(currentPos);

            CreateWorkspaceCell(currentPos, ingredientSpawnerPrefab, ingredients[0]);
            MarkPositionUsed(currentPos, false); // No buffer for same ingredient group

            // Place remaining ingredients adjacent to each other
            for (int i = 1; i < ingredients.Count; i++)
            {
                List<Vector2Int> adjacentPositions = GetAvailableAdjacentPositions(currentPos, remainingPositions, true);

                if (adjacentPositions.Count > 0)
                {
                    // Place the next ingredient adjacent to the last one
                    Vector2Int nextPos = adjacentPositions[0];
                    remainingPositions.Remove(nextPos);

                    CreateWorkspaceCell(nextPos, ingredientSpawnerPrefab, ingredients[i]);
                    MarkPositionUsed(nextPos, false); // No buffer for same ingredient group

                    currentPos = nextPos; // Update current position for the next ingredient
                }
                else
                {
                    // If no adjacent positions are available, update remaining positions
                    remainingPositions = GetAvailablePositions(availablePositions);

                    if (remainingPositions.Count > 0)
                    {
                        // Find a new starting point
                        Vector2Int newStartPos = FindBestPosition(remainingPositions);
                        remainingPositions.Remove(newStartPos);

                        CreateWorkspaceCell(newStartPos, ingredientSpawnerPrefab, ingredients[i]);
                        MarkPositionUsed(newStartPos, false);

                        currentPos = newStartPos;
                    }
                    else
                    {
                        break; // No more positions available
                    }
                }
            }

            // After placing all ingredients, add buffer around the whole ingredient group
            usedPositions.Clear(); // Clear existing buffer tracking
            foreach (GameObject workspace in spawnedWorkspaces)
            {
                if (workspace != null)
                {
                    Vector2Int position = new Vector2Int(
                        Mathf.RoundToInt(workspace.transform.position.x - 0.5f),
                        Mathf.RoundToInt(workspace.transform.position.z - 0.5f)
                    );
                    MarkPositionUsed(position);
                }
            }

            // Remove all of the workspace positions from the buffer positions
            foreach (GameObject workspace in spawnedWorkspaces)
            {
                if (workspace != null)
                {
                    Vector2Int position = new Vector2Int(
                        Mathf.RoundToInt(workspace.transform.position.x - 0.5f),
                        Mathf.RoundToInt(workspace.transform.position.z - 0.5f)
                    );
                    bufferPositions.Remove(position);
                }
            }
        }

        // Update remaining positions for trash bins
        remainingPositions = GetAvailablePositions(availablePositions);

        // 4. Place trash bins randomly throughout
        if (trashBinPrefab != null && requiredApplianceCounts.TryGetValue(trashBinPrefab, out int binCount))
        {
            // Keep trash bins placement random but respect buffer zones
            List<Vector2Int> shuffledPositions = new List<Vector2Int>(remainingPositions);
            ShufflePositions(shuffledPositions);

            for (int i = 0; i < binCount && i < shuffledPositions.Count; i++)
            {
                Vector2Int binPos = shuffledPositions[i];
                remainingPositions.Remove(binPos);

                CreateWorkspaceCell(binPos, trashBinPrefab, null);
                MarkPositionUsed(binPos);

                // Update available positions after each bin placement
                shuffledPositions = GetAvailablePositions(shuffledPositions);
            }
        }

        // Fill remaining positions with empty workspaces
        remainingPositions = availablePositions.Where(pos => !usedPositions.Contains(pos)).ToList();
        foreach (Vector2Int pos in remainingPositions)
        {
            CreateWorkspaceCell(pos, emptyWorkspacePrefab, null);
        }

        // Loop through all buffer positions that are in original list and add empty workspace
        foreach (Vector2Int pos in bufferPositions)
        {
            if (availablePositions.Contains(pos)) CreateWorkspaceCell(pos, emptyWorkspacePrefab, null);
        }
    }

    private Vector2Int FindBestPosition(List<Vector2Int> availablePositions)
    {
        if (availablePositions.Count == 0)
            throw new System.InvalidOperationException("No available positions to choose from");

        return availablePositions[0]; // For simplicity and determinism, use the first available position
    }

    // Mark a position as used and add a buffer zone around it
    private void MarkPositionUsed(Vector2Int position, bool addBuffer = true)
    {
        usedPositions.Add(position);

        if (addBuffer)
        {
            // Add buffer zone (1-tile gap around position)
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    if (xOffset == 0 && yOffset == 0) continue; // Skip the position itself

                    Vector2Int bufferPos = new Vector2Int(position.x + xOffset, position.y + yOffset);
                    usedPositions.Add(bufferPos);
                    bufferPositions.Add(bufferPos);
                }
            }
        }
    }

    // Check if a position or its buffer zone is already used
    private bool IsPositionAvailable(Vector2Int position, List<Vector2Int> availablePositions)
    {
        return availablePositions.Contains(position) && !usedPositions.Contains(position);
    }

    // Find positions that are available and have proper spacing from used positions
    private List<Vector2Int> GetAvailablePositions(List<Vector2Int> allPositions, bool ignoreBuffer = false)
    {
        return allPositions.Where(pos => ignoreBuffer ? !usedPositions.Contains(pos) : IsPositionAvailable(pos, allPositions)).ToList();
    }

    // Find adjacent positions that respect the buffer zones
    private List<Vector2Int> GetAvailableAdjacentPositions(Vector2Int position, List<Vector2Int> availablePositions, bool ignoreBuffer = false)
    {
        List<Vector2Int> adjacentPositions = new List<Vector2Int>();

        // Define directions with labels
        Dictionary<string, Vector2Int> directions = new Dictionary<string, Vector2Int>()
    {
        { "Up",    new Vector2Int(0, 1) },
        { "Right", new Vector2Int(1, 0) },
        { "Down",  new Vector2Int(0, -1) },
        { "Left",  new Vector2Int(-1, 0) }
    };

        // Track which directions were added
        Dictionary<string, Vector2Int> foundDirections = new Dictionary<string, Vector2Int>();

        foreach (var pair in directions)
        {
            Vector2Int adjacentPos = position + pair.Value;

            bool isAvailable = ignoreBuffer
                ? availablePositions.Contains(adjacentPos) && !usedPositions.Contains(adjacentPos)
                : IsPositionAvailable(adjacentPos, availablePositions);

            if (isAvailable)
            {
                adjacentPositions.Add(adjacentPos);
                foundDirections[pair.Key] = adjacentPos;
            }
        }

        // If exactly 3 positions are found, determine the missing direction and remove the opposite
        if (adjacentPositions.Count == 3)
        {
            var allDirections = new List<string> { "Up", "Right", "Down", "Left" };
            var missingDirection = allDirections.Except(foundDirections.Keys).FirstOrDefault();

            if (missingDirection != null)
            {
                string opposite = missingDirection switch
                {
                    "Up" => "Down",
                    "Down" => "Up",
                    "Left" => "Right",
                    "Right" => "Left",
                    _ => null
                };

                if (opposite != null && foundDirections.ContainsKey(opposite))
                {
                    adjacentPositions.Remove(foundDirections[opposite]);
                }
            }
        }

        return adjacentPositions;
    }


    private List<Vector2Int> GetPositionsWithSpacing(Vector2Int position, List<Vector2Int> availablePositions, int spacing)
    {
        List<Vector2Int> spacedPositions = new List<Vector2Int>();

        foreach (Vector2Int pos in availablePositions)
        {
            int xDiff = Mathf.Abs(pos.x - position.x);
            int yDiff = Mathf.Abs(pos.y - position.y);

            // Check if spacing is on the same axis only (no diagonal)
            if ((xDiff == spacing + 1 && yDiff == 0) || (yDiff == spacing + 1 && xDiff == 0))
            {
                spacedPositions.Add(pos);
            }
        }

        return spacedPositions;
    }

    private void CleanupPreviousWorkspaces()
    {
        foreach (GameObject workspace in spawnedWorkspaces)
        {
            if (workspace != null)
                Destroy(workspace);
        }
        spawnedWorkspaces.Clear();
        bufferPositions.Clear();
        usedPositions.Clear();
        workspaceGrid.Clear(); // Clear the grid

        if (workspacesParent) Destroy(workspacesParent.gameObject);
        workspacesParent = null;

        if (workspacesParent == null)
        {
            workspacesParent = new GameObject("Workspaces").transform;
            workspacesParent.transform.SetParent(transform);
        }
    }

    private GameObject CreateWorkspaceCell(Vector2Int pos, GameObject prefab, IngredientData ingredient, bool isCornerCell = false)
    {
        GameObject cell = Instantiate(prefab);
        cell.name = $"Workspace_{pos.x}_{pos.y}";
        cell.transform.SetParent(workspacesParent);

        cell.transform.position = new Vector3(
            pos.x + 0.5f,
            FloorHeight + FloorYScale,
            pos.y + 0.5f
        );

        spawnedWorkspaces.Add(cell);

        // Add to grid system
        if (!isCornerCell) workspaceGrid[pos] = cell;

        if (borderMaterial != null && cell.TryGetComponent(out Renderer rend))
        {
            rend.material = borderMaterial;
        }

        // Initialize ingredient spawner
        if (ingredient != null && cell.TryGetComponent(out IngredientSpawner spawner))
        {
            spawner.SetIngredientData(ingredient);
        }

        return cell;
    }

    private void AnalyzeRecipes()
    {
        requiredApplianceCounts.Clear();
        requiredIngredientCounts.Clear();

        if (RecipeManager.Instance == null)
        {
            Debug.LogWarning("Cannot find Recipe Manager");
            return;
        }

        // Analyze each recipe
        foreach (RecipeData recipe in RecipeManager.Instance.GetAvailableRecipes())
        {
            // Count required ingredients
            foreach (RequiredRecipeIngredient ingredient in recipe.baseRequiredIngredients)
            {
                if (ingredient.ingredient.isProduct)
                {
                    //FIND RECIPE TO MAKE THIS PRODUCT
                    continue;
                }

                if (!requiredIngredientCounts.ContainsKey(ingredient.ingredient))
                    requiredIngredientCounts[ingredient.ingredient] = 1;

                //requiredIngredientCounts[ingredient.ingredient]++;

                //ADD APPLIANCE FOR OUTPUT STATE
                CheckOrAddAppliance(ingredient.requiredState);


                //ADD APPLIANCE FOR THE PRECONDITION eg (chopped -> boiled, adds chopping board aswell)
                IngredientState? preconditionState = ingredient.ingredient.GetPreconditionState(ingredient.requiredState);
                if (preconditionState.HasValue) CheckOrAddAppliance(preconditionState.Value);
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
                appliancePrefab = ovenPrefab;
                break;
            case IngredientState.Boiled:
                appliancePrefab = stovePrefab;
                break;
            case IngredientState.Fried:
                appliancePrefab = hobPrefab;
                break;
        }

        if (appliancePrefab != null)
        {
            if (!requiredApplianceCounts.ContainsKey(appliancePrefab))
                requiredApplianceCounts[appliancePrefab] = 0;

            if (requiredApplianceCounts[appliancePrefab] + 1 <= maxiumumNumberOfEachAppliance)
                requiredApplianceCounts[appliancePrefab]++;
        }
    }

    private void EnsureMinimumAppliances()
    {
        if (trashBinPrefab) requiredApplianceCounts[trashBinPrefab] = 1;
        if (deliveryStationPrefab) requiredApplianceCounts[deliveryStationPrefab] = 1;
        if (plateStationPrefab) requiredApplianceCounts[plateStationPrefab] = 1;
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

    // This function is kept for compatibility but used selectively to reduce randomness
    private void ShufflePositions(List<Vector2Int> positions)
    {
        // If we need deterministic behavior for debugging, we can comment out the shuffle logic
        int n = positions.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            Vector2Int temp = positions[k];
            positions[k] = positions[n];
            positions[n] = temp;
        }
    }
}
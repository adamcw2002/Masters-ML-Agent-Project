using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class AgentObservationManager : MonoSingleton<AgentObservationManager>
{
    private const int observationsPerTile = 17;
    private const int observationsPerTileWithRelativePos = 19;


    private GameObject cachedPlayer;

    private void OnEnable()
    {
        PlayerAgent.OnAgentSpawned += PlayerAgent_OnAgentSpawned;
    }
    private void OnDisable()
    {
        PlayerAgent.OnAgentSpawned -= PlayerAgent_OnAgentSpawned;
    }

    private void PlayerAgent_OnAgentSpawned(object sender, EventArgs e)
    {
        cachedPlayer = sender as GameObject;
    }

    private Vector2Int GetVector2IntPos(Vector3 pos) => new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z));
    public Vector2Int GetPlayerVector2IntPos() => new Vector2Int(Mathf.FloorToInt(cachedPlayer.transform.position.x), Mathf.FloorToInt(cachedPlayer.transform.position.z));

    public float[] GetCurrentRecipeObservation()
    {
        RecipeData activeRecipe = RecipeManager.Instance.GetActiveRecipe();
        int maxIngredients = 4;
        int stateCount = System.Enum.GetValues(typeof(IngredientState)).Length;

        float[] observation = new float[50];
        int index = 0;

        // --- Final Product ---
        observation[index++] = activeRecipe.finalProductData.GetNormalizedID();

        float[] finalStateOneHot = GetOneHotIngredientState(activeRecipe.finalProductState);
        foreach (float val in finalStateOneHot)
            observation[index++] = val;

        // --- Ingredients (baseRequiredIngredients only) ---
        for (int i = 0; i < maxIngredients; i++)
        {
            if (i < activeRecipe.baseRequiredIngredients.Count)
            {
                var req = activeRecipe.baseRequiredIngredients[i];
                observation[index++] = req.ingredient.GetNormalizedID();

                IngredientState requiredState = req.requiredState;
                IngredientState? preconditionState = req.ingredient.GetPreconditionState(requiredState);

                //PRECONDITION STATE
                float[] oneHotPrecondition = GetOneHotIngredientState(preconditionState);
                foreach (float val in oneHotPrecondition)
                    observation[index++] = val;

                //REQUIRED STATE
                float[] oneHotRequired = GetOneHotIngredientState(req.requiredState);
                foreach (float val in oneHotRequired)
                    observation[index++] = val;
            }
            else
            {
                // Pad
                observation[index++] = 0f;
                for (int s = 0; s < stateCount; s++)
                    observation[index++] = 0f;
            }
        }

        return observation;
    }

    public float[] GetOneHotIngredientState(IngredientState? state)
    {
        int numStates = System.Enum.GetValues(typeof(IngredientState)).Length;
        float[] oneHot = new float[numStates];

        if (state == null) return oneHot;

        oneHot[(int)state] = 1f;

        return oneHot;
    }

    public float[] GetTileObservations(Vector3 pos, int range, bool showDebug = false)
    {
        int width = range + range + 1;

        float[] observation = new float[(width * width) * observationsPerTileWithRelativePos];
        int index = 0;

        GameObject[,] workspaces = new GameObject[width,width];

        Vector2Int currentVector2Pos = GetVector2IntPos(pos);
        WorkspaceGenerator workspaceGenerator = WorkspaceGenerator.Instance;
        Vector2Int vector2Range = new Vector2Int(range, range);

        //ASSIGN GRID OF GAMEOBJECTS TO AD OBSERVATIONS
        for (int z = -range; z < range + 1; z++)
        {
            for (int x = -range; x < range + 1; x++)
            {
                Vector2Int posToCheck = currentVector2Pos + new Vector2Int(x, z);

                var tileOneHot = GetOneHotTileObservationWithRelativePos(workspaceGenerator.GetWorkspaceAt(posToCheck), posToCheck.Equals(currentVector2Pos), new Vector3(x, 0, z));

                foreach (float val in tileOneHot)
                    observation[index++] = val;

                if (showDebug) Debug.Log("Tile Observation: [" + string.Join(", ", tileOneHot) + "]");
            }
        }

        return observation;
    }

    public float[] GetOneHotTileObservationWithRelativePos(GameObject workspace, bool playerOccupied, Vector3 relativePos)
    {
        float[] observation = new float[observationsPerTileWithRelativePos];

        int index = 0;

        observation[index++] = relativePos.x;
        observation[index++] = relativePos.z;

        float[] tileObservation = GetOneHotTileObservation(workspace, playerOccupied);
        foreach (float val in tileObservation)
            observation[index++] = val;

        return observation;
    }

    public float[] GetDeliveryStationObservation(Vector3 playerPos)
    {
        float[] observation = new float[3];

        GameObject deliveryStation = WorkspaceGenerator.Instance.GetDeliveryStation();

        observation[0] = playerPos.x - deliveryStation.transform.position.x;
        observation[1] = playerPos.z - deliveryStation.transform.position.z;
        observation[2] = GetWorkspaceType(deliveryStation);

        return observation;
    }

    public float[] GetOneHotTileObservation(GameObject workspace, bool playerOccupied)
    {
        /*
        tileType (e.g. 0 = floor, 1 = workspace, 2 = stove...), 

        isOccupiedByAgent (bool) 

        HasItem (bool) 

        can workspace process (bool)

        cookProgress (0-1), 

        outputState (one hot)

        itemType (normalised ID), 

        itemState [1,0,0,0,0] raw etc

        */

        float[] observation = new float[observationsPerTile];

        int index = 0;

        //TYPE
        observation[index++] = GetWorkspaceType(workspace);

        //IS WORKSPACE OCCUPIED BY THE AGENT
        observation[index++] = playerOccupied ? 1 : 0;

        //HAS ITEM (0,1)
        observation[index++] = WorkspaceHasItem(workspace);

        //HAS STORAGE (0,1)
        observation[index++] = WorkspaceHasStorage(workspace);

        //CAN WORKSPACE PROCESS INGREDIENTS
        observation[index++] = GetWorkspaceCanProcess(workspace);

        //WORKSPACE PROCESSING PROGRESS
        observation[index++] = GetWorkspaceProgress(workspace);

        //ONE HOT OUTPUT STATE

        float[] oneHotOutputState = GetWorkspaceOutputStateOneHot(workspace);
        foreach (float val in oneHotOutputState)
            observation[index++] = val;

        //ID OF ITEM ON WORKSPACE
        observation[index++] = GetItemIDOnWorkspace(workspace);

        //STATE OF ITEM ON WORKSPACE IF INGREDIENT
        float[] ingredientState = GetIngredientStateOnWorkpace(workspace);
        foreach (float val in ingredientState)
            observation[index++] = val;

        return observation;
    }

    private float GetWorkspaceType(GameObject obj)
    {
        const float totalWorkspaces = 7f;

        if (obj == null) return 0f;

        if (obj.TryGetComponent(out EmptyWorkspace _)) return (1f / totalWorkspaces);
        else if (obj.TryGetComponent(out ChoppingBoard _)) return (2f / totalWorkspaces);
        else if (obj.TryGetComponent(out Stove _)) return (3f / totalWorkspaces);
        else if (obj.TryGetComponent(out Hob _)) return (4f / totalWorkspaces);
        else if (obj.TryGetComponent(out Bin _)) return (5f / totalWorkspaces);
        else if (obj.TryGetComponent(out DeliveryStation _)) return (6f / totalWorkspaces);
        else if (obj.TryGetComponent(out IngredientSpawner _)) return (7f / totalWorkspaces);

        return 0f;
    }

    private int WorkspaceHasItem(GameObject obj)
    {
        if (obj?.TryGetComponent(out Workspace workspace) == true)
        {
            return workspace.HasItems ? 1 : 0;
        }

        return 0;
    }

    private int WorkspaceHasStorage(GameObject obj)
    {
        if (obj?.TryGetComponent(out Workspace workspace) == true)
        {
            return workspace.GetFirstItem()?.TryGetComponent(out PortableStorage storage) == true ? 1 : 0;
        }

        return 0;
    }

    private float GetItemIDOnWorkspace(GameObject obj)
    {
        if (obj?.TryGetComponent(out Workspace workspace) == true && workspace.HasItems)
        {
            GameObject item = workspace.GetFirstItem();
            if (item != null && item?.TryGetComponent(out IngredientItem ingredient) == true)
            {
                return ingredient.IngredientData.GetNormalizedID();
            }
            else if (item != null && item.TryGetComponent(out PortableStorage storage) == true)
            {
                //PLATE
                return 0f;
            }
        }
        else if (obj?.TryGetComponent(out IngredientSpawner spawner) == true)
        {
            return spawner.GetSpawnedIngredient().GetNormalizedID();
        }

        return 0;
    }

    private float GetWorkspaceCanProcess(GameObject obj)
    {
        if (obj?.TryGetComponent(out Workspace workspace) == true)
        {
            return workspace.CanProcessItems() ? 1 : 0;
        }

        return 0;
    }

    private float GetWorkspaceProgress(GameObject obj)
    {
        if (obj?.TryGetComponent(out Workspace workspace) == true)
        {
            return workspace.CanProcessItems() ? workspace.GetProccessAmount() : 0;
        }

        return 0;
    }

    private float[] GetWorkspaceOutputStateOneHot(GameObject obj)
    {
        if (obj?.TryGetComponent(out Workspace workspace) == true)
        {
            return GetOneHotIngredientState(workspace.GetOutputState());
        }

        return new float[5];
    }

    private float[] GetIngredientStateOnWorkpace(GameObject obj)
    { 
        //If it is a workspace and there is an ingredient stored on it
        if (obj != null && obj?.TryGetComponent(out Workspace workspace) == true)
        {
            GameObject item = workspace?.GetFirstItem();
            if (item != null && item?.TryGetComponent(out IngredientItem ingredient) == true)
            {
                return GetOneHotIngredientState(ingredient.CurrentState);
            }
        }
        if (obj != null && obj?.TryGetComponent(out IngredientSpawner spawner) == true)
        {
            return GetOneHotIngredientState(spawner.GetSpawnedIngredient().initialState);
        }

        return new float[5];
    }


    public float[] GetAllPlateObservations(Vector3 playerPosition)
    {
        Vector2Int playerVector2Pos = GetVector2IntPos(playerPosition);

        List<Plate> plates = PlateInitializer.Instance.GetPlates();

        int plateCount = 3;
        float[] observation = new float[GetPlateObservationSize() * plateCount];
        int index = 0;

        for (int i = 0; i < plateCount; i++)
        {
            if (i < plates.Count)
            {
                float[] plateObservation = GetPlateObservation(plates[i], playerVector2Pos);

                foreach (float val in plateObservation)
                    observation[index++] = val;
            }
        }
        return observation;
    }

    public int GetPlateObservationSize()
    {
        int numStates = System.Enum.GetValues(typeof(IngredientState)).Length;
        int plateMaxIngredients = 4;
        return 1 + 2 + 1 + plateMaxIngredients * (1 + numStates);
    }

    private float[] GetPlateObservation(Plate plate, Vector2Int playerPos)
    {
        int numStates = System.Enum.GetValues(typeof(IngredientState)).Length;
        int plateMaxIngredients = 4;

        int observationSize = GetPlateObservationSize();
        //int observationSize = 24;
        float[] observation = new float[observationSize];
        int index = 0;

        if (plate == null)
        {
            for(int i = 0; i < observationSize; i++)
            {
                observation[i] = 0;
            }

            return observation;
        }

        Vector2Int platePos = GetVector2IntPos(plate.gameObject.transform.position);
        Vector2Int relativePos = platePos - playerPos;

        //PLATE EXISTS
        observation[index++] = 1;

        observation[index++] = relativePos.x;
        observation[index++] = relativePos.y;

        observation[index++] = plate.HasCombinedIngredients ? 1 : 0;

        for (int i = 0; i < plateMaxIngredients; i++)
        {
            if (i < plate.StoredItems.Count)
            {
                GameObject storedItem = plate.StoredItems[i];

                if (storedItem?.TryGetComponent(out IngredientItem ingredientItem) == true)
                {
                    // INGREDIENT ID
                    observation[index++] = ingredientItem.IngredientData.GetNormalizedID();

                    // INGREDIENT STATE (one-hot)
                    float[] stateOneHot = GetOneHotIngredientState(ingredientItem.CurrentState);
                    foreach (float val in stateOneHot)
                        observation[index++] = val;
                }
                else
                {
                    // Padding for invalid item
                    observation[index++] = 0;
                    for (int j = 0; j < numStates; j++)
                        observation[index++] = 0;
                }
            }
            else
            {
                // Padding for unused ingredient slot
                observation[index++] = 0;
                for (int j = 0; j < numStates; j++)
                    observation[index++] = 0;
            }
        }

        return observation;
    }


    public float[] GetAllIngredientObservations(Vector3 playerPosition, bool showDebug = false)
    {
        Vector2Int playerVector2Pos = GetVector2IntPos(playerPosition);

        List<IngredientItem> looseIngredients = LooseIngredientManager.Instance.GetAllLooseItems();
        int maxLooseIngredients = LooseIngredientManager.Instance.GetMaxLooseItems();
        List<IngredientSpawner> ingredientSpawners = LooseIngredientManager.Instance.GetAllIngredientSpawners();

        float[] observation = new float[9 * maxLooseIngredients];
        int index = 0;

        //INGREDIENT SPAWNERS
        for (int i = 0; i < ingredientSpawners.Count; i++)
        {
            float[] ingredientSpawnerObservation = GetIngredientSpawnerObservation(ingredientSpawners[i], playerVector2Pos, showDebug);

            foreach (float val in ingredientSpawnerObservation)
                observation[index++] = val;
        }

        //INGREDIENTS
        for (int i = 0; i < maxLooseIngredients - ingredientSpawners.Count; i++)
        {
            if (i < looseIngredients.Count)
            {
                float[] looseIngredientObservation = GetIngredientObservation(looseIngredients[i], playerVector2Pos, showDebug);

                foreach (float val in looseIngredientObservation)
                    observation[index++] = val;
            }
        }

        return observation;
    }

    private float[] GetIngredientObservation(IngredientItem ingredient, Vector2Int playerPos, bool showDebug = false)
    {
        //9 OBSERVATIONS PER LOOSE INGREDIENT
        float[] observation = new float[9];
        int index = 0;

        Vector2Int ingredientPos = GetVector2IntPos(ingredient.gameObject.transform.position);

        //INGREDIENT EXISTS
        observation[index++] = 1;

        //INGREDIENT POSITION
        Vector2Int relativePos = ingredientPos - playerPos;
        observation[index++] = relativePos.x;
        observation[index++] = relativePos.y;

        //INGREDIENT ID
        observation[index++] = ingredient.IngredientData.GetNormalizedID();

        //INGREDIENT STATE
        float[] stateOneHot = GetOneHotIngredientState(ingredient.CurrentState);
        foreach (float val in stateOneHot)
            observation[index++] = val;

        if (showDebug) Debug.Log("Loose Item: [" + string.Join(", ", observation) + "]");

        return observation;
    }

    private float[] GetIngredientSpawnerObservation(IngredientSpawner spawner, Vector2Int playerPos, bool showDebug = false)
    {
        //9 OBSERVATIONS PER LOOSE INGREDIENT
        float[] observation = new float[9];
        int index = 0;

        Vector2Int ingredientPos = GetVector2IntPos(spawner.gameObject.transform.position);

        //INGREDIENT EXISTS
        observation[index++] = 1;

        //INGREDIENT POSITION
        Vector2Int relativePos = ingredientPos - playerPos;
        observation[index++] = relativePos.x;
        observation[index++] = relativePos.y;

        //INGREDIENT ID
        observation[index++] = spawner.GetSpawnedIngredient().GetNormalizedID();

        //INGREDIENT STATE
        float[] stateOneHot = GetOneHotIngredientState(spawner.GetSpawnedIngredient().initialState);
        foreach (float val in stateOneHot)
            observation[index++] = val;

        if (showDebug) Debug.Log("IngredientSpawner: [" + string.Join(", ", observation) + "]");

        return observation;
    }


}

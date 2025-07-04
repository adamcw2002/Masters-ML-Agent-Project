using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class AgentObservationManager : MonoSingleton<AgentObservationManager>
{
    private Vector2Int GetVector2IntPos(Vector3 pos) => new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z));

    public float[] GetCurrentRecipeObservation()
    {
        RecipeData activeRecipe = RecipeManager.Instance.GetActiveRecipe();
        int maxIngredients = 4;
        int stateCount = System.Enum.GetValues(typeof(IngredientState)).Length;

        // Total: 1 ID + n state one-hot = 1 + 5 = 6 per entry
        float[] observation = new float[1 + stateCount + (maxIngredients * 2) * (1 + stateCount)];
        int index = 0;

        // --- Final Product ---
        observation[index++] = activeRecipe.finalProductData.uniqueIntID;

        float[] finalStateOneHot = GetOneHotIngredientState(activeRecipe.finalProductState);
        foreach (float val in finalStateOneHot)
            observation[index++] = val;

        // --- Ingredients (baseRequiredIngredients only) ---
        for (int i = 0; i < maxIngredients; i++)
        {
            if (i < activeRecipe.baseRequiredIngredients.Count)
            {
                var req = activeRecipe.baseRequiredIngredients[i];
                observation[index++] = req.ingredient.uniqueIntID;

                IngredientState requiredState = req.requiredState;
                IngredientState? preconditionState = req.ingredient.GetPreconditionState(requiredState);

                float[] oneHotPrecondition = GetOneHotIngredientState(preconditionState);
                foreach (float val in oneHotPrecondition)
                    observation[index++] = val;

                float[] oneHotRequired = GetOneHotIngredientState(req.requiredState);
                foreach (float val in oneHotRequired)
                    observation[index++] = val;
            }
            else
            {
                // Pad
                observation[index++] = -1f;
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

        float[] observation = new float[(width * width) * 10];
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

                var tileOneHot = GetOneHotTileObservation(workspaceGenerator.GetWorkspaceAt(posToCheck), posToCheck.Equals(currentVector2Pos));

                foreach (float val in tileOneHot)
                    observation[index++] = val;

                if (showDebug) Debug.Log("Tile Observation: [" + string.Join(", ", tileOneHot) + "]");
            }
        }

        return observation;
    }

    private float[] GetOneHotTileObservation(GameObject workspace, bool playerOccupied)
    {
        /*
        tileType (e.g. 0 = floor, 1 = workspace, 2 = stove...), 

        isOccupiedByAgent (bool) 

        HasItem (bool) 

        cookProgress (0–100 or -1 if not applicable), 

        itemType (e.g. 0 = nothing, 1 = tomato, 2 = plate...), 

        itemState [1,0,0,0,0] raw etc

        */

        float[] observation = new float[10];

        int index = 0;

        //TYPE
        observation[index++] = GetWorkspaceType(workspace);

        //IS WORKSPACE OCCUPIED BY THE AGENT
        observation[index++] = playerOccupied ? 1 : 0;

        //HAS ITEM (0,1)
        observation[index++] = WorkspaceHasItem(workspace);

        //WORKSPACE PROCESSING PROGRESS
        observation[index++] = GetWorkspaceProgress(workspace);

        //ID OF ITEM ON WORKSPACE
        observation[index++] = GetItemOnWorkspace(workspace);

        //STATE OF ITEM ON WORKSPACE IF INGREDIENT
        float[] ingredientState = GetIngredientStateOnWorkpace(workspace);
        foreach (float val in ingredientState)
            observation[index++] = val;

        return observation;
    }

    private int GetWorkspaceType(GameObject obj)
    {
        if (obj == null) return 0;

        if (obj.TryGetComponent(out EmptyWorkspace emptyWorkspace)) return 1;
        else if (obj.TryGetComponent(out ChoppingBoard choppingBoard)) return 2;
        else if (obj.TryGetComponent(out Stove stove)) return 3;
        else if (obj.TryGetComponent(out Hob hob)) return 4;
        else if (obj.TryGetComponent(out Bin bin)) return 5;
        else if (obj.TryGetComponent(out DeliveryStation delivery)) return 6;
        else if (obj.TryGetComponent(out IngredientSpawner ingredientSpawner)) return 7;

        return 0;
    }

    private int WorkspaceHasItem(GameObject obj)
    {
        if (obj?.TryGetComponent(out Workspace workspace) == true)
        {
            return workspace.HasItems ? 1 : 0;
        }

        return 0;
    }

    private int GetItemOnWorkspace(GameObject obj)
    {
        if (obj?.TryGetComponent(out Workspace workspace) == true)
        {
            GameObject item = workspace.GetFirstItem();
            if (item?.TryGetComponent(out IngredientItem ingredient) == true)
            {
                return ingredient.IngredientData.uniqueIntID;
            }
            if (item?.TryGetComponent(out PortableStorage storage) == true)
            {
                //PLATE
                return storage.GetStorageID();
            }
        }
        else if (obj?.TryGetComponent(out IngredientSpawner spawner) == true)
        {
            return spawner.GetSpawnedIngredient().uniqueIntID;
        }

        return 0;
    }

    private int GetWorkspaceProgress(GameObject obj)
    {
        if (obj?.TryGetComponent(out Workspace workspace) == true)
        {
            return workspace.CanProcessItems() ? workspace.GetProccessAmount() : -1;
        }

        return -1;
    }

    private float[] GetIngredientStateOnWorkpace(GameObject obj)
    {
        //If it is a workspace and there is an ingredient stored on it
        if (obj?.TryGetComponent(out Workspace workspace) == true && workspace?.GetFirstItem()?.TryGetComponent(out IngredientItem ingredient) == true)
        {
            return GetOneHotIngredientState(ingredient.CurrentState);
        }

        return new float[5];
    }


    public float[] GetAllPlateObservations(Vector3 playerPosition)
    {
        Vector2Int playerVector2Pos = GetVector2IntPos(playerPosition);

        List<Plate> plates = PlateInitializer.Instance.GetPlates();

        int plateCount = 3;
        float[] observation = new float[24 * plateCount];
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

    private float[] GetPlateObservation(Plate plate, Vector2Int playerPos)
    {
        int observationSize = 24;
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

        int plateMaxIngredients = 4;
        for (int i = 0; i < plateMaxIngredients; i++)
        {
            if (i < plate.StoredItems.Count)
            {
                GameObject storedItem = plate.StoredItems[i];

                if (storedItem.TryGetComponent(out IngredientItem ingredientItem))
                {
                    //INGREDIENT ID
                    observation[index++] = ingredientItem.IngredientData.uniqueIntID;

                    //INGREDIENT STATE
                    float[] stateOneHot = GetOneHotIngredientState(ingredientItem.CurrentState);
                    foreach (float val in stateOneHot)
                        observation[index++] = val;

                }
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
        observation[index++] = ingredient.IngredientData.uniqueIntID;

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
        observation[index++] = spawner.GetSpawnedIngredient().uniqueIntID;

        //INGREDIENT STATE
        float[] stateOneHot = GetOneHotIngredientState(spawner.GetSpawnedIngredient().initialState);
        foreach (float val in stateOneHot)
            observation[index++] = val;

        if (showDebug) Debug.Log("IngredientSpawner: [" + string.Join(", ", observation) + "]");

        return observation;
    }


}

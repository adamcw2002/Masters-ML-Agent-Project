using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.MLAgents;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class AgentObservationLogger : MonoBehaviour
{
    private PlayerAgent agent;
    private PlayerInteract interact;

    public KeyCode logKey = KeyCode.L;

    private void Awake()
    {
        PlayerAgent.OnAgentSpawned += PlayerAgent_OnAgentSpawned;
    }

    private void OnDestroy()
    {
        PlayerAgent.OnAgentSpawned -= PlayerAgent_OnAgentSpawned;
    }

    private void PlayerAgent_OnAgentSpawned(object sender, System.EventArgs e)
    {
        agent = (PlayerAgent)sender;
        interact = agent.GetComponent<PlayerInteract>();
    }

    void Update()
    {
        if (Input.GetKeyDown(logKey) && agent != null)
        {
            WriteObservationsToFile();
        }
    }

    void WriteObservationsToFile()
    {
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string folderPath = Application.dataPath + "/ObservationLogs";
        string filePath = $"{folderPath}/agent_obs_{timestamp}.txt";

        Directory.CreateDirectory(folderPath);
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Agent Observations Log");
        sb.AppendLine($"Date: {System.DateTime.Now.ToShortDateString()}");
        sb.AppendLine($"Time: {System.DateTime.Now.ToLongTimeString()}");
        sb.AppendLine(new string('=', 40));

        int index = 0;

        // === Position Observations ===
        sb.AppendLine("\n=== Position Observations ===");
        Vector3 pos = agent.transform.position;
        sb.AppendLine($"[{index++}] Position X: {pos.x}");
        sb.AppendLine($"[{index++}] Position Z: {pos.z}");

        // === Inventory Observations ===
        sb.AppendLine("\n=== Inventory Observations ===");
        var inventory = interact.GetAgentInventoryObservation();
        for (int i = 0; i < inventory.Length; i++)
            sb.AppendLine($"[{index++}] Inventory[{i}] {GetInventoryString(i)}: {inventory[i]}");

        // === Current Interactable Observations ===
        sb.AppendLine("\n=== Current Interactable Observations ===");
        var interactable = AgentObservationManager.Instance.GetOneHotTileObservation(interact.GetCurrentInteractableGameObject(), false);
        for (int i = 0; i < interactable.Length; i++)
            sb.AppendLine($"[{index++}] Current Interactable [{i}] {GetCurrentInteractableString(i)}: {interactable[i]}");

        // === Recipe Observations ===
        sb.AppendLine("\n=== Recipe Observations ===");
        var recipe = AgentObservationManager.Instance.GetCurrentRecipeObservation();
        for (int i = 0; i < recipe.Length; i++)
            sb.AppendLine($"[{index++}] Recipe[{i}] {GetRecipeObservationString(i)}: {recipe[i]}");

        // === Tile Observations ===
        sb.AppendLine("\n=== Tile Observations ===");

        int tileRange = 3;
        float[] tiles = AgentObservationManager.Instance.GetTileObservations(agent.transform.position, tileRange);
        int floatsPerTile = 17;
        int numTiles = tiles.Length / floatsPerTile;

        for (int tile = 0; tile < numTiles; tile++)
        {
            sb.AppendLine($"\n-- Tile #{tile} --");

            for (int j = 0; j < floatsPerTile; j++)
            {
                int flatIndex = tile * floatsPerTile + j;
                string label = GetSingleTileObservationLabel(j);
                sb.AppendLine($"[{index++}] {label}: {tiles[flatIndex]}");
            }
        }

        // === Plate Observations ===
        sb.AppendLine("\n=== Plate Observations ===");
        var plates = AgentObservationManager.Instance.GetAllPlateObservations(agent.transform.position);
        int plateObservationSize = AgentObservationManager.Instance.GetPlateObservationSize();
        int numPlates = plates.Length / plateObservationSize;

        for (int plateIndex = 0; plateIndex < numPlates; plateIndex++)
        {
            sb.AppendLine($"\n-- Plate #{plateIndex + 1} --");

            for (int j = 0; j < plateObservationSize; j++)
            {
                int flatIndex = plateIndex * plateObservationSize + j;
                string label = GetPlateObservationString(j);
                sb.AppendLine($"[{index++}] {label}: {plates[flatIndex]}");
            }
        }

        // === Loose Ingredient Observations ===
        var loose = AgentObservationManager.Instance.GetAllIngredientObservations(agent.transform.position);
        int obsPerIngredient = 9;
        int numIngredients = loose.Length / obsPerIngredient;

        int numSpawners = LooseIngredientManager.Instance.GetAllIngredientSpawners().Count;

        for (int i = 0; i < numIngredients; i++)
        {
            string type = i < numSpawners ? "Spawner" : "Loose Item";
            sb.AppendLine($"\n-- Ingredient #{i + 1} ({type}) --");

            for (int j = 0; j < obsPerIngredient; j++)
            {
                int flatIndex = i * obsPerIngredient + j;
                string label = GetLooseIngredientObservationLabel(j);
                sb.AppendLine($"[{index++}] {label}: {loose[flatIndex]}");
            }
        }

        // === Game Timer Observation ===
        sb.AppendLine("\n=== Game Timer Observation ===");
        float gameTime = GameTimer.Instance.GetCurrentGameTime();
        sb.AppendLine($"[{index++}] Game Time: {gameTime}");

        File.WriteAllText(filePath, sb.ToString());

        Debug.Log($"[AgentObservationLogger] Observations written to:\n{filePath}");
    }

    private string GetStateName(int index)
    {
        switch (index)
        {
            case 0: return "Raw";
            case 1: return "Chopped";
            case 2: return "Cooked";
            case 3: return "Boiled";
            case 4: return "Fried";
            default: return "Unknown";
        }
    }

    private string GetInventoryString(int index)
    {
        switch (index)
        {
            case 0:
                return "Is Holding Item";
            case 1:
                return "Is Holding Storage";
            case 2:
                return "Ingredient ID";
        }

        if (index > 2 && index < 8)
        {
            return $"State: {GetStateName(index - 3)}";
        }

        return "NULL";
    }

    private string GetCurrentInteractableString(int index)
    {
        return GetSingleTileObservationLabel(index + 2);
    }

    private string GetRecipeObservationString(int index)
    {
        if (index == 0)
            return "Final Product ID";

        if (index >= 1 && index <= 5)
            return $"Final Product State: {GetStateName(index - 1)}";

        // Each ingredient takes 10 slots (5 precondition + 5 final states) + 1 ID = 11 per ingredient
        int ingredientBlockStart = 6;
        int statesPerType = 5;
        int totalPerIngredient = 1 + (statesPerType * 2); // ID + pre + final
        int relativeIndex = index - ingredientBlockStart;

        int ingredientNum = relativeIndex / totalPerIngredient;
        int positionInIngredient = relativeIndex % totalPerIngredient;

        if (ingredientNum >= 0 && ingredientNum < 4) // Assuming max 4 ingredients
        {
            if (positionInIngredient == 0)
                return $"Ingredient {ingredientNum + 1} ID";

            if (positionInIngredient >= 1 && positionInIngredient <= statesPerType)
            {
                string state = GetStateName(positionInIngredient - 1);
                return $"Ingredient {ingredientNum + 1} Precondition State: {state}";
            }

            if (positionInIngredient >= statesPerType + 1 && positionInIngredient <= totalPerIngredient - 1)
            {
                string state = GetStateName(positionInIngredient - (statesPerType + 1));
                return $"Ingredient {ingredientNum + 1} Final State: {state}";
            }
        }

        return "NULL";
    }

    private string GetSingleTileObservationLabel(int index)
    {
        if (index == 0)
            return "X Position";

        if (index == 1)
            return "Z Position";

        if (index == 2)
            return "Tile Type ID";

        if (index == 3)
            return "Occupied by Agent";

        if (index == 4)
            return "Has Item";

        if (index == 5)
            return "Cooking Progress (0–100 or -1)";

        if (index >= 6 && index <= 10)
        {
            string state = GetStateName(index - 6);
            return $"Output State: {state}";
        }

        if (index == 11)
            return "Item ID";

        if (index >= 12 && index <= 16)
        {
            string state = GetStateName(index - 12);
            return $"Item State: {state}";
        }

        return "NULL";
    }

    private string GetPlateObservationString(int index)
    {
        int statesPerIngredient = System.Enum.GetValues(typeof(IngredientState)).Length;
        int perIngredientSize = 1 + statesPerIngredient; // ID + state one-hot
        int plateHeaderSize = 4; // exists, relX, relY, hasCombined

        if (index == 0) return "Plate Exists";
        if (index == 1) return "Plate Relative X Position";
        if (index == 2) return "Plate Relative Z Position";
        if (index == 3) return "Plate Has Combined Ingredients";

        int relativeIndex = index - plateHeaderSize;
        int ingredientIndex = relativeIndex / perIngredientSize;
        int withinIngredient = relativeIndex % perIngredientSize;

        if (ingredientIndex >= 0 && ingredientIndex < 4)
        {
            if (withinIngredient == 0)
                return $"Ingredient {ingredientIndex + 1} ID";

            string stateName = GetStateName(withinIngredient - 1);
            return $"Ingredient {ingredientIndex + 1} State: {stateName}";
        }

        return "NULL";
    }

    private string GetLooseIngredientObservationLabel(int index)
    {
        if (index == 0)
            return "Ingredient Exists";
        if (index == 1)
            return "Relative X Position";
        if (index == 2)
            return "Relative Z Position";
        if (index == 3)
            return "Ingredient ID";

        // index 4–8 are one-hot states
        string stateName = GetStateName(index - 4);
        return $"Ingredient State: {stateName}";
    }
}

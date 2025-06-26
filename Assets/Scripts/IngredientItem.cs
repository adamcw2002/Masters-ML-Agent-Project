using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientItem : MonoBehaviour
{
    [SerializeField] private IngredientData ingredientData;
    [SerializeField] private IngredientState currentState;

    private Dictionary<IngredientState, GameObject> stateVisuals = new Dictionary<IngredientState, GameObject>();

    public IngredientData IngredientData => ingredientData;
    public IngredientState CurrentState => currentState;
    public bool RequiresPlate => ingredientData.CheckForPlateNeeded(currentState);

    private ItemDisplay itemDisplay = null;

    private void Awake()
    {
        InitializeFromData();
    }

    private void OnDestroy()
    {
        RemoveItemDisplay();
    }

    public void RemoveItemDisplay()
    {
        itemDisplay?.ReturnToPool();
        itemDisplay = null;
    }

    public void AddItemDisplay()
    {
        if (ingredientData == null) return;
        itemDisplay = ItemDisplayManager.Instance.CreateItemDisplay(transform, ingredientData);
    }

    public void SetIngredientData(IngredientData data, IngredientState startingState = IngredientState.Raw)
    {
        ingredientData = data;
        InitializeFromData(startingState);

        AddItemDisplay();
    }

    private void InitializeFromData(IngredientState startingState = IngredientState.Raw)
    {
        if (ingredientData == null) return;

        // Clear any existing state visuals
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        stateVisuals.Clear();

        // Create visual for each possible state
        foreach (IngredientData.StateVariant stateVariant in ingredientData.possibleStates)
        {
            if (stateVariant.visualPrefab != null)
            {
                GameObject visual = Instantiate(stateVariant.visualPrefab, transform);
                visual.name = stateVariant.outputState.ToString();
                visual.SetActive(false);
                stateVisuals[stateVariant.outputState] = visual;
            }
        }

        // Set initial state
        if (startingState != IngredientState.Raw) currentState = startingState;
        else currentState = ingredientData.initialState;

        UpdateVisual();
    }

    public void ChangeState(IngredientState newState)
    {
        if (stateVisuals.ContainsKey(newState))
        {
            currentState = newState;
            UpdateVisual();

            Debug.Log("New state: " + newState);
        }
    }

    private void UpdateVisual()
    {
        // Hide all visuals
        foreach (var visual in stateVisuals.Values)
        {
            visual.SetActive(false);
        }

        // Show the current state visual
        if (stateVisuals.ContainsKey(currentState))
        {
            stateVisuals[currentState].SetActive(true);
        }
    }

    public  float[] OneHotEncodeCurrentState()
    {
        int numStates = System.Enum.GetNames(typeof(IngredientState)).Length;
        float[] oneHot = new float[numStates];

        int index = (int)currentState;
        if (index >= 0 && index < numStates)
        {
            oneHot[index] = 1f;
        }
        return oneHot;
    }
}

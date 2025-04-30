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

    private void Awake()
    {
        InitializeFromData();
    }

    public void SetIngredientData(IngredientData data)
    {
        ingredientData = data;
        InitializeFromData();
    }

    private void InitializeFromData()
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
                visual.name = stateVariant.state.ToString();
                visual.SetActive(false);
                stateVisuals[stateVariant.state] = visual;
            }
        }

        // Set initial state
        currentState = ingredientData.initialState;
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
}

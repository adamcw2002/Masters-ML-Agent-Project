 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IngredientState
{
    Raw, Chopped, Cooked
}

[CreateAssetMenu(fileName = "New Ingredient", menuName = "Ingredient")]
public class IngredientData : ScriptableObject
{
    public string ingredientName;
    public Sprite icon;

    [System.Serializable]
    public class StateVariant
    {
        public IngredientState state;
        public GameObject visualPrefab;
        public bool plateRequired;
    }

    [Header("Visual Variants by State")]
    public List<StateVariant> possibleStates = new List<StateVariant>();
    public IngredientState initialState;

    public bool CheckPossibleStates(IngredientState state)
    {
        foreach (var stateVariant in possibleStates)
        {
            if (stateVariant.state == state) return true;
        }
        return false;
    }

    public bool CheckForPlateNeeded(IngredientState state)
    {
        foreach (var stateVariant in possibleStates)
        {
            if (stateVariant.state == state) return stateVariant.plateRequired;
        }
        return false;
    }

    public GameObject GetPrefabFromState(IngredientState state)
    {
        foreach (var stateVariant in possibleStates)
        {
            if (stateVariant.state == state) return stateVariant.visualPrefab;
        }
        return null;
    }
}

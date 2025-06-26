 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IngredientState
{
    Raw, Chopped, Cooked, Boiled, Fried
}

[CreateAssetMenu(fileName = "New Ingredient", menuName = "new Ingredient")]
public class IngredientData : ScriptableObject
{
    public string ingredientName;
    public Sprite icon;
    public bool isProduct;

    public int uniqueIntID;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (uniqueIntID == 0) // 0 means uninitialized
        {
            string guidStr = System.Guid.NewGuid().ToString();
            uniqueIntID = guidStr.GetHashCode(); // Convert GUID string to int hash
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    [System.Serializable]
    public class StateVariant
    {
        public IngredientState preconditionState;
        public IngredientState outputState;
        public GameObject visualPrefab;
        public bool plateRequired;
    }

    [Header("Visual Variants by State")]
    public List<StateVariant> possibleStates = new List<StateVariant>();
    public IngredientState initialState;
    public Material ingredientMaterial;

    public bool CheckValidStates(IngredientState currentState, IngredientState outputState)
    {
        foreach (var stateVariant in possibleStates)
        {
            if (stateVariant.outputState == outputState && stateVariant.preconditionState == currentState) return true;
        }
        return false;
    }

    public bool CheckForPlateNeeded(IngredientState state)
    {
        foreach (var stateVariant in possibleStates)
        {
            if (stateVariant.outputState == state) return stateVariant.plateRequired;
        }
        return false;
    }

    public GameObject GetPrefabFromState(IngredientState state)
    {
        foreach (var stateVariant in possibleStates)
        {
            if (stateVariant.outputState == state) return stateVariant.visualPrefab;
        }
        return null;
    }

    public IngredientState? GetPreconditionState(IngredientState outputState)
    {
        foreach (var stateVariant in possibleStates)
        {
            if (stateVariant.outputState == outputState) return stateVariant.preconditionState;
        }
        return null;
    }
}

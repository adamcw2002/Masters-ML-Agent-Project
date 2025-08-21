using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IngredientState
{
    Raw, Chopped, Cooked, Boiled, Fried
}

public enum IngredientType
{
    Bread = 0,
    Brocolli = 1,
    Carrot = 2,
    Cheese = 3,
    Dough = 4,
    HotDogBun = 5,
    Lettuce = 6,
    Mushroom = 7,
    Pasta = 8,
    Sausage = 9,
    Tomato = 10,
    BrocolliMacAndCheese = 11,
    CheeseSandwhich = 12,
    HotDog = 13,
    MacAndCheese= 14,
    Pizza = 15,
    MushroomPizza = 16,
    SausagePizza = 17,
    Salad = 18
}

[CreateAssetMenu(fileName = "New Ingredient", menuName = "new Ingredient")]
public class IngredientData : ScriptableObject
{
    public string ingredientName;
    public Sprite icon;
    public bool isProduct;

    public IngredientType ingredientType;

    public int GetID() => (int)ingredientType;
    public float GetNormalizedID()
    {
        return (float)GetID() / (Enum.GetValues(typeof(IngredientType)).Length - 1);
    }


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

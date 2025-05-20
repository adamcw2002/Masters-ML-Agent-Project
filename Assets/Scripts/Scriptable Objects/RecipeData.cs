using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "New Recipe")]
public class RecipeData : ScriptableObject
{
    public string recipeName;
    public IngredientData finalProductData;
    public IngredientState finalProductState;
    public List<RequiredRecipeIngredient> baseRequiredIngredients;
    public List<RequiredRecipeIngredient> alternativeRecipe;
}

[System.Serializable]
public struct RequiredRecipeIngredient
{
    public IngredientData ingredient;
    public IngredientState requiredState;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Recipe
{
    public string recipeName;
    public List<string> requiredIngredients;
    public List<CookingStep> cookingSteps;
}
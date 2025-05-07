using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class RecipeManager : MonoSingleton<RecipeManager>
{
    [SerializeField] private List<RecipeData> availableRecipes = new List<RecipeData>();
    public List<RecipeData> GetAvailableRecipes() => availableRecipes;


    [SerializeField] private RecipeData activeRecipe;
    public RecipeData GetActiveRecipe() => activeRecipe;

    private Dictionary<IngredientData, IngredientState> currentRecipe = new Dictionary<IngredientData, IngredientState>();

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI currentRecipeNameText;
    [SerializeField] private TextMeshProUGUI currentRecipeDetailsText;

    private void Start()
    {
        if (activeRecipe == null) SelectNewRecipe();

        UpdateText();
    }

    public bool CompleteRecipe(Plate plate)
    {
        if (activeRecipe == null)
        {
            Debug.Log("No active recipe");
            return false;
        }

        // Check if holding an item
        if (plate == null)
        {
            Debug.Log("Not holding a plate");
            return false;
        }

        //Check the contents of the plate
        if (plate.IsEmpty)
        {
            Debug.Log("Plate does not have anything on it");
            return false;
        }

        List<GameObject> plateItems = plate.StoredItems;

        // Check if the amount of items on the plate are the same as the recipe
        if (activeRecipe.requiredIngredients.Count != plateItems.Count)
        {
            Debug.Log("Incorrect amount of items for recipe");
            return false;
        }

        // Create dictionary from active recipe
        InitRecipeDictionary(activeRecipe);

        // Check each item is correct
        foreach (GameObject item in plateItems)
        {
            if (item.TryGetComponent(out IngredientItem plateIngredient) == false)
            {
                Debug.Log($"{item.name} is not a valid ingredient");
                return false;
            }

            IngredientData data = plateIngredient.IngredientData;
            IngredientState state = plateIngredient.CurrentState;

            // Check if the ingredient is needed and in the correct state
            if (currentRecipe.TryGetValue(data, out IngredientState requiredState))
            {
                if (requiredState != state)
                {
                    Debug.Log($"Ingredient {data.name} is in the wrong state. Needed: {requiredState}, Got: {state}");
                    return false;
                }

                // Remove the matched ingredient to prevent duplicate matches
                currentRecipe.Remove(data);
            }
            else
            {
                Debug.Log($"Ingredient {data.name} is not required or duplicated.");
                return false;
            }
        }

        Debug.Log("Recipe delivered successfully!");

        SelectNewRecipe();

        return true;
    }

    private void InitRecipeDictionary(RecipeData recipe)
    {
        currentRecipe.Clear();

        foreach (var requiredIngredient in recipe.requiredIngredients)
        {
            currentRecipe[requiredIngredient.ingredient] = requiredIngredient.requiredState;
        }
    }

    private void SelectNewRecipe()
    {
        activeRecipe = availableRecipes[UnityEngine.Random.Range(0, availableRecipes.Count)];

        UpdateText();
    }

    private void UpdateText()
    {
        if (currentRecipeNameText && activeRecipe != null)
        {
            currentRecipeNameText.text = "Current Recipe: " + activeRecipe.recipeName;
            currentRecipeDetailsText.text = GetRecipeDetails();
        }
    }

    private string GetRecipeDetails()
    {
        StringBuilder sb = new StringBuilder();

        foreach (var IngredientRequirement in activeRecipe.requiredIngredients)
        {
            sb.AppendLine($"- {IngredientRequirement.requiredState} {IngredientRequirement.ingredient.ingredientName}");
        }

        return sb.ToString();
    }
}

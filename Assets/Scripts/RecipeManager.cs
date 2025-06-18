using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using System;

public class RecipeManager : MonoSingleton<RecipeManager>
{
    public static event Action OnRecipeCompleted;
    public static event EventHandler<RecipeData> OnNewRecipeSelected;

    [SerializeField] private List<RecipeData> availableRecipes = new List<RecipeData>();
    public List<RecipeData> GetAvailableRecipes() => availableRecipes;

    private List<RecipeData> allRecipes = new List<RecipeData>();

    public List<RecipeData> GetAllRecipes() => allRecipes;


    [SerializeField] private RecipeData activeRecipe;
    public RecipeData GetActiveRecipe() => activeRecipe;

    private Dictionary<IngredientData, IngredientState> currentRecipeRequirements = new Dictionary<IngredientData, IngredientState>();
    private Dictionary<IngredientData, IngredientState> alternativeRecipeRequirements = new Dictionary<IngredientData, IngredientState>();

    private void OnEnable()
    {
        GameTimer.OnTimeEnd += SelectNewRecipe;
        OnRecipeCompleted += SelectNewRecipe;
    }

    private void OnDisable()
    {
        GameTimer.OnTimeEnd -= SelectNewRecipe;
        OnRecipeCompleted -= SelectNewRecipe;
    }

    private void Start()
    {
        if (activeRecipe == null) SelectNewRecipe();

        UpdateText();

        allRecipes.AddRange(Resources.LoadAll<RecipeData>("Recipes"));
    }

    public RecipeData CanCombineIngredients(Plate plate)
    {
        if (plate == null || plate.IsEmpty) return null;

        return CanCombineIngredients(plate.StoredItems);
    }

    public RecipeData CanCombineIngredients(List<GameObject> storedIngredients)
    {
        RecipeData matched = GetMatchingRecipe(storedIngredients);
        return matched;
    }

    public RecipeData GetMatchingRecipe(List<GameObject> storedItems)
    {
        foreach (RecipeData recipe in allRecipes)
        {
            InitRecipeDictionaries(recipe);

            if (ComparePlateToRecipeDictionary(new Dictionary<IngredientData, IngredientState>(currentRecipeRequirements), storedItems))
                return recipe;

            if (alternativeRecipeRequirements.Count > 0 &&
                ComparePlateToRecipeDictionary(new Dictionary<IngredientData, IngredientState>(alternativeRecipeRequirements), storedItems))
                return recipe;
        }

        return null;
    }

    private bool ComparePlateToRecipeDictionary(Dictionary<IngredientData, IngredientState> dict, List<GameObject> plateItems)
    {
        if (dict.Count != plateItems.Count) return false;

        foreach (GameObject item in plateItems)
        {
            if (!item.TryGetComponent(out IngredientItem plateIngredient))
            {
                Debug.Log($"{item.name} is not a valid ingredient");
                return false;
            }

            IngredientData data = plateIngredient.IngredientData;
            IngredientState state = plateIngredient.CurrentState;

            if (!dict.TryGetValue(data, out IngredientState requiredState) || requiredState != state)
            {
                Debug.Log($"Invalid ingredient or state mismatch: {data.name}");
                return false;
            }

            dict.Remove(data); // Remove to prevent duplicates
        }

        return true;
    }

    public bool CheckCompletedRecipe(Plate plate, RecipeData recipe)
    {
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

        // Check for the made up recipe on the plate
        if (plateItems.Count == 1)
        {
            plateItems[0].TryGetComponent(out IngredientItem ingredient);

            if (ingredient != null && ingredient.IngredientData == recipe.finalProductData)
            {
                return true;
            }

            return false;
        }

        return false;
    }

    public bool CompleteRecipe(Plate plate)
    {
        if (activeRecipe == null)
        {
            Debug.Log("No active recipe");
            return false;
        }

        if (CheckCompletedRecipe(plate, activeRecipe) == false) return false;

        Debug.Log("Recipe delivered successfully!");

        OnRecipeCompleted?.Invoke();

        return true;
    }

    private void InitRecipeDictionaries(RecipeData recipe)
    {
        currentRecipeRequirements.Clear();
        alternativeRecipeRequirements.Clear();

        foreach (var requiredIngredient in recipe.baseRequiredIngredients)
        {
            currentRecipeRequirements[requiredIngredient.ingredient] = requiredIngredient.requiredState;
        }

        foreach (var requiredIngredient in recipe.alternativeRecipe)
        {
            alternativeRecipeRequirements[requiredIngredient.ingredient] = requiredIngredient.requiredState;
        }
    }

    private void SelectNewRecipe()
    {
        activeRecipe = availableRecipes[UnityEngine.Random.Range(0, availableRecipes.Count)];

        OnNewRecipeSelected?.Invoke(this, activeRecipe);

        UpdateText();
    }

    private void UpdateText()
    {
        Debug.Log("Current Recipe: " + activeRecipe.recipeName + " \n " + GetRecipeDetails());
    }

    private string GetRecipeDetails()
    {
        StringBuilder sb = new StringBuilder();

        foreach (var IngredientRequirement in activeRecipe.baseRequiredIngredients)
        {
            IngredientState? preconditionState = IngredientRequirement.ingredient.GetPreconditionState(IngredientRequirement.requiredState);

            sb.AppendLine($"- {IngredientRequirement.requiredState} {(preconditionState == IngredientState.Raw ? "" : preconditionState)} {IngredientRequirement.ingredient.ingredientName}");
        }

        return sb.ToString();
    }

    public List<IngredientData> GetBaseIngredients(IngredientData data)
    {
        List<IngredientData> ingredients = new List<IngredientData>();

        foreach (var recipe in allRecipes)
        {
            if (recipe.finalProductData == data)
            {
                foreach (var requiredIngredients in recipe.baseRequiredIngredients)
                {
                    ingredients.Add(requiredIngredients.ingredient);
                }

                return ingredients;
            }
        }

        return null;
    }
}

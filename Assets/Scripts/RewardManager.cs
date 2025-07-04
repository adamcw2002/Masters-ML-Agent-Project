using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RewardManager : MonoSingleton<RewardManager>
{
    [Header("Positive Rewards")]
    [SerializeField] private float GrabCorrectIngredientReward = 0.1f;
    [SerializeField] private float CorrectIngredientToWorkspaceReward = 0.1f;
    [SerializeField] private float CorrectIngredientStateToPlateReward = 0.2f;
    [SerializeField] private float CreatedFinalRecipeOnPlateReward = 0.2f;
    [SerializeField] private float CorrectFinalProductDeliveredReward = 0.5f;
    [SerializeField] private float BinnedIncorrectPlate = 0.05f;
    [SerializeField] private float EfficientMovementReward = 0.1f;

    [Header("Negative Rewards")]
    [SerializeField] private float GrabIncorrectIngredientReward = -0.1f;
    [SerializeField] private float IncorrectIngredientToWorkspaceReward = -0.1f;
    [SerializeField] private float IncorrectRecipeDelivered = -0.5f;
    [SerializeField] private float BinnedCorrectRecipe = -0.5f;

    private PlayerAgent agent;

    private List<IngredientData> currentRequiredIngredients = new List<IngredientData>();
    private Dictionary<IngredientData, IngredientState> currentIngredientStateRequirements = new Dictionary<IngredientData, IngredientState>();
    private Dictionary<IngredientData, IngredientState> currentRecipeRequirements = new Dictionary<IngredientData, IngredientState>();

    private float idleTimer;
    private float idleTime = 10f;

    private void Start()
    {
        PlayerAgent.OnAgentSpawned += PlayerAgent_OnAgentSpawned;

        //Positive Rewards
        IngredientSpawner.OnPlayerGrabIngredient += IngredientSpawner_OnPlayerGrabIngredient;
        RecipeManager.OnNewRecipeSelected += RecipeManager_OnNewRecipeSelected;
        Workspace.OnAnyItemAddedToWorkspace += Workspace_OnItemAddedToWorkspace;
        PortableStorage.OnAnyIngredientAddedToPortableStorage += PortableStorage_OnAnyIngredientAddedToPortableStorage;
        Plate.OnAnyCombineIngredients += Plate_OnAnyCombineIngredients;
        DeliveryStation.OnRecipeDelivered += DeliveryStation_OnRecipeDelivered;
        Bin.OnPlateBinned += Bin_OnPlateBinned;

        ResetIdleTimer();
    }

    private void OnDisable()
    {
        PlayerAgent.OnAgentSpawned -= PlayerAgent_OnAgentSpawned;
    }

    private void Update()
    {
        if (idleTimer > 0)
        {
            idleTimer -= Time.deltaTime;
        }

        if (idleTimer <= 0)
        {
            AddAgentReward(-Time.deltaTime);
        }
    }

    private void PlayerAgent_OnAgentSpawned(object sender, System.EventArgs e)
    {
        agent = sender as PlayerAgent;
    }

    private void RecipeManager_OnNewRecipeSelected(object sender, RecipeData e)
    {
        currentIngredientStateRequirements.Clear();
        currentRequiredIngredients.Clear();
        currentRecipeRequirements.Clear();

        RecipeData recipe = RecipeManager.Instance.GetActiveRecipe();

        foreach (var requiredIngredient in recipe.baseRequiredIngredients)
        {
            currentRecipeRequirements[requiredIngredient.ingredient] = requiredIngredient.requiredState;
        }
    }

    private void IngredientSpawner_OnPlayerGrabIngredient(object sender, IngredientEventArgs e)
    {
        //CHECK IF THE INGREDIENT IS PART OF THE MAIN RECIPE AND HASNT BEEN PICKED UP YET

        IngredientData ingredientData = e.IngredientItem.IngredientData;

        RecipeData recipe = RecipeManager.Instance.GetActiveRecipe();

        foreach (var requiredIngredient in recipe.baseRequiredIngredients)
        {
            //CHECK IF INGREDIENT IS NEEDED FOR THE RECIPE
            if (ingredientData == requiredIngredient.ingredient)
            {
                //CHECK IF THE INGREDIENT HAS ALREADY BEEN REWARDED
                if (!currentRequiredIngredients.Contains(ingredientData))
                {
                    currentRequiredIngredients.Add(ingredientData);
                    AddAgentReward(GrabCorrectIngredientReward);
                }

                //INGREDIENT IS NEEDED BUT ALREADY USED. NO REWARD

                return;
            }
        }

        //INGREDIENT IS NOT NEEDED FOR CURRENT RECIPE, NEGATIVELY REWARD
        AddAgentReward(GrabIncorrectIngredientReward);
    }

    private void Workspace_OnItemAddedToWorkspace(object sender, IngredientEventArgs e)
    {
        
        IngredientData ingredientData = e.IngredientItem?.IngredientData;
        Workspace workspace = sender as Workspace;

        if (ingredientData == null || workspace.CanProcessItems() == false) return;

        //CHECK IF THE INGREDIENT WAS ADDED TO THE CORRECT WORKSPACE FOR THE FIRST TIME

        RecipeData recipe = RecipeManager.Instance.GetActiveRecipe();

        if (ingredientData == recipe.finalProductData && workspace?.GetOutputState() == recipe.finalProductState)
        {
            if (!currentIngredientStateRequirements.ContainsKey(ingredientData))
            {
                currentIngredientStateRequirements[ingredientData] = recipe.finalProductState;
                AddAgentReward(CorrectIngredientToWorkspaceReward);
            }

            return;
        }

        foreach (var requiredIngredient in recipe.baseRequiredIngredients)
        {
            //IF INGREDIENT IS NEEDED IN THE FINAL RECIPE AND THE WORKSPACE TURNS IT INTO THE CORRECT STATE
            if (ingredientData == requiredIngredient.ingredient && (workspace?.GetOutputState() == requiredIngredient.requiredState || 
                workspace?.GetOutputState() == requiredIngredient.ingredient.GetPreconditionState(requiredIngredient.requiredState)))
            {

                //CORRECT INGREDIENT HAS NOT BEEN TURNED INTO CORRECT STATE YET, REWARD
                if (!currentIngredientStateRequirements.ContainsKey(ingredientData))
                {
                    currentIngredientStateRequirements[requiredIngredient.ingredient] = requiredIngredient.requiredState;
                    AddAgentReward(CorrectIngredientToWorkspaceReward);
                }

                //CORRECT INGREDIENT ALREADY TURNED TO CORRECT STATE, DO NOTHING
                return;
            }
        }

        //INGREDIENT STATE IS NOT NEEDED
        AddAgentReward(IncorrectIngredientToWorkspaceReward);
    }

    private void PortableStorage_OnAnyIngredientAddedToPortableStorage(object sender, IngredientEventArgs e)
    {
        //CHECK IF THE INGREDIENT ADDED TO A PLATE IS IN THE CORRECT STATE FOR THE FIRST TIME

        IngredientData ingredientData = e.IngredientItem.IngredientData;

        if (currentRecipeRequirements.TryGetValue(ingredientData, out IngredientState stateRequired)
            && e.IngredientItem.CurrentState == stateRequired)
        {
            AddAgentReward(CorrectIngredientStateToPlateReward);
            currentRecipeRequirements.Remove(ingredientData);
        }

        ResetIdleTimer();
    }

    private void Plate_OnAnyCombineIngredients(object sender, IngredientEventArgs e)
    {
        //CHECK IF PLATE HAS COMBINED ALL INGREDIENTS TO FORM THE CORRECT RECIPE

        IngredientData ingredientData = e.IngredientItem.IngredientData;
        RecipeData activeRecipeData = RecipeManager.Instance.GetActiveRecipe();

        if (ingredientData == activeRecipeData.finalProductData && e.IngredientItem.CurrentState == activeRecipeData.finalProductState)
        {
            AddAgentReward(CreatedFinalRecipeOnPlateReward);
        }

        ResetIdleTimer();
    }

    private void DeliveryStation_OnRecipeDelivered(object sender, DeliveryEventArgs e)
    {
        if (e.isCorrectRecipe)
        {
            AddAgentReward(CorrectFinalProductDeliveredReward);
        }
        else
        {
            AddAgentReward(IncorrectRecipeDelivered);
        }
    }

    private void Bin_OnPlateBinned(object sender, BinEventArgs e)
    {
        //CHECK IF THE PLAYER MADE THE WRONG RECIPE, AND THEN DISPOSED OF IT

        bool isCorrectRecipe = RecipeManager.Instance.CheckCompletedRecipe(e.plate, RecipeManager.Instance.GetActiveRecipe());

        if (isCorrectRecipe == false)
        {
            AddAgentReward(BinnedIncorrectPlate);
        }
        else
        {
            AddAgentReward(BinnedCorrectRecipe);
        }
    }


    private void AddAgentReward(float amount, bool checkEfficiency = true)
    {
        agent?.AddReward(amount);

        if (checkEfficiency && amount > 0) CheckMovementReward();

        Debug.Log("Reward: " + amount);

        ResetIdleTimer();
    }

    private void ResetIdleTimer()
    {
        idleTimer = idleTime;
    }

    private void CheckMovementReward()
    {
        if (idleTimer > 5)
        {
            AddAgentReward(EfficientMovementReward, false);
        }
    }
}

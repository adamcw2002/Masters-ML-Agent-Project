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
    [SerializeField] private float CompletingRecipeWithNoMistakes = 1f;
    [SerializeField] private bool GiveNormalisedTimeBonus;

    [Header("Negative Rewards")]
    [SerializeField] private float GrabIncorrectIngredientReward = -0.1f;
    [SerializeField] private float IncorrectIngredientToWorkspaceReward = -0.1f;
    [SerializeField] private float IncorrectRecipeDelivered = -0.5f;
    [SerializeField] private float BinnedCorrectRecipe = -0.5f;
    [SerializeField] private float IdleBehaviour = -0.1f;

    private PlayerAgent agent;

    private List<IngredientData> currentRequiredIngredients = new List<IngredientData>();
    private Dictionary<IngredientData, IngredientState> currentIngredientStateRequirements = new Dictionary<IngredientData, IngredientState>();
    private Dictionary<IngredientData, IngredientState> currentRecipeRequirements = new Dictionary<IngredientData, IngredientState>();

    private int idleStepThreshold = 300;
    private int idleStepCount;

    private bool madeMistake = false;
    private bool createdFinalRecipe = false;

    private void Start()
    {
        PlayerAgent.OnAgentSpawned += PlayerAgent_OnAgentSpawned;
        PlayerAgent.OnEpisodeEnd += PlayerAgent_OnEpisodeEnd;
        PlayerAgent.OnAgentStep += PlayerAgent_OnAgentStep;

        //Positive Rewards
        IngredientSpawner.OnPlayerGrabIngredient += IngredientSpawner_OnPlayerGrabIngredient;
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

        PlayerAgent.OnAgentStep -= PlayerAgent_OnAgentStep;

        IngredientSpawner.OnPlayerGrabIngredient -= IngredientSpawner_OnPlayerGrabIngredient;
        Workspace.OnAnyItemAddedToWorkspace -= Workspace_OnItemAddedToWorkspace;
        PortableStorage.OnAnyIngredientAddedToPortableStorage -= PortableStorage_OnAnyIngredientAddedToPortableStorage;
        Plate.OnAnyCombineIngredients -= Plate_OnAnyCombineIngredients;
        DeliveryStation.OnRecipeDelivered -= DeliveryStation_OnRecipeDelivered;
        Bin.OnPlateBinned -= Bin_OnPlateBinned;
    }

    private void PlayerAgent_OnEpisodeEnd(object sender, System.EventArgs e)
    {
        ResetIdleTimer();

        currentIngredientStateRequirements.Clear();
        currentRequiredIngredients.Clear();
        currentRecipeRequirements.Clear();

        RecipeData recipe = RecipeManager.Instance.GetActiveRecipe();

        foreach (var requiredIngredient in recipe.baseRequiredIngredients)
        {
            currentRecipeRequirements[requiredIngredient.ingredient] = requiredIngredient.requiredState;
        }

        madeMistake = false;
        createdFinalRecipe = false;
    }

    public void PlayerAgent_OnAgentStep()
    {
        idleStepCount++;

        if (idleStepCount > idleStepThreshold)
        {
            AddAgentReward(IdleBehaviour, "Idle too long", false);
            idleStepCount = 0;
        }
    }

    private void PlayerAgent_OnAgentSpawned(object sender, System.EventArgs e)
    {
        agent = sender as PlayerAgent;
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
                    AddAgentReward(GrabCorrectIngredientReward * currentRequiredIngredients.Count, "Picking up the correct ingredient");
                }

                //INGREDIENT IS NEEDED BUT ALREADY USED. NO REWARD
                ResetIdleTimer();

                return;
            }
        }

        //INGREDIENT IS NOT NEEDED FOR CURRENT RECIPE, NEGATIVELY REWARD
        AddAgentReward(GrabIncorrectIngredientReward, "Picking up the wrong ingredient");
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
                AddAgentReward(CorrectIngredientToWorkspaceReward * currentIngredientStateRequirements.Count, "Adding the correct ingredient to a workspace");
            }

            ResetIdleTimer();

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
                    AddAgentReward(CorrectIngredientToWorkspaceReward * currentIngredientStateRequirements.Count, "Adding the correct ingredient to a workspace");
                }

                ResetIdleTimer();

                //CORRECT INGREDIENT ALREADY TURNED TO CORRECT STATE, DO NOTHING
                return;
            }
        }

        //INGREDIENT STATE IS NOT NEEDED
        AddAgentReward(IncorrectIngredientToWorkspaceReward, "Adding the wrong ingredient to a workspace");
    }

    private void PortableStorage_OnAnyIngredientAddedToPortableStorage(object sender, IngredientEventArgs e)
    {
        //CHECK IF THE INGREDIENT ADDED TO A PLATE IS IN THE CORRECT STATE FOR THE FIRST TIME

        IngredientData ingredientData = e.IngredientItem.IngredientData;

        if (currentRecipeRequirements.TryGetValue(ingredientData, out IngredientState stateRequired)
            && e.IngredientItem.CurrentState == stateRequired)
        {
            AddAgentReward(CorrectIngredientStateToPlateReward, "Adding a correct ingredient to a plate");
            currentRecipeRequirements.Remove(ingredientData);
        }

        ResetIdleTimer();
    }

    private void Plate_OnAnyCombineIngredients(object sender, IngredientEventArgs e)
    {
        //CHECK IF PLATE HAS COMBINED ALL INGREDIENTS TO FORM THE CORRECT RECIPE

        if (createdFinalRecipe) return;

        IngredientData ingredientData = e.IngredientItem.IngredientData;
        RecipeData activeRecipeData = RecipeManager.Instance.GetActiveRecipe();

        if (ingredientData == activeRecipeData.finalProductData && e.IngredientItem.CurrentState == activeRecipeData.finalProductState)
        {
            AddAgentReward(CreatedFinalRecipeOnPlateReward, "Creating final recipe on plate");
            createdFinalRecipe = true;
        }

        ResetIdleTimer();
    }

    private void DeliveryStation_OnRecipeDelivered(object sender, DeliveryEventArgs e)
    {
        if (e.isCorrectRecipe)
        {
            AddAgentReward(CorrectFinalProductDeliveredReward, "Delivering correct recipe");

            if (madeMistake == false) AddAgentReward(CompletingRecipeWithNoMistakes, "Making no mistakes");

            if (GiveNormalisedTimeBonus)
            {
                AddAgentReward(GameTimer.Instance.GetNormalisedTimeRemaining(), "Time Bonus");
            }
        }
        else
        {
            AddAgentReward(IncorrectRecipeDelivered, "Delivering wrong recipe");
        }
    }

    private void Bin_OnPlateBinned(object sender, BinEventArgs e)
    {
        //CHECK IF THE PLAYER MADE THE WRONG RECIPE, AND THEN DISPOSED OF IT

        bool isCorrectRecipe = RecipeManager.Instance.CheckCompletedRecipe(e.plate, RecipeManager.Instance.GetActiveRecipe());

        if (isCorrectRecipe == false)
        {
            AddAgentReward(BinnedIncorrectPlate, "Binning incorrect recipe");
        }
        else
        {
            AddAgentReward(BinnedCorrectRecipe, "Binning correct recipe");
        }
    }

    public void AddAgentReward(float amount, string reason, bool resetIdleTimer = true)
    {
        if (amount == 0) return;

        agent?.AddReward(amount);

        //if (checkEfficiency && amount > 0) CheckMovementReward();

        Debug.Log("Reward for " + reason + ": " + amount);

        if (amount > 0 && resetIdleTimer)
        {

            ResetIdleTimer();
        }

        if (amount < 0) madeMistake = true;
    }

    private void ResetIdleTimer()
    {
        idleStepCount = 0;
    }
}

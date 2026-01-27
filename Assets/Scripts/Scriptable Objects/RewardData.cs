using UnityEngine;

[CreateAssetMenu(fileName = "RewardData", menuName = "Reward Data", order = 1)]
public class RewardData : ScriptableObject
{
    [Header("Positive Rewards")]
    [SerializeField] private float grabCorrectIngredientReward = 0.1f;
    [SerializeField] private float correctIngredientToWorkspaceReward = 0.1f;
    [SerializeField] private float correctIngredientStateToPlateReward = 0.2f;
    [SerializeField] private float createdFinalRecipeOnPlateReward = 0.2f;
    [SerializeField] private float pickedUpPlateWithFinalRecipe = 0.5f;
    [SerializeField] private float correctFinalProductDeliveredReward = 0.5f;
    [SerializeField] private float binnedIncorrectPlate = 0.05f;
    [SerializeField] private float completingRecipeWithNoMistakes = 1f;
    [SerializeField] private float timeBonusMultiplier = 1f;
    [SerializeField] private bool giveNormalisedTimeBonus;

    [Header("Negative Rewards")]
    [SerializeField] private float grabIncorrectIngredientReward = -0.1f;
    [SerializeField] private float incorrectIngredientToWorkspaceReward = -0.1f;
    [SerializeField] private float incorrectRecipeDelivered = -0.5f;
    [SerializeField] private float binnedCorrectRecipe = -0.5f;
    [SerializeField] private float addedExtraIngredient = -0.1f;
    [SerializeField] private float idleBehaviour = -0.1f;
    [SerializeField] private int idleStepThreshold = 300;

    // Public getters
    public float GrabCorrectIngredientReward => grabCorrectIngredientReward;
    public float CorrectIngredientToWorkspaceReward => correctIngredientToWorkspaceReward;
    public float CorrectIngredientStateToPlateReward => correctIngredientStateToPlateReward;
    public float CreatedFinalRecipeOnPlateReward => createdFinalRecipeOnPlateReward;
    public float PickedUpPlateWithFinalRecipe => pickedUpPlateWithFinalRecipe;
    public float CorrectFinalProductDeliveredReward => correctFinalProductDeliveredReward;
    public float BinnedIncorrectPlate => binnedIncorrectPlate;
    public float CompletingRecipeWithNoMistakes => completingRecipeWithNoMistakes;
    public float TimeBonusMultiplier => timeBonusMultiplier;
    public bool GiveNormalisedTimeBonus => giveNormalisedTimeBonus;

    public float GrabIncorrectIngredientReward => grabIncorrectIngredientReward;
    public float IncorrectIngredientToWorkspaceReward => incorrectIngredientToWorkspaceReward;
    public float IncorrectRecipeDelivered => incorrectRecipeDelivered;
    public float BinnedCorrectRecipe => binnedCorrectRecipe;
    public float AddedExtraIngredient => addedExtraIngredient;
    public float IdleBehaviour => idleBehaviour;
    public int IdleStepThreshold => idleStepThreshold;
}

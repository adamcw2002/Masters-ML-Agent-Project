using TMPro;
using UnityEngine;

public class RewardDataUI : MonoSingleton<RewardDataUI>
{
    [SerializeField] private TextMeshProUGUI latestRewardText;
    [SerializeField] private TextMeshProUGUI reasonText;
    [SerializeField] private TextMeshProUGUI episodeTotalText;
    [SerializeField] private TextMeshProUGUI averageRewardText;

    [SerializeField] private TextMeshProUGUI loggerText;

    private float episodeTotal = 0;
    private float cumulativeReward = 0;
    private int episodeCount = 0;

    private void Start()
    {
        PlayerAgent.OnEpisodeEnd += PlayerAgent_OnEpisodeEnd;
    }

    private void PlayerAgent_OnEpisodeEnd(object sender, System.EventArgs e)
    {
        cumulativeReward += episodeTotal;
        episodeCount++;

        UpdateAverageReward();

        // Reset for next episode
        episodeTotal = 0;
        loggerText.text = "";
    }

    public void SetLatestReward(float reward)
    {
        episodeTotal += reward;
        latestRewardText.text = "Latest Reward\n" + reward;

        UpdateEpisodeTotal();
    }

    public void SetRewardReason(string reason)
    {
        reasonText.text = reason;
    }

    public void LogReward(float reward, string reason)
    {
        loggerText.text += $"\n{reward} | {reason}";
    }

    private void UpdateEpisodeTotal()
    {
        episodeTotalText.text = "Episode Total\n" + episodeTotal;
    }

    private void UpdateAverageReward()
    {
        if (episodeCount == 0) return;

        float average = cumulativeReward / episodeCount;
        averageRewardText.text = "Average Reward\n" + average.ToString("F2");
    }
}

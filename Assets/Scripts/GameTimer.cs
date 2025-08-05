using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoSingleton<GameTimer>
{
    public static event Action OnTimeEnd;

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timerDuration = 60f;

    private float currentTime;
    public float GetCurrentGameTime() => currentTime;

    private void OnEnable()
    {
        PlayerAgent.OnEpisodeEnd += PlayerAgent_OnEpisodeEnd;
    }

    private void OnDisable()
    {
        PlayerAgent.OnEpisodeEnd -= PlayerAgent_OnEpisodeEnd;
    }
    private void PlayerAgent_OnEpisodeEnd(object sender, EventArgs e)
    {
        ResetTimer();
    }

    void Start()
    {
        currentTime = timerDuration;
    }

    void Update()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            OnTimeEnd?.Invoke();
            ResetTimer();
        }

        UpdateTimerUI();
    }

    public float GetNormalisedTimeRemaining()
    {
        return currentTime / timerDuration;
    }

    private void ResetTimer()
    {
        currentTime = timerDuration;
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}

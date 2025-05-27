using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarUI : MonoBehaviour
{
    public Image progressImage;
    private float processingTime = 0;
    private float processingTimer = 0f;
    private bool isProcessing = false;

    void Update()
    {
        if (isProcessing)
        {
            processingTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(processingTimer / processingTime);
            progressImage.fillAmount = 1 - progress;

            if (progress >= 1f)
            {
                FinishProcessing();
            }
        }
    }
    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }

    public void StartProcessing(float duration)
    {
        gameObject.SetActive(true);

        processingTime = duration;
        processingTimer = 0f;
        isProcessing = true;
        progressImage.fillAmount = 1;
    }

    public void FinishProcessing()
    {
        processingTime = 0;
        processingTimer = 0f;
        isProcessing = false;

        gameObject.SetActive(false);
    }
}

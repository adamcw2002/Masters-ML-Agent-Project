using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressBarManager : MonoSingleton<ProgressBarManager>
{
    [SerializeField] private GameObject progressBarPrefab;
    [SerializeField] private Canvas worldSpaceCanvas;
    private Transform container = null;

    [SerializeField] private Vector3 offset = new Vector3(0, 2, 0);

    private void Start()
    {
        container = new GameObject().transform;
        container.transform.parent = worldSpaceCanvas.transform;
        container.name = "Progress Bar Container";
    }

    public ProgressBarUI CreateProgressBar(Transform objectTransform)
    {
        GameObject progressBarObject = Instantiate(progressBarPrefab);
        progressBarObject.transform.SetParent(container);
        progressBarObject.transform.position = objectTransform.position + offset;

        if (progressBarObject.TryGetComponent(out ProgressBarUI controller))
        {
            return controller;
        }

        return null;
    }
}

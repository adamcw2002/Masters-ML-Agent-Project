using UnityEditor;
using UnityEngine;

public class MaterialHighlighter : MonoSingleton<MaterialHighlighter>
{
    private GameObject currentlyHighlighted = null;
    [SerializeField] private float emmisionIntensity = 0.1f;
    private Color highlightColor = Color.white;

    private void ApplyHighlight(GameObject obj)
    {
        Material mat = obj.GetComponent<Renderer>()?.material;

        if (mat?.HasProperty("_EmissionColor") == true)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", highlightColor * emmisionIntensity);
        }
    }

    private void RemoveHighlight(GameObject obj)
    {
        Material mat = obj.GetComponent<Renderer>()?.material;

        if (mat?.HasProperty("_EmissionColor") == true)
        {
            mat.DisableKeyword("_EMISSION");
        }
    }

    public void HighlightObject(GameObject obj)
    {
        if (obj == null || obj == currentlyHighlighted) return;

        // Clear previous
        if (currentlyHighlighted != null)
        {
            RemoveHighlight(currentlyHighlighted);
        }

        // Highlight new
        currentlyHighlighted = obj;
        ApplyHighlight(obj);
    }

    public void ClearHighlight()
    {
        if (currentlyHighlighted != null)
        {
            RemoveHighlight(currentlyHighlighted);
            currentlyHighlighted = null;
        }
    }
}

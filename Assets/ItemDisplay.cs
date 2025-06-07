using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ItemDisplay : MonoBehaviour
{
    private Transform followTransform = null;
    [SerializeField] private Vector3 offset = new Vector3(0, 0.5f, 0);

    List<Sprite> existingIcons = new List<Sprite>();

    private void Update()
    {
        if (followTransform)
        {
            transform.position = followTransform.position + offset;
        }
    }

    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }

    public void Init(Transform transform, IngredientData data)
    {
        followTransform = transform;

        RemoveAllIcons();

        if (data != null) AddNewIcon(data);
    }

    public void AddNewIcon(IngredientData data)
    {
        if (data.isProduct)
        {
            List<IngredientData> ingredients = RecipeManager.Instance.GetBaseIngredients(data);

            if (ingredients != null && ingredients.Count > 0)
            {
                foreach (var ingredient in ingredients)
                {
                    AddNewIcon(ingredient.icon);
                }
            }
        }
        else
        {
            AddNewIcon(data.icon);
        }
    }

    private void AddNewIcon(Sprite icon)
    {
        if (existingIcons.Contains(icon)) return;

        GameObject newIcon = new GameObject();
        newIcon.transform.SetParent(transform, false);

        Image image = newIcon.AddComponent<Image>();
        image.sprite = icon;

        existingIcons.Add(icon);
    }

    public void RemoveIcon(IngredientData data) => RemoveIcon(data.icon);

    public void RemoveIcon(Sprite icon)
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out Image image) && image.sprite == icon)
            {
                Destroy(child.gameObject);
                existingIcons.Remove(icon);
                return;
            }
        }
    }

    public void RemoveAllIcons()
    {
        foreach (Transform child in transform)
        {
            Destroy(child?.gameObject);
            existingIcons.Clear();
        }
    }

    public void ReturnToPool()
    {
        RemoveAllIcons();

        if (gameObject != null) ObjectPooler.Instance?.ReturnToPool(ItemDisplayManager.poolKey, gameObject);
    }

}

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class ItemDisplay : MonoBehaviour
{
    private Transform followTransform = null;
    [SerializeField] private Vector3 offset = new Vector3(0, 0.5f, 0);

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

        if (data != null) AddNewIcon(data.icon);
    }

    public void AddNewIcon(IngredientData data) => AddNewIcon(data.icon);

    public void AddNewIcon(Sprite icon)
    {
        GameObject newIcon = new GameObject();
        newIcon.transform.SetParent(transform, false);

        Image image = newIcon.AddComponent<Image>();
        image.sprite = icon;
    }



    public void RemoveIcon(IngredientData data) => RemoveIcon(data.icon);

    public void RemoveIcon(Sprite icon)
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out Image image) && image.sprite == icon)
            {
                Destroy(child.gameObject);
                return;
            }
        }
    }

    public void ReturnToPool()
    {
        if (gameObject) ObjectPooler.Instance.ReturnToPool(ItemDisplayManager.poolKey, gameObject);
    }

}

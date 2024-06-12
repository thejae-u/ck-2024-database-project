using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoHandler : MonoBehaviour
{
    [SerializeField] private Image itemInfoImage;
    [SerializeField] private Button itemSelectButton;

    public bool IsSelected { get; private set; }
    
    private void Awake()
    {
        itemSelectButton = GetComponentInChildren<Button>();
        itemInfoImage = GetComponentInChildren<Image>();
        IsSelected = false;
    }

    private void OnEnable()
    {
        itemSelectButton.onClick.AddListener(OnItemSelectButtonClick);
    }

    private void OnDisable()
    {
        itemSelectButton.onClick.RemoveListener(OnItemSelectButtonClick);
    }

    public void Deselect()
    {
        IsSelected = false;
        itemInfoImage.color = Color.white;
    }
    
    private void OnItemSelectButtonClick()
    {
        IsSelected = !IsSelected;
        itemInfoImage.color = IsSelected ? Color.yellow : Color.white;
    }
}

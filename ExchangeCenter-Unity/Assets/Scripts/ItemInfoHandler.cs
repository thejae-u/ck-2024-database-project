using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoHandler : MonoBehaviour
{
    [SerializeField] private Image itemInfoImage;
    [SerializeField] private Button itemSelectButton;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemPrice;
    
    public string UserID { get; private set; }
    public string ItemName { get; set; }
    private string ItemPrice { get; set; }
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

    public void SetItemInfo(string[] itemInfo)
    {
        UserID = itemInfo[0];
        ItemName = itemInfo[1];
        ItemPrice = itemInfo[2];
        
        itemName.text = $"Item Name : {ItemName}";
        itemPrice.text = $"Price : {int.Parse(ItemPrice):C}";
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

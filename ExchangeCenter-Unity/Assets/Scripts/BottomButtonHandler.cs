using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BottomButtonHandler : MonoBehaviour
{
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button inventoryButton;
    
    private readonly List<string> _selectedItemList = new();

    private void OnEnable()
    {
        buyButton.onClick.AddListener(OnBuyButtonClick);
        sellButton.onClick.AddListener(OnSellButtonClick);
        inventoryButton.onClick.AddListener(OnInventoryButtonClick);
    }

    private void OnDisable()
    {
        buyButton.onClick.RemoveListener(OnBuyButtonClick);
        sellButton.onClick.RemoveListener(OnSellButtonClick);
        inventoryButton.onClick.RemoveListener(OnInventoryButtonClick);
    }


    private void OnBuyButtonClick()
    {
        foreach (var itemInfo in ItemListHandler.Instance.ItemList)
        {
            if (itemInfo.IsSelected)
            {
                _selectedItemList.Add(itemInfo.name);
            }
        }
        
        foreach(var itemName in _selectedItemList)
        {
            Debug.Log($"Buy Button Clicked with {itemName}");
        }
    }

    private void OnSellButtonClick()
    {
        Debug.Log($"Sell Button Clicked");
    }

    private void OnInventoryButtonClick()
    {
        Debug.Log($"Inventory Button Clicked");
    }
}
